#!/usr/bin/env bash
set -euo pipefail

SOURCE_HOST="${SOURCE_HOST:-}"
SOURCE_PORT="${SOURCE_PORT:-}"
SOURCE_API_KEY="${SOURCE_API_KEY:-}"
TARGET_URL="${TARGET_URL:-http://localhost:8787}"
TARGET_API_KEY="${TARGET_API_KEY:-}"
TARGET_CONFIG="${TARGET_CONFIG:-/opt/bookdarr-dev/config/config.xml}"
TARGET_DB="${TARGET_DB:-/opt/bookdarr-dev/config/readarr.db}"
REPLACE_EXISTING="${REPLACE_EXISTING:-true}"
ENV_FILE="${ENV_FILE:-/opt/bookdarr-dev/import-indexers.env}"

usage() {
  cat <<'EOF'
Usage:
  import-indexers.sh <source_host> <source_port> <source_api_key> [target_api_key] [target_url]

Environment variables:
  SOURCE_HOST, SOURCE_PORT, SOURCE_API_KEY
  TARGET_API_KEY (optional; will be read from TARGET_CONFIG if missing)
  TARGET_URL (default: http://localhost:8787)
  TARGET_CONFIG (default: /opt/bookdarr-dev/config/config.xml)
  TARGET_DB (default: /opt/bookdarr-dev/config/readarr.db)
  ENV_FILE (default: /opt/bookdarr-dev/import-indexers.env)
  REPLACE_EXISTING (default: true)
EOF
}

ensure_echo() {
  if command -v stty >/dev/null 2>&1; then
    stty echo || true
  fi
}

if [ -f "${ENV_FILE}" ]; then
  set -a
  # shellcheck disable=SC1090
  . "${ENV_FILE}"
  set +a
fi

if [ $# -ge 3 ]; then
  SOURCE_HOST="$1"
  SOURCE_PORT="$2"
  SOURCE_API_KEY="$3"
fi

if [ $# -ge 4 ]; then
  TARGET_API_KEY="$4"
fi

if [ $# -ge 5 ]; then
  TARGET_URL="$5"
fi

if [ -z "${SOURCE_HOST}" ]; then
  read -r -p "Source Readarr host (IP/DNS): " SOURCE_HOST
fi

if [ -z "${SOURCE_PORT}" ]; then
  read -r -p "Source Readarr port: " SOURCE_PORT
fi

if [ -z "${SOURCE_API_KEY}" ]; then
  ensure_echo
  read -r -p "Source Readarr API key: " SOURCE_API_KEY
fi

if [ -z "${TARGET_URL}" ]; then
  TARGET_URL="http://localhost:8787"
fi

read -r -p "Target Bookdarr URL [${TARGET_URL}]: " target_url_input
if [ -n "${target_url_input}" ]; then
  TARGET_URL="${target_url_input}"
fi

if [ -z "${SOURCE_HOST}" ] || [ -z "${SOURCE_PORT}" ] || [ -z "${SOURCE_API_KEY}" ]; then
  usage
  exit 1
fi

if [ -z "${TARGET_API_KEY}" ] && [ -f "${TARGET_CONFIG}" ]; then
  TARGET_API_KEY="$(
    python3 - "${TARGET_CONFIG}" <<'PY'
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1]
try:
    root = ET.parse(path).getroot()
    key = root.findtext("ApiKey")
    if key:
        print(key.strip())
except Exception:
    pass
PY
  )"
fi

if [ -z "${TARGET_API_KEY}" ]; then
  ensure_echo
  read -r -p "Target Bookdarr API key: " TARGET_API_KEY
fi

if [ -z "${TARGET_API_KEY}" ]; then
  echo "Target API key not set. Provide TARGET_API_KEY or TARGET_CONFIG." >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to run this script." >&2
  exit 1
fi

log() {
  printf '[%s] %s\n' "$(date -u +'%F %T UTC')" "$*"
}

SOURCE_URL="http://${SOURCE_HOST}:${SOURCE_PORT}"

log "Fetching indexers from ${SOURCE_URL}"
source_indexers_json="$(
  curl -fsS "${SOURCE_URL}/api/v1/indexer" -H "X-Api-Key: ${SOURCE_API_KEY}"
)"

log "Fetching existing indexers from ${TARGET_URL}"
if ! target_indexers_json="$(
  curl -fsS "${TARGET_URL}/api/v1/indexer" -H "X-Api-Key: ${TARGET_API_KEY}"
)"; then
  log "Failed to fetch target indexers (HTTP error)."
  if [ -f "${TARGET_DB}" ] && command -v sqlite3 >/dev/null 2>&1; then
    read -r -p "Reset target indexers in ${TARGET_DB}? This deletes all target indexers. [y/N]: " reset_confirm
    if [[ "${reset_confirm}" =~ ^[Yy]$ ]]; then
      if ! sqlite3 "${TARGET_DB}" "PRAGMA busy_timeout=5000; DELETE FROM Indexers; DELETE FROM sqlite_sequence WHERE name='Indexers';"; then
        echo "Failed to reset indexers (database locked). Stop Bookdarr and rerun." >&2
        exit 1
      fi
      target_indexers_json="[]"
      log "Target indexers cleared. Continuing with import."
    else
      echo "Cannot continue without target indexers. Resolve the API error and retry." >&2
      exit 1
    fi
  else
    echo "Cannot reset target indexers (missing sqlite3 or DB). Resolve the API error and retry." >&2
    exit 1
  fi
fi

declare -A TARGET_IDS=()
while IFS='|' read -r name id; do
  if [ -n "${name}" ] && [ -n "${id}" ]; then
    TARGET_IDS["${name}"]="${id}"
  fi
done < <(
  printf '%s' "${target_indexers_json}" | python3 - <<'PY'
import json
import sys

data = json.load(sys.stdin)
for item in data:
    name = item.get("name") or ""
    id_value = item.get("id")
    if name and id_value is not None:
        print(f"{name}|{id_value}")
PY
)

payload_file="$(mktemp)"
printf '%s' "${source_indexers_json}" | python3 - <<'PY' > "${payload_file}"
import json
import sys

data = json.load(sys.stdin)
for item in data:
    payload = {
        "name": item.get("name"),
        "implementation": item.get("implementation"),
        "configContract": item.get("configContract"),
        "fields": item.get("fields") or [],
        "tags": item.get("tags") or [],
        "enableRss": item.get("enableRss", True),
        "enableAutomaticSearch": item.get("enableAutomaticSearch", True),
        "enableInteractiveSearch": item.get("enableInteractiveSearch", True),
        "priority": item.get("priority", 25),
        "downloadClientId": item.get("downloadClientId", 0),
    }
    print(f"{payload['name']}\t{json.dumps(payload)}")
PY

success=0
skipped=0
failed=0

while IFS=$'\t' read -r name payload; do
  if [ -z "${name}" ]; then
    continue
  fi

  if [ -n "${TARGET_IDS[${name}]:-}" ]; then
    if [ "${REPLACE_EXISTING}" = "true" ]; then
      log "Deleting existing indexer: ${name}"
      if ! curl -fsS -X DELETE "${TARGET_URL}/api/v1/indexer/${TARGET_IDS[${name}]}" \
        -H "X-Api-Key: ${TARGET_API_KEY}" >/dev/null; then
        log "Failed to delete ${name}"
        failed=$((failed + 1))
        continue
      fi
    else
      log "Skipping existing indexer: ${name}"
      skipped=$((skipped + 1))
      continue
    fi
  fi

  if curl -fsS -X POST "${TARGET_URL}/api/v1/indexer" \
    -H "X-Api-Key: ${TARGET_API_KEY}" \
    -H "Content-Type: application/json" \
    -d "${payload}" >/dev/null; then
    log "Imported indexer: ${name}"
    success=$((success + 1))
  else
    log "Failed to import indexer: ${name}"
    failed=$((failed + 1))
  fi
done < "${payload_file}"

rm -f "${payload_file}"

log "Done. Imported: ${success}, Skipped: ${skipped}, Failed: ${failed}"

if [ "${failed}" -gt 0 ]; then
  exit 1
fi

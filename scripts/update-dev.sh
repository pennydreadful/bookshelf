#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
USER_NAME="${USER_NAME:-joe}"
RUN_APP="${RUN_APP:-true}"
LOG_FILE="${LOG_FILE:-/opt/bookdarr-dev/run.log}"
CONFIG_FILE="${CONFIG_FILE:-/opt/bookdarr-dev/config/config.xml}"
DIAGNOSTICS_PUSH="${DIAGNOSTICS_PUSH:-true}"
DIAGNOSTICS_WAIT_SECONDS="${DIAGNOSTICS_WAIT_SECONDS:-60}"

SUDO=""
if command -v sudo >/dev/null 2>&1; then
  SUDO="sudo"
fi

log() {
  printf '[%s] %s\n' "$(date -u +'%F %T UTC')" "$*"
}

run_as_user() {
  if [ "$(id -u)" -eq 0 ] && [ -n "${SUDO}" ]; then
    ${SUDO} -u "${USER_NAME}" "$@"
  else
    "$@"
  fi
}

read_config_values() {
  python3 - <<'PY' "${CONFIG_FILE}"
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1]
tree = ET.parse(path)
root = tree.getroot()

def get_value(tag, default=""):
    elem = root.find(tag)
    if elem is None or elem.text is None:
        return default
    return elem.text.strip()

print(get_value("ApiKey"))
print(get_value("Port", "8787"))
print(get_value("EnableSsl", "False"))
print(get_value("UrlBase", ""))
PY
}

push_diagnostics() {
  if [ "${DIAGNOSTICS_PUSH}" != "true" ]; then
    log "Diagnostics push disabled."
    return
  fi

  if [ ! -f "${CONFIG_FILE}" ]; then
    log "Diagnostics push skipped (missing config)."
    return
  fi

  mapfile -t config_values < <(read_config_values)
  API_KEY="${config_values[0]:-}"
  PORT="${config_values[1]:-8787}"
  ENABLE_SSL="${config_values[2]:-False}"
  URL_BASE="${config_values[3]:-}"

  if [ -z "${API_KEY}" ]; then
    log "Diagnostics push skipped (missing API key)."
    return
  fi

  SCHEME="http"
  CURL_EXTRA=""
  if [ "${ENABLE_SSL}" = "True" ] || [ "${ENABLE_SSL}" = "true" ]; then
    SCHEME="https"
    CURL_EXTRA="-k"
  fi

  BASE_URL="${SCHEME}://localhost:${PORT}${URL_BASE}"
  log "Waiting for Bookdarr API before diagnostics push."

  READY="false"
  ATTEMPTS=$((DIAGNOSTICS_WAIT_SECONDS / 2))
  if [ "${ATTEMPTS}" -lt 1 ]; then
    ATTEMPTS=1
  fi

  for _ in $(seq 1 "${ATTEMPTS}"); do
    if curl -fsS ${CURL_EXTRA} -H "X-Api-Key: ${API_KEY}" "${BASE_URL}/api/v1/system/status" >/dev/null 2>&1; then
      READY="true"
      break
    fi
    sleep 2
  done

  if [ "${READY}" != "true" ]; then
    log "Diagnostics push skipped (API not ready)."
    return
  fi

  log "Pushing diagnostics bundle."
  if ! curl -fsS ${CURL_EXTRA} -H "X-Api-Key: ${API_KEY}" -X POST "${BASE_URL}/api/v1/diagnostics/push" >/dev/null 2>&1; then
    log "Diagnostics push failed."
  fi
}

log "Stopping Bookdarr"
if pgrep -f "${REPO_DIR}/_output/net10.0" >/dev/null 2>&1; then
  ${SUDO} pkill -f "${REPO_DIR}/_output/net10.0"
fi

log "Updating repo"
run_as_user bash -lc "cd \"${REPO_DIR}\" && git fetch && git checkout develop && git reset --hard origin/develop"

log "Building"
run_as_user bash -lc "cd \"${REPO_DIR}\" && ./scripts/dev-build.sh"

if [ "${RUN_APP}" = "true" ]; then
  log "Starting Bookdarr"
  if [ "$(id -u)" -eq 0 ] && [ -n "${SUDO}" ]; then
    ${SUDO} -u "${USER_NAME}" nohup "${REPO_DIR}/scripts/dev-run.sh" > "${LOG_FILE}" 2>&1 &
  else
    nohup "${REPO_DIR}/scripts/dev-run.sh" > "${LOG_FILE}" 2>&1 &
  fi

  push_diagnostics
else
  log "Update complete. Skipping start."
fi

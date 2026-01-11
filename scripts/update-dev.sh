#!/usr/bin/env bash
set -Eeuo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
USER_NAME="${USER_NAME:-joe}"
RUN_APP="${RUN_APP:-true}"
LOG_FILE="${LOG_FILE:-/opt/bookdarr-dev/run.log}"
CONFIG_FILE="${CONFIG_FILE:-/opt/bookdarr-dev/config/config.xml}"
CONFIG_DIR="${CONFIG_DIR:-$(dirname "${CONFIG_FILE}")}"
UPDATE_LOG_DIR="${UPDATE_LOG_DIR:-${REPO_DIR}/Logs}"
UPDATE_LOG_FILE="${UPDATE_LOG_FILE:-}"
DIAGNOSTICS_PUSH="${DIAGNOSTICS_PUSH:-true}"
DIAGNOSTICS_WAIT_SECONDS="${DIAGNOSTICS_WAIT_SECONDS:-60}"
DIAGNOSTICS_PUSHED="false"

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

read_diagnostics_config() {
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

print(get_value("DiagnosticsRepo"))
print(get_value("DiagnosticsToken"))
print(get_value("DiagnosticsGitUserName"))
print(get_value("DiagnosticsGitUserEmail"))
print(get_value("Branch"))
print(get_value("InstanceName"))
PY
}

write_sanitized_config() {
  local destination_path="$1"

  if [ ! -f "${CONFIG_FILE}" ]; then
    return
  fi

  run_as_user python3 - <<'PY' "${CONFIG_FILE}" "${destination_path}"
import sys
import xml.etree.ElementTree as ET

source_path = sys.argv[1]
destination_path = sys.argv[2]
sensitive = {"ApiKey", "DiagnosticsToken", "PostgresPassword", "SslCertPassword"}

tree = ET.parse(source_path)
root = tree.getroot()

for element in root.iter():
    if element.tag in sensitive:
        element.text = "REDACTED"

tree.write(destination_path, encoding="utf-8", xml_declaration=False)
PY
}

copy_log_files() {
  local source_folder="$1"
  local destination_folder="$2"

  if [ ! -d "${source_folder}" ]; then
    return
  fi

  run_as_user python3 - <<'PY' "${source_folder}" "${destination_folder}"
import os
import shutil
import sys

source = sys.argv[1]
destination = sys.argv[2]
allowed = {".log", ".txt", ".json", ".xml"}

for root, _, files in os.walk(source):
    for name in files:
        extension = os.path.splitext(name)[1].lower()
        if extension not in allowed:
            continue
        source_path = os.path.join(root, name)
        relative_path = os.path.relpath(source_path, source)
        destination_path = os.path.join(destination, relative_path)
        os.makedirs(os.path.dirname(destination_path), exist_ok=True)
        shutil.copy2(source_path, destination_path)
PY
}

get_latest_update_log() {
  run_as_user python3 - <<'PY' "${UPDATE_LOG_DIR}"
import glob
import os
import sys

update_dir = sys.argv[1]

if not os.path.isdir(update_dir):
    sys.exit(0)

candidates = glob.glob(os.path.join(update_dir, "update-*.log"))
if not candidates:
    sys.exit(0)

latest = max(candidates, key=os.path.getmtime)
print(latest)
PY
}

copy_update_log_file() {
  local destination_folder="$1"
  local update_log_path=""

  if [ -n "${UPDATE_LOG_FILE}" ] && [ -f "${UPDATE_LOG_FILE}" ]; then
    update_log_path="${UPDATE_LOG_FILE}"
  else
    update_log_path="$(get_latest_update_log)"
  fi

  if [ -z "${update_log_path}" ] || [ ! -f "${update_log_path}" ]; then
    return
  fi

  run_as_user mkdir -p "${destination_folder}"
  run_as_user cp "${update_log_path}" "${destination_folder}/"
}

write_diagnostics_metadata() {
  local destination_root="$1"
  local repo="$2"
  local remote_url="$3"
  local timestamp="$4"
  local branch="$5"
  local instance="$6"
  local exit_code="$7"
  local reason="$8"

  run_as_user python3 - <<'PY' "${destination_root}" "${repo}" "${remote_url}" "${timestamp}" "${branch}" "${instance}" "${exit_code}" "${reason}" "${CONFIG_DIR}"
import json
import os
import sys

destination_root = sys.argv[1]
repo = sys.argv[2]
remote_url = sys.argv[3]
timestamp = sys.argv[4]
branch = sys.argv[5]
instance = sys.argv[6]
exit_code = sys.argv[7]
reason = sys.argv[8]
config_dir = sys.argv[9]

metadata = {
    "timestamp": timestamp,
    "branch": branch,
    "instance": instance,
    "repo": repo,
    "remoteUrl": remote_url,
    "configDir": config_dir,
    "exitCode": exit_code,
    "reason": reason
}

path = os.path.join(destination_root, "diagnostics.json")
with open(path, "w", encoding="utf-8") as handle:
    json.dump(metadata, handle, indent=2)
PY
}

zip_diagnostics_bundle() {
  local source_folder="$1"
  local zip_path="$2"

  run_as_user python3 - <<'PY' "${source_folder}" "${zip_path}"
import os
import zipfile
import sys

source = sys.argv[1]
zip_path = sys.argv[2]

if os.path.exists(zip_path):
    os.remove(zip_path)

with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as bundle:
    for root, _, files in os.walk(source):
        for name in files:
            path = os.path.join(root, name)
            arcname = os.path.relpath(path, source)
            bundle.write(path, arcname)
PY
}

push_diagnostics_on_error() {
  local exit_code="${1:-1}"

  if [ "${DIAGNOSTICS_PUSHED}" = "true" ]; then
    return
  fi

  DIAGNOSTICS_PUSHED="true"

  if [ "${DIAGNOSTICS_PUSH}" != "true" ]; then
    log "Diagnostics push disabled."
    return
  fi

  if [ ! -f "${CONFIG_FILE}" ]; then
    log "Diagnostics push skipped (missing config)."
    return
  fi

  mapfile -t diag_values < <(read_diagnostics_config)
  DIAG_REPO="${diag_values[0]:-}"
  DIAG_TOKEN="${diag_values[1]:-}"
  DIAG_GIT_NAME="${diag_values[2]:-}"
  DIAG_GIT_EMAIL="${diag_values[3]:-}"
  DIAG_BRANCH="${diag_values[4]:-}"
  DIAG_INSTANCE="${diag_values[5]:-}"

  if [ "${DIAG_BRANCH}" != "develop" ]; then
    log "Diagnostics push skipped (branch ${DIAG_BRANCH:-unknown})."
    return
  fi

  if [ -z "${DIAG_REPO}" ] || [ -z "${DIAG_TOKEN}" ]; then
    log "Diagnostics push skipped (missing repo or token)."
    return
  fi

  local repo_path="${CONFIG_DIR}/diagnostics-repo"
  local timestamp
  timestamp="$(date -u +'%Y%m%d-%H%M%S')"
  local diagnostics_root="${repo_path}/diagnostics"
  local bundle_root="${diagnostics_root}/${timestamp}"
  local zip_path="${diagnostics_root}/diagnostics-${timestamp}.zip"
  local token_user="x-access-token"
  local safe_token
  safe_token="$(python3 - <<'PY' "${DIAG_TOKEN}"
import sys
import urllib.parse
print(urllib.parse.quote(sys.argv[1]))
PY
)"

  local base_url
  if [[ "${DIAG_REPO}" == http* ]]; then
    base_url="${DIAG_REPO}"
  else
    base_url="https://github.com/${DIAG_REPO}.git"
  fi

  local remote_url
  if [[ "${base_url}" == https://* ]]; then
    remote_url="https://${token_user}:${safe_token}@${base_url#https://}"
  elif [[ "${base_url}" == http://* ]]; then
    remote_url="http://${token_user}:${safe_token}@${base_url#http://}"
  else
    remote_url="${base_url}"
  fi

  local sanitized_remote="${base_url}"
  local reason

  if [ "${exit_code}" -eq 0 ]; then
    reason="update-dev.sh completed"
  else
    reason="update-dev.sh failure"
  fi

  log "Pushing diagnostics bundle before exit."

  if [ ! -d "${repo_path}/.git" ]; then
    run_as_user mkdir -p "${repo_path}"
    if ! run_as_user git clone "${remote_url}" "${repo_path}" >/dev/null 2>&1; then
      log "Diagnostics push failed (git clone)."
      return
    fi
  else
    run_as_user git -C "${repo_path}" remote set-url origin "${remote_url}" >/dev/null 2>&1 || true
  fi

  local branch
  branch="$(run_as_user git -C "${repo_path}" symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null || true)"
  branch="${branch##*/}"
  if [ -z "${branch}" ] || [ "${branch}" = "(unknown)" ]; then
    branch="$(run_as_user bash -lc "git -C \"${repo_path}\" remote show origin 2>/dev/null | sed -n 's/.*HEAD branch: //p' | head -n 1" || true)"
  fi
  if [ -z "${branch}" ] || [ "${branch}" = "(unknown)" ]; then
    branch="main"
  fi

  if ! run_as_user git -C "${repo_path}" checkout -B "${branch}" >/dev/null 2>&1; then
    run_as_user git -C "${repo_path}" checkout -b "${branch}" >/dev/null 2>&1 || true
  fi

  if run_as_user git -C "${repo_path}" ls-remote --exit-code --heads origin "${branch}" >/dev/null 2>&1; then
    run_as_user git -C "${repo_path}" pull --rebase origin "${branch}" >/dev/null 2>&1 || true
  fi

  run_as_user mkdir -p "${bundle_root}"

  copy_log_files "${CONFIG_DIR}/logs" "${bundle_root}/app-logs"
  copy_log_files "${REPO_DIR}/Logs" "${bundle_root}/update-logs"
  copy_log_files "${CONFIG_DIR}/Logs" "${bundle_root}/legacy-logs"
  copy_update_log_file "${bundle_root}/update-log"

  write_sanitized_config "${bundle_root}/config.xml"
  write_diagnostics_metadata "${bundle_root}" "${DIAG_REPO}" "${sanitized_remote}" "${timestamp}" "${DIAG_BRANCH}" "${DIAG_INSTANCE}" "${exit_code}" "${reason}"

  zip_diagnostics_bundle "${bundle_root}" "${zip_path}"
  run_as_user rm -rf "${bundle_root}"

  run_as_user git -C "${repo_path}" add . >/dev/null 2>&1

  if [ -z "$(run_as_user git -C "${repo_path}" status --porcelain 2>/dev/null)" ]; then
    log "Diagnostics push skipped (no changes)."
    run_as_user git -C "${repo_path}" remote set-url origin "${sanitized_remote}" >/dev/null 2>&1 || true
    return
  fi

  if [ -z "${DIAG_GIT_NAME}" ]; then
    DIAG_GIT_NAME="Bookdarr Diagnostics"
  fi

  if [ -z "${DIAG_GIT_EMAIL}" ]; then
    DIAG_GIT_EMAIL="diagnostics@bookdarr.local"
  fi

  run_as_user git -C "${repo_path}" config user.name "${DIAG_GIT_NAME}"
  run_as_user git -C "${repo_path}" config user.email "${DIAG_GIT_EMAIL}"

  if ! run_as_user git -C "${repo_path}" commit -m "Diagnostics ${timestamp}" >/dev/null 2>&1; then
    log "Diagnostics push failed (git commit)."
    run_as_user git -C "${repo_path}" remote set-url origin "${sanitized_remote}" >/dev/null 2>&1 || true
    return
  fi

  if ! run_as_user git -C "${repo_path}" push -u origin "${branch}" >/dev/null 2>&1; then
    log "Diagnostics push failed (git push)."
    run_as_user git -C "${repo_path}" remote set-url origin "${sanitized_remote}" >/dev/null 2>&1 || true
    return
  fi

  run_as_user git -C "${repo_path}" remote set-url origin "${sanitized_remote}" >/dev/null 2>&1 || true
  log "Diagnostics bundle pushed (${zip_path})."
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

on_error() {
  local exit_code=$?
  trap - ERR
  set +e
  push_diagnostics_on_error "${exit_code}"
  exit "${exit_code}"
}

trap on_error ERR

on_exit() {
  local exit_code=$?

  push_diagnostics_on_error "${exit_code}"
}

trap on_exit EXIT

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

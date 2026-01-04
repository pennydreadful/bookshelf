#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
USER_NAME="${USER_NAME:-joe}"
RUN_APP="${RUN_APP:-true}"
LOG_FILE="${LOG_FILE:-/opt/bookdarr-dev/run.log}"

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

log "Stopping Bookdarr"
if pgrep -f "${REPO_DIR}/_output/net6.0" >/dev/null 2>&1; then
  ${SUDO} pkill -f "${REPO_DIR}/_output/net6.0"
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
else
  log "Update complete. Skipping start."
fi

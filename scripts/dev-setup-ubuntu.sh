#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
USER_NAME="${USER_NAME:-joe}"

SUDO=""
if command -v sudo >/dev/null 2>&1; then
  SUDO="sudo"
fi

run_as_user() {
  if [ -n "${SUDO}" ]; then
    ${SUDO} -u "${USER_NAME}" "$@"
  else
    su - "${USER_NAME}" -c "$*"
  fi
}

log() {
  printf '[%s] %s\n' "$(date -u +'%F %T UTC')" "$*"
}

log "Installing base packages"
${SUDO} apt-get update
${SUDO} apt-get install -y curl git ca-certificates gnupg lsb-release build-essential python3

need_node=true
if command -v node >/dev/null 2>&1; then
  node_major=$(node -v | sed 's/^v//' | cut -d. -f1)
  if [ "${node_major}" -ge 20 ]; then
    need_node=false
  fi
fi

if [ "${need_node}" = "true" ]; then
  log "Installing Node.js 20"
  if [ -n "${SUDO}" ]; then
    curl -fsSL https://deb.nodesource.com/setup_20.x | ${SUDO} bash -
  else
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  fi
  ${SUDO} apt-get install -y nodejs
fi

log "Enabling Corepack (Yarn 1.22.19)"
${SUDO} corepack enable
run_as_user corepack prepare yarn@1.22.19 --activate

need_dotnet=true
if command -v dotnet >/dev/null 2>&1; then
  if dotnet --list-sdks | grep -q '^6\.'; then
    need_dotnet=false
  fi
fi

if [ "${need_dotnet}" = "true" ]; then
  log "Installing .NET SDK 6.0"
  ubuntu_version="$(lsb_release -rs)"
  ${SUDO} curl -fsSL "https://packages.microsoft.com/config/ubuntu/${ubuntu_version}/packages-microsoft-prod.deb" -o /tmp/packages-microsoft-prod.deb
  ${SUDO} dpkg -i /tmp/packages-microsoft-prod.deb
  ${SUDO} rm -f /tmp/packages-microsoft-prod.deb
  ${SUDO} apt-get update
  ${SUDO} apt-get install -y dotnet-sdk-6.0
fi

log "Preparing repo at ${REPO_DIR}"
${SUDO} mkdir -p "${REPO_DIR}"
${SUDO} chown -R "${USER_NAME}:${USER_NAME}" "${REPO_DIR}"

if [ ! -d "${REPO_DIR}/.git" ]; then
  run_as_user git clone https://github.com/thashiznit2003/Bookdarr.git "${REPO_DIR}"
else
  run_as_user git -C "${REPO_DIR}" fetch
fi

${SUDO} mkdir -p "${REPO_DIR}/config"
${SUDO} chown -R "${USER_NAME}:${USER_NAME}" "${REPO_DIR}/config"

log "Dev setup complete. Next: run dev-build.sh, then dev-run.sh"

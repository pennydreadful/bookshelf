#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
APPDATA_DIR="${APPDATA_DIR:-/opt/bookdarr-dev/config}"
USER_NAME="${USER_NAME:-joe}"
RID="${RID:-linux-x64}"
RUN_APP="${RUN_APP:-true}"

SUDO=""
if command -v sudo >/dev/null 2>&1; then
  SUDO="sudo"
fi

log() {
  printf '[%s] %s\n' "$(date -u +'%F %T UTC')" "$*"
}

run_as_user() {
  if [ -n "${SUDO}" ]; then
    ${SUDO} -u "${USER_NAME}" "$@"
  else
    su - "${USER_NAME}" -c "$*"
  fi
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
  curl -fsSL https://deb.nodesource.com/setup_20.x | ${SUDO} bash -
  ${SUDO} apt-get install -y nodejs
fi

log "Installing Yarn 1.22.19"
${SUDO} npm install -g yarn@1.22.19

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
  if ! ${SUDO} apt-get install -y dotnet-sdk-6.0; then
    log "dotnet-sdk-6.0 not available via apt, using dotnet-install.sh fallback"
    ${SUDO} curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    ${SUDO} chmod +x /tmp/dotnet-install.sh
    ${SUDO} /tmp/dotnet-install.sh --channel 6.0 --install-dir /usr/share/dotnet
    ${SUDO} rm -f /tmp/dotnet-install.sh
    if [ ! -x /usr/bin/dotnet ]; then
      ${SUDO} ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
    fi
  fi
fi

log "Preparing repo at ${REPO_DIR}"
${SUDO} mkdir -p "${REPO_DIR}"
${SUDO} chown -R "${USER_NAME}:${USER_NAME}" "${REPO_DIR}"

if [ ! -d "${REPO_DIR}/.git" ]; then
  run_as_user git clone https://github.com/thashiznit2003/Bookdarr.git "${REPO_DIR}"
else
  run_as_user git -C "${REPO_DIR}" fetch
fi

${SUDO} mkdir -p "${APPDATA_DIR}"
${SUDO} chown -R "${USER_NAME}:${USER_NAME}" "${APPDATA_DIR}"

log "Building Bookdarr"
run_as_user bash -lc "cd \"${REPO_DIR}\" && yarn install --frozen-lockfile --network-timeout 120000 && yarn build && dotnet msbuild -restore src/Readarr.sln -p:Configuration=Release -p:Platform=Posix -p:RuntimeIdentifiers=${RID} -t:PublishAllRids"

if [ "${RUN_APP}" = "true" ]; then
  log "Starting Bookdarr (foreground)"
  run_as_user bash -lc "cd \"${REPO_DIR}\" && ./_output/net6.0/${RID}/Readarr \"/data=${APPDATA_DIR}\" /nobrowser"
else
  log "Build complete. Run /opt/bookdarr-dev/scripts/dev-run.sh to start."
fi

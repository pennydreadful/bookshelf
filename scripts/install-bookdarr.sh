#!/usr/bin/env bash
set -euo pipefail

REPO="${REPO:-thashiznit2003/Bookdarr}"
BRANCH="${BRANCH:-develop}"
INSTALL_DIR="${INSTALL_DIR:-/opt/bookdarr}"
CONFIG_DIR="${CONFIG_DIR:-/opt/bookdarr/config}"
IMAGE_TAG="${IMAGE_TAG:-bookdarr:local}"
CONTAINER_NAME="${CONTAINER_NAME:-bookdarr}"
RID="${RID:-linux-musl-x64}"
LOG_FILE="${LOG_FILE:-/opt/bookdarr/install.log}"
START_CONTAINER="${START_CONTAINER:-false}"

log() {
  printf '[%s] %s\n' "$(date -u +'%F %T UTC')" "$*"
}

on_error() {
  local exit_code=$?
  local line=$1
  local cmd=$2
  log "ERROR: command failed (exit=${exit_code}) at line ${line}: ${cmd}"
  log "Log file: ${LOG_FILE}"
  exit "$exit_code"
}

trap 'on_error ${LINENO} "${BASH_COMMAND}"' ERR

mkdir -p "$INSTALL_DIR" "$CONFIG_DIR"
touch "$LOG_FILE"
exec > >(tee -a "$LOG_FILE") 2>&1

log "Starting Bookdarr install (repo=${REPO} branch=${BRANCH})"

if ! command -v docker >/dev/null 2>&1; then
  log "ERROR: docker is not installed."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  log "ERROR: docker daemon is not running or not accessible."
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  log "ERROR: curl is not installed."
  exit 1
fi

log "Preparing install directory"
find "$INSTALL_DIR" -mindepth 1 -maxdepth 1 \
  ! -name "config" \
  ! -name "$(basename "$LOG_FILE")" \
  -exec rm -rf {} +

log "Downloading source"
curl -L "https://github.com/${REPO}/archive/refs/heads/${BRANCH}.tar.gz" | \
  tar -xz --strip-components=1 -C "$INSTALL_DIR"

log "Building UI (yarn)"
docker run --rm -v "${INSTALL_DIR}:/src" -w /src node:20-bullseye \
  bash -lc "corepack enable && corepack prepare yarn@1.22.19 --activate && yarn install --frozen-lockfile --network-timeout 120000 && yarn build"

log "Building server (.NET)"
docker run --rm -v "${INSTALL_DIR}:/src" -w /src mcr.microsoft.com/dotnet/sdk:6.0 \
  bash -lc "dotnet msbuild -restore src/Readarr.sln -p:Configuration=Release -p:Platform=Posix -p:RuntimeIdentifiers=${RID} -t:PublishAllRids"

log "Building Docker image"
DOCKER_BUILDKIT=1 docker build -t "$IMAGE_TAG" -f "$INSTALL_DIR/docker/Dockerfile" "$INSTALL_DIR"

if [ "${START_CONTAINER}" = "true" ]; then
  if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}\$"; then
    log "Removing existing container ${CONTAINER_NAME}"
    docker rm -f "$CONTAINER_NAME"
  fi

  log "Starting container ${CONTAINER_NAME}"
  docker run -d --name "$CONTAINER_NAME" -p 8787:8787 -v "${CONFIG_DIR}:/config" "$IMAGE_TAG"
  log "Install complete. Open http://<vm-ip>:8787"
else
  log "Build complete. Redeploy your Portainer stack to start the container."
fi

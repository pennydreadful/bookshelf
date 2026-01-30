#!/usr/bin/env bash
set -euo pipefail

# Simple helper to build bookshelf inside a containerized build env
# and then assemble the runtime docker image. This keeps your host
# clean and downloads any needed toolchain into the builder image.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILDER_IMAGE=${BUILDER_IMAGE:-bookshelf-buildenv}
TAG=${TAG:-bookshelf:local}
PLATFORMS=${PLATFORMS:-linux/amd64}
PUSH=${PUSH:-0}
BUILDX_BUILDER=${BUILDX_BUILDER:-bookshelf-builder}
MODE=${1:-build}

METADATA_ARG=${METADATA_URL:+--build-arg METADATA_URL=${METADATA_URL}}
HARDCOVER_ARG=${HARDCOVER:+--build-arg HARDCOVER=${HARDCOVER}}

if ! command -v docker >/dev/null 2>&1; then
  echo "docker CLI not found; please install Docker first" >&2
  exit 1
fi

build() {
  echo "[1/3] Building builder image ${BUILDER_IMAGE}"
  docker build -t "${BUILDER_IMAGE}" -f "${ROOT_DIR}/docker/Dockerfile.buildenv" "${ROOT_DIR}"

  echo "[2/3] Running build.sh inside builder"
  docker run --rm \
    -u "$(id -u):$(id -g)" \
    -v "${ROOT_DIR}:/src" \
    -w /src \
    -e DOTNET_CLI_HOME=/src/.dotnet \
    -e DOTNET_NOLOGO=1 \
    -e NUGET_PACKAGES=/src/.nuget/packages \
    -e HOME=/tmp \
    "${BUILDER_IMAGE}" \
    bash -c "./build.sh --backend --frontend --packages --lint"

  echo "[3/3] Building runtime image ${TAG} for platform(s): ${PLATFORMS}"
  if ! docker buildx inspect "${BUILDX_BUILDER}" >/dev/null 2>&1; then
    docker buildx create --name "${BUILDX_BUILDER}" --use >/dev/null
  else
    docker buildx use "${BUILDX_BUILDER}" >/dev/null
  fi

  BUILD_CMD=(docker buildx build \
    --platform "${PLATFORMS}" \
    -t "${TAG}" \
    -f "${ROOT_DIR}/docker/Dockerfile" \
    ${METADATA_ARG} ${HARDCOVER_ARG} \
    "${ROOT_DIR}")

  if [[ "${PUSH}" == "1" ]]; then
    BUILD_CMD+=(--push)
  else
    BUILD_CMD+=(--load)
  fi

  # shellcheck disable=SC2068
  ${BUILD_CMD[@]}

  echo "Done. Image tag: ${TAG}"
}

run() {
  echo "Running ${TAG} with default config..."
  docker run -d --restart=unless-stopped \
    -p 8787:8787 \
    -v ~/.config/bookshelf:/config \
    -v /path/to/audiobooks:/audiobooks \
    -v /path/to/ebooks:/ebooks \
    -v /path/to/downloads:/downloads \
    "${TAG}"
}

case "${MODE}" in
  build)
    build
    ;;
  run)
    run
    ;;
  build-run)
    build
    run
    ;;
  *)
    echo "Usage: $0 {build|run|build-run}" >&2
    exit 1
    ;;
esac


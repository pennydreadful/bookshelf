#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
RID="${RID:-linux-x64}"

if [ ! -d "${REPO_DIR}" ]; then
  echo "Repo not found at ${REPO_DIR}. Run dev-setup-ubuntu.sh first." >&2
  exit 1
fi

cd "${REPO_DIR}"

if ! command -v yarn >/dev/null 2>&1; then
  echo "Yarn is not installed. Run dev-setup-ubuntu.sh first." >&2
  exit 1
fi

yarn install --frozen-lockfile --network-timeout 120000
yarn build

dotnet msbuild -restore src/Readarr.sln -p:Configuration=Release -p:Platform=Posix -p:RuntimeIdentifiers=${RID} -t:PublishAllRids

ui_src="${REPO_DIR}/_output/UI"
ui_dest="${REPO_DIR}/_output/net6.0/${RID}/UI"

if [ -d "${ui_src}" ]; then
  rm -rf "${ui_dest}"
  mkdir -p "${ui_dest}"
  cp -a "${ui_src}/." "${ui_dest}/"
fi

echo "Build complete. Run dev-run.sh to start Bookdarr."

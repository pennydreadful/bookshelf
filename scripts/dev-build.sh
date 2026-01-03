#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
RID="${RID:-linux-x64}"

if [ ! -d "${REPO_DIR}" ]; then
  echo "Repo not found at ${REPO_DIR}. Run dev-setup-ubuntu.sh first." >&2
  exit 1
fi

cd "${REPO_DIR}"

corepack enable
corepack prepare yarn@1.22.19 --activate

yarn install --frozen-lockfile --network-timeout 120000
yarn build

dotnet msbuild -restore src/Readarr.sln -p:Configuration=Release -p:Platform=Posix -p:RuntimeIdentifiers=${RID} -t:PublishAllRids

echo "Build complete. Run dev-run.sh to start Bookdarr."

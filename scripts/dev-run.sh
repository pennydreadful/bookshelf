#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
APPDATA_DIR="${APPDATA_DIR:-/opt/bookdarr-dev/config}"
RID="${RID:-linux-x64}"

BIN="${REPO_DIR}/_output/net6.0/${RID}/Readarr"

if [ ! -x "${BIN}" ]; then
  echo "Binary not found at ${BIN}. Run dev-build.sh first." >&2
  exit 1
fi

mkdir -p "${APPDATA_DIR}"

exec "${BIN}" "/data=${APPDATA_DIR}" "/nobrowser"

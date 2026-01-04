#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/opt/bookdarr-dev}"
APPDATA_DIR="${APPDATA_DIR:-/opt/bookdarr-dev/config}"
RID="${RID:-linux-x64}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export BOOKDARR_HOME="${REPO_DIR}"
export BOOKDARR_DATA_DIR="${APPDATA_DIR}"
export RID

exec "${SCRIPT_DIR}/run-bookdarr.sh" "$@"

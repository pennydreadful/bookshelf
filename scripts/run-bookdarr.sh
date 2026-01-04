#!/usr/bin/env bash
set -euo pipefail

RID="${RID:-linux-x64}"
BOOKDARR_HOME="${BOOKDARR_HOME:-}"
BOOKDARR_BIN="${BOOKDARR_BIN:-}"
BOOKDARR_DATA_DIR="${BOOKDARR_DATA_DIR:-}"

if [ -z "${BOOKDARR_DATA_DIR}" ]; then
  if [ -d "/config" ]; then
    BOOKDARR_DATA_DIR="/config"
  elif [ -n "${BOOKDARR_HOME}" ]; then
    BOOKDARR_DATA_DIR="${BOOKDARR_HOME}/config"
  else
    BOOKDARR_DATA_DIR="/opt/bookdarr-dev/config"
  fi
fi

if [ -z "${BOOKDARR_BIN}" ] && [ -n "${BOOKDARR_HOME}" ]; then
  candidate="${BOOKDARR_HOME}/_output/net6.0/${RID}/Readarr"
  if [ -x "${candidate}" ]; then
    BOOKDARR_BIN="${candidate}"
  fi
fi

if [ -z "${BOOKDARR_BIN}" ] && [ -x "/app/readarr/bin/Readarr" ]; then
  BOOKDARR_BIN="/app/readarr/bin/Readarr"
fi

if [ -z "${BOOKDARR_BIN}" ] && [ -x "/opt/bookdarr-dev/_output/net6.0/${RID}/Readarr" ]; then
  BOOKDARR_BIN="/opt/bookdarr-dev/_output/net6.0/${RID}/Readarr"
fi

if [ -z "${BOOKDARR_BIN}" ] || [ ! -x "${BOOKDARR_BIN}" ]; then
  echo "Bookdarr binary not found. Set BOOKDARR_BIN or BOOKDARR_HOME and retry." >&2
  exit 1
fi

mkdir -p "${BOOKDARR_DATA_DIR}"

exec "${BOOKDARR_BIN}" "/data=${BOOKDARR_DATA_DIR}" "/nobrowser" "$@"

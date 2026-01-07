#!/usr/bin/env bash
set -euo pipefail

LOG_PATH="${1:-/opt/bookdarr-dev/Logs/readarr.debug.txt}"
OUT_DIR="${2:-/opt/bookdarr-dev/Logs}"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT_FILE="${OUT_DIR}/search-rejections-${STAMP}.log"
PATTERN="Release rejected|Unable to parse release|Unable to parse books|Unknown Author|not wanted in profile|Processing release|Parsing string|Quality parsed|Author/Book null|reparsing with search criteria|Release rejected for the following reasons"

if [ ! -f "${LOG_PATH}" ]; then
  echo "Log not found: ${LOG_PATH}" >&2
  echo "Usage: $0 [log_path] [output_dir]" >&2
  exit 1
fi

mkdir -p "${OUT_DIR}"

{
  echo "Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "Source: ${LOG_PATH}"
  echo "Pattern: ${PATTERN}"
  echo "----"
} > "${OUT_FILE}"

if command -v rg >/dev/null 2>&1; then
  rg -n -i -e "${PATTERN}" "${LOG_PATH}" >> "${OUT_FILE}" || true
else
  grep -nEi "${PATTERN}" "${LOG_PATH}" >> "${OUT_FILE}" || true
fi

if [ ! -s "${OUT_FILE}" ]; then
  echo "No matches found." >> "${OUT_FILE}"
fi

echo "Wrote: ${OUT_FILE}"

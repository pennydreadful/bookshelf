#!/usr/bin/env bash
set -euo pipefail

LOG_PATH="${1:-}"
OUT_DIR="${2:-/opt/bookdarr-dev/Logs}"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT_FILE="${OUT_DIR}/search-rejections-${STAMP}.log"
PATTERN="Release rejected|Unable to parse release|Unable to parse books|Unknown Author|not wanted in profile|Processing release|Parsing string|Quality parsed|Author/Book null|reparsing with search criteria|Release rejected for the following reasons"

LOG_CANDIDATES=()
if [ -n "${LOG_PATH}" ]; then
  LOG_CANDIDATES+=("${LOG_PATH}")
else
  LOG_CANDIDATES+=("/opt/bookdarr-dev/Logs/readarr.debug.txt")
  LOG_CANDIDATES+=("/opt/bookdarr-dev/Logs/readarr.txt")
fi

mkdir -p "${OUT_DIR}"

{
  echo "Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "Pattern: ${PATTERN}"
  echo "----"
} > "${OUT_FILE}"

found_log=0
for candidate in "${LOG_CANDIDATES[@]}"; do
  if [ ! -f "${candidate}" ]; then
    continue
  fi

  found_log=1
  {
    echo "Source: ${candidate}"
    echo "LineCount: $(wc -l < "${candidate}")"
    echo "---- Matches ----"
  } >> "${OUT_FILE}"

  if command -v rg >/dev/null 2>&1; then
    if ! rg -n -i -e "${PATTERN}" "${candidate}" >> "${OUT_FILE}"; then
      echo "No matches found." >> "${OUT_FILE}"
    fi
  else
    if ! grep -nEi "${PATTERN}" "${candidate}" >> "${OUT_FILE}"; then
      echo "No matches found." >> "${OUT_FILE}"
    fi
  fi

  {
    echo "---- Tail (last 5000 lines) ----"
    tail -n 5000 "${candidate}"
    echo "---- End ----"
  } >> "${OUT_FILE}"
done

if [ "${found_log}" -eq 0 ]; then
  echo "No logs found. Checked: ${LOG_CANDIDATES[*]}" >&2
  exit 1
fi

echo "Wrote: ${OUT_FILE}"

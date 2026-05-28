#!/usr/bin/env bash
# 通过系统默认程序打开 DWG（Linux / macOS）
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: open_dwg.sh <path-to.dwg>" >&2
  exit 1
fi

FILE="$(realpath "$1")"
if [[ ! -f "$FILE" ]]; then
  echo "File not found: $FILE" >&2
  exit 1
fi

if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$FILE"
  OPENER="xdg-open"
elif command -v gio >/dev/null 2>&1; then
  gio open "$FILE"
  OPENER="gio open"
elif [[ "$(uname)" == "Darwin" ]] && command -v open >/dev/null 2>&1; then
  open "$FILE"
  OPENER="open"
else
  echo "No desktop opener found (xdg-open / gio / open)." >&2
  exit 1
fi

cat <<EOF
{"ok":true,"path":"$FILE","method":"$OPENER","platform":"$(uname -s)"}
EOF

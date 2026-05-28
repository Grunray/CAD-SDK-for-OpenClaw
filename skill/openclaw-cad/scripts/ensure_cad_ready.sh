#!/usr/bin/env bash
# 拉起中望 CAD Linux + 插件，可选打开 DWG
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLATFORM_DIR="$SCRIPT_DIR/platforms/linux-zwcad"

DWG_PATH=""
OPEN_DWG_ONLY=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dwg-path|-DwgPath) DWG_PATH="$2"; shift 2 ;;
    --open-dwg-only|-OpenDwgOnly) OPEN_DWG_ONLY=true; shift ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

# shellcheck disable=SC1091
source "$PLATFORM_DIR/config.sh"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"

if cad_api GET "/ping" >/dev/null 2>&1; then
  if [[ -n "$DWG_PATH" ]]; then
    full="$(realpath "$DWG_PATH")"
    cad_api POST "/document/open" "{\"path\":\"$full\"}"
  else
    cad_api GET "/ping"
  fi
  exit 0
fi

if [[ "$OPEN_DWG_ONLY" == true && -n "$DWG_PATH" ]]; then
  xdg-open "$(realpath "$DWG_PATH")" || true
  echo "Opened DWG only; plugin may still be offline." >&2
  exit 0
fi

"$PLATFORM_DIR/start_zwcad_with_plugin.sh" ${DWG_PATH:+--dwg-path "$DWG_PATH"}
"$PLATFORM_DIR/wait_for_plugin.sh"

if [[ -n "$DWG_PATH" ]]; then
  full="$(realpath "$DWG_PATH")"
  cad_api POST "/document/open" "{\"path\":\"$full\"}"
fi

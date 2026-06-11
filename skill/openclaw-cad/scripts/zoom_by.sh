#!/usr/bin/env bash
set -euo pipefail
FACTOR=""
CENTER_X=""
CENTER_Y=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --factor|-Factor) FACTOR="$2"; shift 2 ;;
    --center-x) CENTER_X="$2"; shift 2 ;;
    --center-y) CENTER_Y="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done
[[ -n "$FACTOR" ]] || { echo "Factor is required." >&2; exit 1; }
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"
# 服务端 /zoom/by 同时接受 GET/POST；统一走编码安全的 GET helper
params=("factor=$FACTOR")
[[ -n "$CENTER_X" ]] && params+=("centerx=$CENTER_X")
[[ -n "$CENTER_Y" ]] && params+=("centery=$CENTER_Y")
cad_api_get "/zoom/by" "${params[@]}"

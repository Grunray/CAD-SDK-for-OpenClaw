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
path="/zoom/by?factor=$FACTOR"
[[ -n "$CENTER_X" ]] && path="${path}&centerx=$CENTER_X"
[[ -n "$CENTER_Y" ]] && path="${path}&centery=$CENTER_Y"
cad_api POST "$path"

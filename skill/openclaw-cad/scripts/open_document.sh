#!/usr/bin/env bash
set -euo pipefail
PATH_ARG=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --path|-Path) PATH_ARG="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done
[[ -n "$PATH_ARG" ]] || { echo "Path is required." >&2; exit 1; }
full="$(realpath "$PATH_ARG")"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"
cad_api POST "/document/open" "{\"path\":\"$full\"}"

#!/usr/bin/env bash
set -euo pipefail
HANDLE=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --handle|-Handle) HANDLE="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done
[[ -n "$HANDLE" ]] || { echo "Handle is required." >&2; exit 1; }
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"
cad_api_get "/zoom/to" "handle=$HANDLE"

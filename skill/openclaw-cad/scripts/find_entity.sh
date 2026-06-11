#!/usr/bin/env bash
set -euo pipefail
QUERY=""
EXACT=false
LAYER=""
LIMIT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --query|-Query) QUERY="$2"; shift 2 ;;
    --exact|-Exact) EXACT=true; shift ;;
    --layer|-Layer) LAYER="$2"; shift 2 ;;
    --limit|-Limit) LIMIT="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

[[ -n "$QUERY" ]] || { echo "Query is required." >&2; exit 1; }
if [[ -n "$LIMIT" && ! "$LIMIT" =~ ^[0-9]+$ ]]; then
  echo "Limit must be a non-negative integer (0 = unlimited)." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"

params=("q=$QUERY")
[[ "$EXACT" == true ]] && params+=("exact=true")
[[ -n "$LAYER" ]] && params+=("layer=$LAYER")
[[ -n "$LIMIT" ]] && params+=("limit=$LIMIT")

cad_api_get "/find" "${params[@]}"

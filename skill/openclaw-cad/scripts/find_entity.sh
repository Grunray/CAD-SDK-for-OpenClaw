#!/usr/bin/env bash
set -euo pipefail
QUERY=""
EXACT=false
LAYER=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --query|-Query) QUERY="$2"; shift 2 ;;
    --exact|-Exact) EXACT=true; shift ;;
    --layer|-Layer) LAYER="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

[[ -n "$QUERY" ]] || { echo "Query is required." >&2; exit 1; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/cad_api.sh"

path="/find?q=$(python3 -c "import urllib.parse; print(urllib.parse.quote('''$QUERY'''))")"
[[ "$EXACT" == true ]] && path="${path}&exact=true"
[[ -n "$LAYER" ]] && path="${path}&layer=$(python3 -c "import urllib.parse; print(urllib.parse.quote('''$LAYER'''))")"

cad_api GET "$path"

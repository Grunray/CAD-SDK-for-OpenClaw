#!/usr/bin/env bash
# UOS/麒麟 x86_64 E2E 验证：ensure → open → find → zoom
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_SCRIPTS="$(cd "$SCRIPT_DIR/../.." && pwd)"

DWG_PATH="${DWG_PATH:-}"
QUERY="${QUERY:-配电柜}"

echo "== Step 1: ensure_cad_ready =="
if [[ -n "$DWG_PATH" ]]; then
  bash "$SKILL_SCRIPTS/ensure_cad_ready.sh" --dwg-path "$DWG_PATH"
else
  bash "$SKILL_SCRIPTS/ensure_cad_ready.sh"
fi

echo "== Step 2: ping =="
PING="$(bash "$SKILL_SCRIPTS/ping.sh")"
echo "$PING"
echo "$PING" | grep -q '"host"[[:space:]]*:[[:space:]]*"zwcad"' || {
  echo "Expected host=zwcad in /ping response" >&2
  exit 1
}

echo "== Step 3: health =="
# shellcheck disable=SC1091
source "$SKILL_SCRIPTS/cad_api.sh"
cad_api GET "/health"

if [[ -z "$DWG_PATH" ]]; then
  echo "DWG_PATH not set; skipping find/zoom (set DWG_PATH and QUERY to test full chain)." >&2
  exit 0
fi

echo "== Step 4: find =="
FIND="$(bash "$SKILL_SCRIPTS/find_entity.sh" --query "$QUERY")"
echo "$FIND"
HANDLE="$(echo "$FIND" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['matches'][0]['handle'] if d.get('count') else '')" 2>/dev/null || true)"
if [[ -z "$HANDLE" ]]; then
  echo "No matches for query: $QUERY" >&2
  exit 1
fi

echo "== Step 5: zoom_to handle=$HANDLE =="
bash "$SKILL_SCRIPTS/zoom_to.sh" --handle "$HANDLE"

echo "== Step 6: zoom_extents =="
bash "$SKILL_SCRIPTS/zoom_extents.sh"

echo "E2E verification passed."

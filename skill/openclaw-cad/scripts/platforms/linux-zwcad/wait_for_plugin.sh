#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/config.sh"

: "${TIMEOUT_SEC:=$COALCLAW_PING_WAIT_SEC}"
: "${INTERVAL_SEC:=$COALCLAW_PING_INTERVAL_SEC}"
BASE_URL="http://127.0.0.1:${COALCLAW_HTTP_PORT}/ping"
DEADLINE=$(( $(date +%s) + TIMEOUT_SEC ))

while [[ $(date +%s) -lt $DEADLINE ]]; do
  if curl -sfS "$BASE_URL" >/dev/null 2>&1; then
    curl -sfS "$BASE_URL"
    exit 0
  fi
  sleep "$INTERVAL_SEC"
done

echo "Plugin HTTP not ready after ${TIMEOUT_SEC}s at $BASE_URL" >&2
exit 1

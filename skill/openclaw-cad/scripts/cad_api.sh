#!/usr/bin/env bash
# 统一 HTTP 客户端（curl）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/config.sh"

resolve_base_url() {
  local candidates=(
    "${HOME}/.openclaw/kb/shared/wiki/cad-runtime.md"
    "${HOME}/.openclaw/kb/shared/wiki/autocad-runtime.md"
  )
  local f line
  for f in "${candidates[@]}"; do
    [[ -f "$f" ]] || continue
    line="$(grep -E 'baseUrl:' "$f" | head -n1 || true)"
    if [[ "$line" =~ baseUrl:[[:space:]]*(https?://[^[:space:]]+) ]]; then
      echo "${BASH_REMATCH[1]}"
      return 0
    fi
  done
  echo "http://127.0.0.1:${COALCLAW_HTTP_PORT}"
}

COALCLAW_BASE_URL="$(resolve_base_url)"

cad_api() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local url="${COALCLAW_BASE_URL}${path}"

  if [[ "$method" == "GET" ]]; then
    curl -sfS "$url"
  elif [[ -n "$body" ]]; then
    curl -sfS -X "$method" -H "Content-Type: application/json" -d "$body" "$url"
  else
    curl -sfS -X "$method" "$url"
  fi
}

export COALCLAW_BASE_URL

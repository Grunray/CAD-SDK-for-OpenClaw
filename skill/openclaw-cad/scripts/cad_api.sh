#!/usr/bin/env bash
# 统一 HTTP 客户端（curl）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/config.sh"

resolve_base_url() {
  # 用户显式设置的 COALCLAW_HTTP_PORT 优先于插件写的 runtime 发现文件
  if [[ -n "${COALCLAW_HTTP_PORT_SET:-}" ]]; then
    echo "http://127.0.0.1:${COALCLAW_HTTP_PORT}"
    return 0
  fi

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

# 必须有超时：插件 UI 线程卡住时，无超时的 curl 会挂死整条 agent 链
# max-time 需大于服务端 UI 上下文 30s 超时，留出余量
COALCLAW_CURL_OPTS=(-sfS --connect-timeout 5 --max-time "${COALCLAW_HTTP_TIMEOUT_SEC:-60}")

cad_api() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local url="${COALCLAW_BASE_URL}${path}"

  if [[ "$method" == "GET" ]]; then
    curl "${COALCLAW_CURL_OPTS[@]}" "$url"
  elif [[ -n "$body" ]]; then
    curl "${COALCLAW_CURL_OPTS[@]}" -X "$method" -H "Content-Type: application/json" -d "$body" "$url"
  else
    curl "${COALCLAW_CURL_OPTS[@]}" -X "$method" "$url"
  fi
}

# GET + query 参数。每个参数为 "name=value"，value 由 curl --data-urlencode 编码：
# 数据走 argv 而非内插进解释器源码，外部输入（LLM 生成的 query 等）没有注入面
cad_api_get() {
  local path="$1"
  shift
  local args=()
  local kv
  for kv in "$@"; do
    args+=(--data-urlencode "$kv")
  done
  curl "${COALCLAW_CURL_OPTS[@]}" --get "${args[@]}" "${COALCLAW_BASE_URL}${path}"
}

export COALCLAW_BASE_URL

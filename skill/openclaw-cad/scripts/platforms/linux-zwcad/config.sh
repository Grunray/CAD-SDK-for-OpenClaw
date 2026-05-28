#!/usr/bin/env bash
# 中望 CAD Linux 2025 路径配置
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../.." && pwd)"

COALCLAW_CAD_HOST="${COALCLAW_CAD_HOST:-zwcad}"
COALCLAW_PLATFORM="${COALCLAW_PLATFORM:-linux}"
COALCLAW_HTTP_PORT="${COALCLAW_HTTP_PORT:-54321}"
COALCLAW_PING_WAIT_SEC="${COALCLAW_PING_WAIT_SEC:-90}"
COALCLAW_PING_INTERVAL_SEC="${COALCLAW_PING_INTERVAL_SEC:-2}"

find_zwcad_exe() {
  local candidates=(
    "${COALCLAW_CAD_EXE:-}"
    "/opt/ZWCAD/zwcad"
    "/opt/zwcad/zwcad"
    "/usr/local/ZWCAD/zwcad"
    "/opt/apps/cn.zwcad.zwcad/files/zwcad"
  )
  local c
  for c in "${candidates[@]}"; do
    [[ -n "$c" && -x "$c" ]] && { echo "$c"; return 0; }
  done
  command -v zwcad 2>/dev/null || true
}

COALCLAW_CAD_EXE="${COALCLAW_CAD_EXE:-$(find_zwcad_exe)}"
COALCLAW_PLUGIN_DLL="${COALCLAW_PLUGIN_DLL:-$REPO_ROOT/plugin/src/CoalClaw.ZwCAD.Plugin/bin/Debug/net6.0/CoalClaw.ZwCAD.Plugin.dll}"

export COALCLAW_CAD_HOST COALCLAW_PLATFORM COALCLAW_CAD_EXE COALCLAW_PLUGIN_DLL
export COALCLAW_HTTP_PORT COALCLAW_PING_WAIT_SEC COALCLAW_PING_INTERVAL_SEC

#!/usr/bin/env bash
# 冷启动中望 CAD 并通过 .scr 脚本 NETLOAD 插件
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/config.sh"

DWG_PATH=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --dwg-path) DWG_PATH="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${COALCLAW_CAD_EXE:-}" || ! -x "$COALCLAW_CAD_EXE" ]]; then
  echo "中望 CAD 可执行文件未找到。请设置 COALCLAW_CAD_EXE。" >&2
  exit 1
fi

if [[ ! -f "$COALCLAW_PLUGIN_DLL" ]]; then
  echo "插件 DLL 未找到: $COALCLAW_PLUGIN_DLL — 请先在 Linux 上 build ZwCAD 插件。" >&2
  exit 1
fi

to_scr_path() {
  echo "$1" | sed 's/\\/\//g'
}

PLUGIN_PATH="$(to_scr_path "$COALCLAW_PLUGIN_DLL")"
SCR="$(mktemp /tmp/coalclaw-zwcad-XXXXXX.scr)"
{
  echo "(command \"._NETLOAD\" \"$PLUGIN_PATH\")"
  if [[ -n "$DWG_PATH" ]]; then
    echo "(command \"._OPEN\" \"$(to_scr_path "$(realpath "$DWG_PATH")")\")"
  fi
} > "$SCR"

nohup "$COALCLAW_CAD_EXE" /nologo /b "$SCR" >/dev/null 2>&1 &
echo "{\"ok\":true,\"cadExe\":\"$COALCLAW_CAD_EXE\",\"pluginDll\":\"$COALCLAW_PLUGIN_DLL\",\"scriptPath\":\"$SCR\"}"

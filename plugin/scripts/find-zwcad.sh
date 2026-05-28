#!/usr/bin/env bash
# 探测中望 CAD Linux 2025 安装目录，可选写入 Directory.Build.props.user
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WRITE_PROPS=false

for arg in "$@"; do
  case "$arg" in
    -WriteProps|--write-props) WRITE_PROPS=true ;;
  esac
done

candidates=(
  "${ZWCAD_INSTALL_DIR:-}"
  "${COALCLAW_ZWCAD_INSTALL_DIR:-}"
  "/opt/ZWCAD"
  "/opt/zwcad"
  "/usr/local/ZWCAD"
  "/opt/apps/cn.zwcad.zwcad/files"
)

found=""
for dir in "${candidates[@]}"; do
  [[ -z "$dir" ]] && continue
  dir="${dir%/}/"
  if [[ -f "${dir}ZwDatabaseMgd.dll" || -f "${dir}zdbmgd.dll" ]]; then
    found="$dir"
    break
  fi
done

if [[ -z "$found" ]]; then
  echo "未找到中望 CAD .NET 互操作 DLL（ZwDatabaseMgd.dll）。" >&2
  echo "请设置 ZWCAD_INSTALL_DIR 或 COALCLAW_ZWCAD_INSTALL_DIR 后重试。" >&2
  exit 1
fi

echo "ZwCadInstallDir=$found"

if [[ "$WRITE_PROPS" == true ]]; then
  props_file="$PLUGIN_DIR/Directory.Build.props.user"
  cat > "$props_file" <<EOF
<Project>
  <PropertyGroup>
    <ZwCadInstallDir>$found</ZwCadInstallDir>
  </PropertyGroup>
</Project>
EOF
  echo "已写入 $props_file"
fi

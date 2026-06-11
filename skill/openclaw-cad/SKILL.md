---
name: openclaw-cad
description: 跨平台 CAD 自动化——Windows 用 AutoCAD 2025，Linux 用中望 CAD Linux 2026。统一 HTTP API：开图、按名称定位图元、缩放视图。优先 ensure_cad_ready。
---

# openclaw-cad

跨平台 CAD 本地自动化 skill。插件在 CAD 进程内提供 HTTP API（默认 `http://127.0.0.1:54321`）。

| 平台 | CAD 宿主 | 拉起脚本 |
|------|----------|----------|
| Windows | AutoCAD 2025 | `ensure_cad_ready.ps1` |
| Linux (UOS/麒麟 x86_64) | 中望 CAD Linux 2026 | `ensure_cad_ready.sh` |

## Agent 标准流程（必读）

```
1. ensure_cad_ready [-DwgPath / --dwg-path <path>]
2. ping
3. find_entity -Query "关键字"
4. zoom_to -Handle <handle>  /  zoom_by  /  zoom_extents
```

**Windows：**

```powershell
powershell -ExecutionPolicy Bypass -File scripts/ensure_cad_ready.ps1 -DwgPath "C:\path\file.dwg"
powershell -ExecutionPolicy Bypass -File scripts/ping.ps1
powershell -ExecutionPolicy Bypass -File scripts/find_entity.ps1 -Query "配电柜"
powershell -ExecutionPolicy Bypass -File scripts/zoom_to.ps1 -Handle "A3F"
```

**Linux：**

```bash
bash scripts/ensure_cad_ready.sh --dwg-path "/path/file.dwg"
bash scripts/ping.sh
bash scripts/find_entity.sh --query "配电柜"
bash scripts/zoom_to.sh --handle "A3F"
```

## 平台能力矩阵

| 能力 | Windows (AutoCAD) | Linux (中望 CAD) |
|------|-------------------|------------------|
| HTTP API | 支持 | 支持 |
| `POST /document/open` | 支持 | 支持 |
| COM NETLOAD | 支持 | **不支持** |
| Win32 NETLOAD 对话框 | fallback 可用 | **不支持** |
| 开图 | ensure + `/document/open` | ensure + `/document/open` |

## 环境变量

| 变量 | 说明 |
|------|------|
| `COALCLAW_CAD_EXE` | CAD 可执行文件（`acad.exe` / `zwcad`） |
| `COALCLAW_PLUGIN_DLL` | 插件 DLL 路径 |
| `COALCLAW_HTTP_PORT` | 默认 `54321`；显式设置时优先于 runtime 发现文件，并经 CAD 进程环境决定插件监听端口 |
| `COALCLAW_PING_WAIT_SEC` | 等待插件就绪超时，默认 `90` |
| `COALCLAW_CAD_HOST` | `autocad` 或 `zwcad`（可选） |

## HTTP API 一览

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/ping` | 插件状态（含 `host`、`platform`、`apiVersion`） |
| GET | `/health` | 活动文档检查 |
| POST | `/document/open` | 打开 DWG（JSON `{"path":"..."}`） |
| GET | `/find?q=&exact=&layer=` | 按名称定位 |
| GET | `/zoom/to?handle=` | 缩放到图元 |
| POST | `/zoom/by?factor=` | 按倍率缩放 |
| GET | `/zoom/extents` | 全图 |

详见 [docs/api-contract.md](../../docs/api-contract.md)。

## Windows 专项

- 优先 `ensure_cad_ready.ps1`（COM LISP 或 `acad.exe /b`）
- SECURELOAD 拦截时 fallback：`platforms/windows-autocad/autoload_via_dialog.ps1`
- 平台脚本目录：`scripts/platforms/windows-autocad/`

## Linux 专项

- 仅 bash + curl，**禁止**声称已执行 Win32/COM 步骤
- 构建插件：在中望 CAD Linux 机器上 `plugin/scripts/find-zwcad.sh -WriteProps && dotnet build src/CoalClaw.ZwCAD.Plugin`（输出 `CoalClawZwCADPlugin.dll`）
- 自动加载：见 [docs/setup-linux-zwcad.md](../../docs/setup-linux-zwcad.md)
- 平台脚本目录：`scripts/platforms/linux-zwcad/`

## 错误处理

- `/ping` 失败 → 先 `ensure_cad_ready`；Windows 且 CAD 已开 → `autoload_via_dialog.ps1 -WaitForPlugin`
- `/find` 400 → 可能未打开 DWG，用 `-DwgPath` 重试 ensure 或 `open_document`
- `/zoom/to` 404 → handle 不存在

## 扩展新宿主

新增 CAD 平台 = 新 Plugin 项目（实现 `ICadHostContext`）+ `scripts/platforms/<name>/` 脚本包。**不改 HTTP 契约。**

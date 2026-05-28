# Windows — AutoCAD 插件开发与调试

本文说明如何在 Visual Studio 2026 中构建、调试 `CoalClaw.AutoCAD.Plugin`，并通过 `openclaw-cad` skill 调用。

Linux / 中望 CAD 见 [setup-linux-zwcad.md](setup-linux-zwcad.md)。HTTP 契约见 [api-contract.md](api-contract.md)。

## 环境要求

| 项 | 要求 |
|---|---|
| IDE | Visual Studio 2026 |
| .NET SDK | 8.0+ |
| AutoCAD | **2025**（Windows x64） |

## 1. 配置 AutoCAD 引用路径

```powershell
cd plugin
.\scripts\find-acad.ps1 -WriteProps
dotnet build src/CoalClaw.AutoCAD.Plugin/CoalClaw.AutoCAD.Plugin.csproj -c Debug
```

或设置 `$env:AcadInstallDir = "D:\...\AutoCAD 2025\"`。

## 2. 构建

```powershell
cd plugin
dotnet build src/CoalClaw.AutoCAD.Plugin/CoalClaw.AutoCAD.Plugin.csproj -c Debug
```

输出：`plugin\src\CoalClaw.AutoCAD.Plugin\bin\Debug\net8.0-windows\CoalClaw.AutoCAD.Plugin.dll`

仅构建 AutoCAD 插件（跳过 ZwCAD）：

```powershell
dotnet build src/CoalClaw.AutoCAD.Plugin/CoalClaw.AutoCAD.Plugin.csproj
```

## 3. F5 调试

1. 打开 `plugin\CoalClaw.Cad.slnx`
2. 启动配置 **AutoCAD 2025** → F5
3. 命令行：`[CoalClaw] HTTP API started at http://127.0.0.1:54321`

## 4. Skill 联调

```powershell
powershell -ExecutionPolicy Bypass -File skill/openclaw-cad/scripts/ensure_cad_ready.ps1 -DwgPath "C:\path\test.dwg"
powershell -File skill/openclaw-cad/scripts/ping.ps1
powershell -File skill/openclaw-cad/scripts/find_entity.ps1 -Query "配电柜"
```

Win32 fallback：`skill/openclaw-cad/scripts/platforms/windows-autocad/autoload_via_dialog.ps1`

## 5. HTTP 测试

```powershell
curl http://127.0.0.1:54321/ping
curl http://127.0.0.1:54321/health
curl -X POST http://127.0.0.1:54321/document/open -H "Content-Type: application/json" -d "{\"path\":\"C:/path/test.dwg\"}"
curl "http://127.0.0.1:54321/find?q=配电柜"
curl "http://127.0.0.1:54321/zoom/to?handle=A3F"
```

`/ping` 应含 `"host":"autocad"`, `"platform":"windows"`, `"apiVersion":"1"`。

## 6. 架构

```
CoalClaw.Cad.Abstractions  ← ICadHostContext
CoalClaw.Cad.Core          ← HttpServerHost, ApiRouter
CoalClaw.AutoCAD.Plugin    ← AutoCAD 宿主实现
```

Runtime 文件：`~/.openclaw/kb/shared/wiki/cad-runtime.md`（兼容读取 `autocad-runtime.md`）。

## 7. 常见问题

见原 [setup.md](setup.md) §8（NETLOAD、threadfix-v5、重复加载等）。

# Linux — 中望 CAD Linux 2025 插件开发与联调

目标环境：**中望 CAD Linux 2025** + **UOS / 银河麒麟** + **x86_64**（Debian 系 SDK）。

Windows AutoCAD 见 [setup-windows.md](setup-windows.md)。HTTP 契约见 [api-contract.md](api-contract.md)。

## 1. 前置条件

| 项 | 要求 |
|---|---|
| OS | UOS 或银河麒麟桌面版（x86_64） |
| CAD | 中望 CAD Linux 2025 |
| .NET SDK | 6.0+（与 ZwCAD .NET 插件 TFM 一致） |
| 工具 | bash, curl |

从中望官网下载 **ZRXSDK 2025 For Debian(x86_64)** 及 .NET 开发文档：  
https://www.zwsoft.cn/support/zwcad-devdoc

## 2. 配置构建环境

在中望 CAD 已安装的 Linux 机器上：

```bash
cd plugin
chmod +x scripts/find-zwcad.sh
./scripts/find-zwcad.sh --write-props
dotnet build src/CoalClaw.ZwCAD.Plugin/CoalClaw.ZwCAD.Plugin.csproj -c Debug
```

或手动设置：

```bash
export ZwCadInstallDir=/opt/ZWCAD/
dotnet build src/CoalClaw.ZwCAD.Plugin/CoalClaw.ZwCAD.Plugin.csproj
```

输出：`plugin/src/CoalClaw.ZwCAD.Plugin/bin/Debug/net6.0/CoalClaw.ZwCAD.Plugin.dll`

## 3. 加载插件

### 方式 A — 启动脚本（推荐联调）

```bash
export COALCLAW_CAD_EXE=/opt/ZWCAD/zwcad
export COALCLAW_PLUGIN_DLL=/path/to/CoalClaw.ZwCAD.Plugin.dll

bash skill/openclaw-cad/scripts/platforms/linux-zwcad/start_zwcad_with_plugin.sh
bash skill/openclaw-cad/scripts/platforms/linux-zwcad/wait_for_plugin.sh
curl http://127.0.0.1:54321/ping
```

### 方式 B — 自动加载（生产）

参考中望官方 PDF《ZWCAD_Linux_二次开发程序自动加载方式.pdf》，将 NETLOAD 写入启动配置。

模板：`skill/openclaw-cad/scripts/platforms/linux-zwcad/autoload.template.scr`

## 4. Skill 全链路

```bash
bash skill/openclaw-cad/scripts/ensure_cad_ready.sh --dwg-path "/home/user/test.dwg"
bash skill/openclaw-cad/scripts/ping.sh
bash skill/openclaw-cad/scripts/find_entity.sh --query "配电柜"
bash skill/openclaw-cad/scripts/zoom_to.sh --handle "A3F"
```

## 5. E2E 验证

```bash
chmod +x skill/openclaw-cad/scripts/platforms/linux-zwcad/verify_e2e.sh
DWG_PATH=/path/to/test.dwg QUERY=配电柜 bash skill/openclaw-cad/scripts/platforms/linux-zwcad/verify_e2e.sh
```

## 6. 环境变量

| 变量 | 说明 |
|------|------|
| `COALCLAW_CAD_EXE` | 中望 CAD 可执行文件 |
| `COALCLAW_PLUGIN_DLL` | ZwCAD 插件 DLL |
| `ZwCadInstallDir` | 构建时 ZwDatabaseMgd.dll 所在目录 |

## 7. 已知限制

- 无 COM / Win32 自动化
- ZwCAD 插件需在 Linux + 中望 SDK 环境构建
- 银河麒麟 RedHat 系需单独 SDK 包（首期 Debian x86_64）

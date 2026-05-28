# 后续待办

> 跨平台 openclaw-cad 代码已落地。以下需在对应环境机器上验证。

---

## Windows — AutoCAD 2025

- [ ] `dotnet build plugin/src/CoalClaw.AutoCAD.Plugin/CoalClaw.AutoCAD.Plugin.csproj`
- [ ] F5 或 `skill/openclaw-cad/scripts/ensure_cad_ready.ps1 -DwgPath test.dwg`
- [ ] `/ping` 含 `"host":"autocad"`, `"apiVersion":"1"`
- [ ] `POST /document/open` → `/find` → `/zoom/to` 全链路

详见 [docs/setup-windows.md](docs/setup-windows.md)

---

## Linux — 中望 CAD Linux 2026 (UOS/麒麟 x86_64)

- [ ] 安装中望 CAD Linux 2026 + .NET 6 SDK
- [ ] `plugin/scripts/find-zwcad.sh --write-props && dotnet build src/CoalClaw.ZwCAD.Plugin`
- [ ] `bash skill/openclaw-cad/scripts/ensure_cad_ready.sh --dwg-path test.dwg`
- [ ] `bash skill/openclaw-cad/scripts/platforms/linux-zwcad/verify_e2e.sh`
- [ ] 配置官方自动加载 PDF（生产环境）
- [ ] 银河麒麟抽测

详见 [docs/setup-linux-zwcad.md](docs/setup-linux-zwcad.md)

---

## OpenClaw 注册

- [ ] 注册 skill：`skill/openclaw-cad/`（`openclaw-autocad` 已重定向）
- [ ] Agent 流程：ensure_cad_ready → ping → find → zoom

---

## 架构（已实现）

```
plugin/src/
  CoalClaw.Cad.Abstractions/   ICadHostContext
  CoalClaw.Cad.Core/           HttpServerHost, ApiRouter
  CoalClaw.AutoCAD.Plugin/     Windows
  CoalClaw.ZwCAD.Plugin/       Linux 中望
skill/openclaw-cad/
  scripts/platforms/windows-autocad/
  scripts/platforms/linux-zwcad/
```

扩展新宿主：实现 `ICadHostContext` + 新增 `platforms/<name>/` 脚本，不改 HTTP 契约。

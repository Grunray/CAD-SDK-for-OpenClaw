---
name: autocad-sdk
description: 指向 openclaw-cad skill。跨平台 CAD 自动化（Windows AutoCAD + Linux 中望 CAD）。
---

# AutoCAD SDK Skill

此 skill 关联的实际脚本和资源位于：

> **`skill/openclaw-cad`**

## 快速指引

- **SKILL.md** — `skill/openclaw-cad/SKILL.md`
- **PowerShell / Bash 脚本** — `skill/openclaw-cad/scripts/`
- **插件源码** — `plugin/`（`CoalClaw.Cad.slnx`）
- **文档** — `docs/setup-windows.md`、`docs/setup-linux-zwcad.md`、`docs/api-contract.md`

## 调用方式

**Windows：**

```powershell
powershell -ExecutionPolicy Bypass -File "skill/openclaw-cad/scripts/ensure_cad_ready.ps1"
powershell -File "skill/openclaw-cad/scripts/ping.ps1"
powershell -File "skill/openclaw-cad/scripts/find_entity.ps1" -Query "配电柜"
```

**Linux：**

```bash
bash skill/openclaw-cad/scripts/ensure_cad_ready.sh --dwg-path "/path/test.dwg"
bash skill/openclaw-cad/scripts/ping.sh
bash skill/openclaw-cad/scripts/find_entity.sh --query "配电柜"
```

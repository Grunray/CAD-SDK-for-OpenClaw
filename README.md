# CoalClaw CAD SDK (openclaw-cad)

> 跨平台 CAD 二次开发能力（模糊定位图元、缩放视图、开图）封装为统一 HTTP 接口，通过 **openclaw-cad** skill 供 OpenClaw 调用。

| 平台 | CAD 宿主 | Skill |
|------|----------|-------|
| Windows | AutoCAD 2025 | `skill/openclaw-cad/` |
| Linux (UOS/麒麟 x86_64) | 中望 CAD Linux 2025 | `skill/openclaw-cad/` |

## 1. 项目目标

1. **打开图纸** — `POST /document/open` 或 ensure 脚本冷启动
2. **图元模糊定位** — `GET /find?q=关键字`
3. **视图缩放** — `/zoom/to`、`/zoom/by`、`/zoom/extents`

最终形态：**CAD 内嵌 .NET 插件（CoalClaw.Cad.Core HTTP 服务）+ openclaw-cad skill**。

文档：[docs/setup-windows.md](docs/setup-windows.md) | [docs/setup-linux-zwcad.md](docs/setup-linux-zwcad.md) | [docs/api-contract.md](docs/api-contract.md)

## 2. 开发环境约定


| 项          | 选定                                    | 备注                                                                     |
| ---------- | ------------------------------------- | ---------------------------------------------------------------------- |
| IDE        | Visual Studio 2026                    | —                                                                      |
| 目标框架       | **.NET 8(优先)** 或 .NET Framework 4.7.2 | .NET 8 与 AutoCAD 2025/2026 官方主路线一致;4.7.2 作为向下兼容备选                      |
| 语言         | C#                                    | —                                                                      |
| 目标 AutoCAD | 2025 / 2026                           | 选 .NET 8 时不需要兼容更老版本;选 4.7.2 时可向下兼容到 2024 及更早                           |
| 通信协议       | HTTP(localhost,默认端口 `54321`)          | .NET 8 用 ASP.NET Core Minimal API;4.7.2 用 ASP.NET Web API 2.1 SelfHost |


> **环境路线选择**
>
> - **推荐 .NET 8**:与 AutoCAD 2025/2026 官方一致,gileCAD R25 模板和 ADN-DevTech v2026 向导都已支持,开箱即用
> - **可选 4.7.2**:如果需要同时支持 AutoCAD 2024 及更早版本(那批仍跑 .NET Framework),用 4.7.2;或将来做多 TFM(`net472` + `net8.0-windows`)双发

---

## 3. 调研结论 —— 可复用的开源仓库

### 3.1 最贴近需求 —— 每个仓库对本项目的真实贡献

> 需求 A = 模糊定位(按文字找实体);需求 B = 视图缩放;需求 C = 包成 skill 的难度。
> ✅ = 现成可用 / 🟡 = 部分启发,需补完 / ❌ = 不覆盖


| 仓库                                                                                                      | 需求 A 模糊定位                                                                                                        | 需求 B 视图缩放           | .NET 8 / AutoCAD 2026 适配                                                                                                                      | skill 化难度                                                        | 推荐用法                                                                            |
| ------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| [luanshixia/AutoCADCodePack](https://github.com/luanshixia/AutoCADCodePack)                             | 🟡 没有现成的"按文字找实体",但 `QuickSelection` 的 LINQ 风格让自实现极简(一行 `.OfType<DBText>().Where(t => t.TextString.Contains(q))`) | ❌ 11 个模块里无 Zoom API | ❌ **2019 年停更**,最高仅到 R23 (AutoCAD 2019) + .NET 4.7.1。**不建议直接引用**,只学风格                                                                          | 中 — 本身不提供 HTTP/IPC,要自己加 server 层                                 | 不引用源码,**借鉴 QuickSelection 的 LINQ 选择器思路**                                        |
| [gileCAD/AutoCAD-Csharp-Project-Template](https://github.com/gileCAD/AutoCAD-Csharp-Project-Template) ⭐ | ❌ 模板不含业务功能                                                                                                       | ❌ 模板不含业务功能          | ✅ **R25 模板明确支持 AutoCAD 2025-2026 + .NET 8**;R24 模板覆盖 2024 及更早 (.NET Framework)                                                                | **简单** — 模板包含调试 launch profile(F5 直接起 AutoCAD 并 netload),省一周环境调试 | **首选脚手架**(.NET 8 路线):clone R25 模板,加 ASP.NET Core Minimal API + 业务 Controller 即可 |
| [ADN-DevTech/AutoCAD-Net-Wizards](https://github.com/ADN-DevTech/AutoCAD-Net-Wizards) ⭐                 | ❌ 向导不含业务功能                                                                                                       | ❌ 向导不含业务功能          | ✅ **v2026.0.0 (2025-07-07) 明确支持 AutoCAD 2026 + net8.0-windows**;预配 acdbmgd/acmgd/accoremgd 引用 + 多产品 launch profile(含 Civil 3D / Architecture) | **简单** — Autodesk 官方维护,VS 装上向导后右键 "新建 AutoCAD 插件项目"一键生成          | 与 gileCAD 二选一(此方案更"官方",gileCAD 更简洁)                                             |
| [CodeCavePro/autocad-sdk](https://github.com/CodeCavePro/autocad-sdk)                                   | ❌                                                                                                                | ❌                   | ⚠️ README 信息不全,仅是把 ObjectARX 互操作 DLL 打成 NuGet                                                                                                 | 不影响 skill 难度                                                     | **可选辅助** — 不想在客户机装 ObjectARX SDK 时,引用此包替代本机 DLL                                 |


**结论**:四个仓库里,**真正给本项目核心功能"省时间"的只有脚手架类(gileCAD R25 / ADN-DevTech v2026)**,它们解决"环境配置 + 项目骨架 + netload 调试"这一周的脏活,但**模糊定位和视图缩放仍需自己写**(代码量不大,§4.3 已给骨架)。

### 3.2 "AutoCAD 被外部调用"的现成方案参考


| 仓库 / 文章                                                                                                                                         | 对需求 A                                  | 对需求 B                                                                             | skill 化难度                                                                                                     | 推荐用法                                                                                                                                                                                                     |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [puran-water/autocad-mcp](https://github.com/puran-water/autocad-mcp) ⭐⭐                                                                        | 🟡 `entity.list` 能枚举,但无"按文字模糊"直接命令(需补) | ✅ **现成 `view.zoom_extents` / `view.zoom_window` / `view.get_screenshot`(PNG 截图)** | **极简(若可接受技术栈不一致)** — 已经是 MCP server 形态。原生支持 MCP 客户端 → 配置 1 条 `~/.openclaw/mcp.json` 即可让 LLM 直接调,**零 .NET 代码** | **如果可以混栈** → 直接拿来用,只在 Python 端补一个"按文字模糊查实体"的 LISP 函数;**如果坚持 .NET 8 单栈** → 只学它的工具切分(`view` / `entity` / `annotation` 三分法)                                                                                 |
| [Drive AutoCAD with Code — ASP.NET Web API 自承载](https://drive-cad-with-code.blogspot.com/2014/01/using-astnet-web-api-to-automate-autocad.html) | ❌                                      | ❌                                                                                 | 中 — 是博客示例不是仓库,要自己整理代码                                                                                         | **关键架构启发**:`IExtensionApplication` + 自承载 HTTP server + `HostApplicationServices.WorkingDatabase` + `DocumentLock` 这条主线必看;.NET 8 下把 `HttpSelfHostServer` 换成 ASP.NET Core `WebApplication.CreateBuilder()` |


### 3.3 写代码时的助手知识库

- [joseguiaCES/autocad-dotnet-claude-skill](https://github.com/joseguiaCES/autocad-dotnet-claude-skill) — 给 Claude Code 用的 skill,内置 AutoCAD 2025/2026 的 API 索引 + 20+ patterns + gotchas(线程/文档锁/版本差异)。不是 runtime,但能让写 .NET 插件时少踩坑。可以借鉴它的 `SKILL.md` 结构改造成 `huo15-openclaw-autocad-dev` skill

### 3.4 具体功能的现成代码片段

- **Zoom 到实体**:Kean Walmsley [Zooming to a window or entity inside AutoCAD with .NET](https://keanw.com/2008/06/zooming-to-a-wi.html) 给出完整可抄的 `ViewTableRecord` 设置法
- **按 handle 定位/缩放**:[Selecting or zooming to an entity based on its handle](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Selecting-or-zooming-to-an-entity-based-on-its-handle-identification-code.html) — 官方做法
- **模糊文字搜索**:没有现成库,但实现极简 —— 遍历 `BlockTableRecord` 拿 `DBText.TextString` / `MText.Contents`,用 .NET 的 `IndexOf(StringComparison.OrdinalIgnoreCase)` 或 `Regex`。参考 [Querying for entities across all BlockTableRecords](https://adndevblog.typepad.com/autocad/2012/05/querying-for-entities-across-all-blocktablerecords.html)

---

## 4. 推荐架构

```
┌──────────────────────────────┐         HTTP                  ┌──────────────────────────────┐
│  skill              │  ──────────────────────────►  │ AutoCAD.exe (主进程)         │
│ huo15-openclaw-autocad        │  POST /find?q=配电柜&fuzzy=1  │ ┌──────────────────────────┐ │
│                              │  GET  /zoom?handle=A3F        │ │ YourPlugin.dll (.NET 8)     │
│ 工具集:                       │  POST /zoom?factor=2          │ │ IExtensionApplication       │
│  - find_entity_by_name        │  ◄─── JSON 结果 ────────────  │ │ + ASP.NET Core Minimal API  │
│  - zoom_to_entity             │                                │ │   (或 4.7.2 + Web API 2.1)  │
│  - zoom_in / zoom_out         │                                │ └──────────────────────────┘ │
└──────────────────────────────┘                                └──────────────────────────────┘
                                                                  netload 一次,常驻
```

### 4.1 关键设计点

1. **走 HTTP self-host,不走 File IPC**
  既然选了 .NET,有标准的 HTTP server 选择(.NET 8 用 `WebApplication.CreateBuilder()` Minimal API;4.7.2 用 `HttpSelfHostServer`),不必学 puran-water 那套 `PostMessageW` 黑魔法。开发调试都更直观。
2. **关键坑 — HTTP server 非 UI 线程,拿不到 MdiActiveDocument**
  Drive-CAD 博客明确指出:`Application.DocumentManager.MdiActiveDocument` 在 HTTP 回调线程里拿不到。必须用:
3. **(OpenClaw)skill 调用方式**
   skill 通过 HTTP 访问插件。推荐流程：
   - `ensure_autocad_ready.ps1`：`/ping` 失败时启动 AutoCAD 并 `NETLOAD` 插件（Windows）
   - `open_dwg.ps1` / `open_dwg.sh`：用系统默认程序打开 DWG
   - 业务调用：`curl` 或 PowerShell 访问 `/find`、`/zoom/*`
4. **端口约定 & 服务发现**
  端口写进 skill 的 `SKILL.md`,允许用户改;启动时插件把端口落到 `~/.openclaw/kb/shared/wiki/autocad-runtime.md`,skill 读取后拼 URL。
5. **三个工具最小集**

  | 工具            | 入参                                                | 返回                                         |
  | ------------- | ------------------------------------------------- | ------------------------------------------ |
  | `find_entity` | `query: string, fuzzy: bool=true, layer?: string` | `[{handle, layer, text, position{x,y,z}}]` |
  | `zoom_to`     | `handle: string`                                  | `{ok, view: {center, width, height}}`      |
  | `zoom_by`     | `factor: float, center?: {x,y}`                   | `{ok, view: ...}`                          |


### 4.2 模糊匹配实现建议

- **第一级**:`string.IndexOf(StringComparison.OrdinalIgnoreCase)` 子串命中
- **第二级**:命中后用 Levenshtein / `Fastenshtein` 排序,取最相似 N 条
- **第三级**(可选):整体落库(SQLite FTS5)做离线索引,适合 10K+ 实体的大图

### 4.3 缩放视图核心代码骨架

```csharp
private static void ZoomWin(Editor ed, Point3d min, Point3d max)
{
    var min2d = new Point2d(min.X, min.Y);
    var max2d = new Point2d(max.X, max.Y);
    var view = new ViewTableRecord
    {
        CenterPoint = min2d + ((max2d - min2d) / 2.0),
        Height = max2d.Y - min2d.Y,
        Width  = max2d.X - min2d.X
    };
    ed.SetCurrentView(view);
}

[CommandMethod("ZE")]
public static void ZoomToEntity()
{
    var doc = Application.DocumentManager.MdiActiveDocument;
    var ed = doc.Editor;
    var per = ed.GetEntity("\nSelect entity: ");
    if (per.Status != PromptStatus.OK) return;
    using (var tr = doc.TransactionManager.StartTransaction())
    {
        var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
        var ext = ent.GeometricExtents;
        ZoomWin(ed, ext.MinPoint, ext.MaxPoint);
        tr.Commit();
    }
}
```

注意:Paperspace 场景需要补一行 `view.IsPaperspaceView = (!db.TileMode && db.PaperSpaceVportId == ed.CurrentViewportObjectId);`

---

## 5. 实施 Roadmap

> 默认 .NET 8 路线;括号内是 4.7.2 替代方案。


| 阶段                     | 内容                                                                                                                                                                            |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| M1 — 项目骨架              | clone `gileCAD/AutoCAD-Csharp-Project-Template` 的 R25 模板(.NET 8)或装 `ADN-DevTech/AutoCAD-Net-Wizards` v2026 用 VS 向导生成;F5 验证 `netload` 自动加载(4.7.2 路线:R24 模板 + TFM=`net472`)     |
| M2 — HTTP 服务           | .NET 8:`WebApplication.CreateBuilder()` Minimal API,在 `IExtensionApplication.Initialize()` 起;加 `/ping` 接口(4.7.2 路线:`Microsoft.AspNet.WebApi.SelfHost` + `HttpSelfHostServer`) |
| M3 — find_entity       | 遍历 ModelSpace + BlockReference,提取 `DBText.TextString` / `MText.Contents` / `Attribute` 文本,实现模糊匹配 + JSON 返回(借鉴 AutoCADCodePack 的 LINQ 选择器风格)                                   |
| M4 — zoom_to / zoom_by | 实现 `ViewTableRecord` 三种缩放(到实体 / 到窗口 / 按倍率),处理 Paperspace 分支                                                                                                                   |
| M5 — skill 封装          | 写 `huo15-openclaw-autocad` skill,`SKILL.md` 描述三个工具,curl 模板放 `scripts/`;参考 `joseguiaCES/autocad-dotnet-claude-skill` 的 SKILL.md 结构                                             |
| M6 — 文档与发版             | 双发(npm + ClawHub),写"如何安装插件 + netload + 启动 skill"的对接手册;按 [插件铁律](https://www.huo15.com) §11 自检清单跑一遍                                                                             |


---

## 5.1 skill 化难度评分(给选型参考)


| 路径                                                              | 难度       | 预估工作量                                        | 适用场景                                                                         |
| --------------------------------------------------------------- | -------- | -------------------------------------------- | ---------------------------------------------------------------------------- |
| **直接复用 `puran-water/autocad-mcp` + MCP 客户端**                    | 🟢 1/5   | 0.5 天 — 只需配置 `~/.openclaw/mcp.json` 一条 entry | 可以接受 **Python + AutoLISP** 技术栈,且只需要 zoom + 基本 CRUD;模糊查文字要在 Python 端补 LISP 函数 |
| `**gileCAD` R25 模板 + ASP.NET Core Minimal API + skill wrapper** | 🟡 2/5   | 3-5 天                                        | **推荐**:.NET 8 单栈,模板省脚手架,Minimal API 写 3 个 endpoint 极简,完全自控                   |
| `**ADN-DevTech` v2026 向导 + ASP.NET Core + skill wrapper**       | 🟡 2/5   | 3-5 天                                        | 与上方等价,VS 向导更"官方",生态扩展性好(可加 Civil 3D / Architecture 模板)                       |
| **4.7.2 + Web API 2.1 SelfHost + skill wrapper**                | 🟡 2.5/5 | 4-6 天                                        | 需要同时支持 AutoCAD 2024 及更早版本时选                                                  |
| **从零写 ObjectARX C++ + .NET 互操作**                                | 🔴 4/5   | 2-3 周                                        | 不建议,除非要做大量 native 性能优化                                                       |


**结论**:如果**死磕 .NET 8 单栈**,gileCAD R25 + ASP.NET Core Minimal API + skill wrapper 是最优解,3-5 天内 MVP;如果**愿意混栈**,直接挂 puran-water 的 MCP 是最快验证路径,半天就能让调通 zoom。

---

## 6. 几条避坑提醒

- **skill 启动策略**：允许通过 `skill/openclaw-autocad/scripts/start_autocad_with_plugin.ps1` 启动 AutoCAD 并 NETLOAD；**禁止**改注册表/启动项做永久注入
- **Linux**：可用 `xdg-open` 打开 DWG，但本插件仅支持 Windows + AutoCAD 2025；自动化须在 Windows 执行
- **(OpenClaw)插件铁律（仍适用）**:skill 检测端口不通时优先跑 `ensure_autocad_ready.ps1`,不要静默改系统配置
- **多文档场景**:`HostApplicationServices.WorkingDatabase` 默认绑当前活动文档,如要支持"指定文件名定位",HTTP 接口需要先 `DocumentManager.Open` 切换
- **AutoCAD 版本兼容**:`acdbmgd.dll` API 在大版本间会变,`CopyLocal=False` + 在用户机安装 AutoCAD 后引用本机 DLL 是稳妥做法
- **线程安全**:任何修改 Database 的操作必须包在 `DocumentLock` + `Transaction` 里,否则 AutoCAD 会崩

---

## 7. 参考资料

- [luanshixia/AutoCADCodePack](https://github.com/luanshixia/AutoCADCodePack)
- [gileCAD/AutoCAD-Csharp-Project-Template](https://github.com/gileCAD/AutoCAD-Csharp-Project-Template)
- [ADN-DevTech/AutoCAD-Net-Wizards](https://github.com/ADN-DevTech/AutoCAD-Net-Wizards)
- [CodeCavePro/autocad-sdk](https://github.com/CodeCavePro/autocad-sdk)
- [puran-water/autocad-mcp](https://github.com/puran-water/autocad-mcp)
- [Drive AutoCAD with Code: ASP.NET Web API 自承载方案](https://drive-cad-with-code.blogspot.com/2014/01/using-astnet-web-api-to-automate-autocad.html)
- [Kean Walmsley: Zooming to a window or entity with .NET](https://keanw.com/2008/06/zooming-to-a-wi.html)
- [Autodesk: Select/zoom to entity by handle](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Selecting-or-zooming-to-an-entity-based-on-its-handle-identification-code.html)
- [ADN DevBlog: Querying entities across all BlockTableRecords](https://adndevblog.typepad.com/autocad/2012/05/querying-for-entities-across-all-blocktablerecords.html)
- [joseguiaCES/autocad-dotnet-claude-skill](https://github.com/joseguiaCES/autocad-dotnet-claude-skill)
- [MadhukarMoogala/ArxNetCore (新版多 TFM 范例)](https://github.com/MadhukarMoogala/ArxNetCore)

---



**公司名称:** 青岛火一五信息科技有限公司

**联系邮箱:** [postmaster@huo15.com](mailto:postmaster@huo15.com) | **QQ群:** 1093992108

---

**关注逸寻智库公众号,获取更多资讯**





---


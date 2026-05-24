# AutoCAD SDK for 辉火云管家

> 把 AutoCAD 二次开发能力(模糊定位图元、缩放视图)封装为 HTTP 接口,
> 通过 skill 暴露给辉火云管家(OpenClaw)调用的工程参考与调研报告。

---

<div align="center">

<img src="https://tools.huo15.com/uploads/images/system/logo-colours.png" alt="火一五Logo" style="width: 120px; height: auto; display: inline; margin: 0;" />

</div>

<div align="center">

<h3>打破信息孤岛,用一套系统驱动企业增长</h3>
<h3>加速企业用户向全场景人工智能机器人转变</h3>

</div>
<div align="center">

| 🏫 教学机构 | 👨‍🏫 讲师 | 📧 联系方式         | 💬 QQ群      | 📺 配套视频                         |
|:-----------:|:--------:|:------------------:|:-----------:|:-----------------------------------:|
| 逸寻智库 | Job | support@huo15.com | 1093992108  | [📺 B站视频](https://space.bilibili.com/400418085) |

</div>

---

## 1. 项目目标

把 AutoCAD 的两个高频能力打成一个稳定的本地接口,供辉火云管家(OpenClaw)skill 调用:

1. **图元模糊定位** —— 给一个名称/关键字,在当前 DWG 图纸里找到对应的文字/标注/块,返回坐标与句柄(支持模糊搜索)
2. **视图缩放** —— 缩放到指定图元(等同 `ZE` 命令)、缩放到指定窗口、按倍率放大缩小、缩放到全图(`ZE`/`ZA`)

最终形态:**AutoCAD 内嵌 .NET 插件(常驻 HTTP 服务) + 辉火云管家 skill(发起 HTTP 调用)**。

## 2. 开发环境约定

| 项 | 选定 |
|---|---|
| IDE | Visual Studio 2026 |
| 目标框架 | .NET Framework 4.7.2 |
| 语言 | C# |
| 目标 AutoCAD | 2025 / 2026(可通过条件编译向下兼容) |
| 通信协议 | HTTP(localhost,端口由配置决定,默认 `54321`) |

> 注:AutoCAD 2026 官方主推 .NET 8 路线,本项目坚持 4.7.2 是为了与既有插件生态对齐。后续视需要再做多 TFM(`net472` + `net8.0`)双发。

---

## 3. 调研结论 —— 可复用的开源仓库

### 3.1 最贴近需求(可直接抄/裁剪)

| 仓库 | 价值 | 适配本项目 |
|---|---|---|
| [luanshixia/AutoCADCodePack](https://github.com/luanshixia/AutoCADCodePack) | LINQ 风格的 .NET 封装,11 个模块(`QuickSelection`/`Interaction`/`DbHelper` 等),可一行代码做"按类型/属性过滤实体" | ✅ 目标 .NET 4.7.1,跟 4.7.2 几乎一致 |
| [gileCAD/AutoCAD-Csharp-Project-Template](https://github.com/gileCAD/AutoCAD-Csharp-Project-Template) | 现成 VS C# 项目模板,省去手配 `accoremgd.dll`/`acdbmgd.dll`/`acmgd.dll` 引用 + `CopyLocal=False` | ⚠️ 新版默认 .NET 8,需把 TFM 改回 `net472` |
| [ADN-DevTech/AutoCAD-Net-Wizards](https://github.com/ADN-DevTech/AutoCAD-Net-Wizards) | Autodesk 官方 VS 向导(老但稳),生成 `IExtensionApplication` 骨架 | ✅ 支持 .NET 4.x |
| [CodeCavePro/autocad-sdk](https://github.com/CodeCavePro/autocad-sdk) | 把 ObjectARX 的 DLL 打成 NuGet,免装 ObjectARX SDK | ✅ |

### 3.2 "AutoCAD 被外部调用"的现成方案参考

| 仓库 / 文章 | 启发点 |
|---|---|
| [puran-water/autocad-mcp](https://github.com/puran-water/autocad-mcp) | **最接近本项目的最终形态**:把 AutoCAD 包成 MCP server,8 个工具(含 `view.zoom_extents`/`view.zoom_window`/`entity` CRUD)。走的是 **Python + AutoLISP + File IPC**(`PostMessageW(WM_CHAR)` 给 MDIClient 发键),不是 .NET。架构和工具切分思路可以直接借鉴 |
| [Drive AutoCAD with Code — ASP.NET Web API 自承载](https://drive-cad-with-code.blogspot.com/2014/01/using-astnet-web-api-to-automate-autocad.html) | **跟 .NET 4.7.2 路线最匹配**:`IExtensionApplication` 里 `HttpSelfHostServer.OpenAsync()`,controller 里 `HostApplicationServices.WorkingDatabase` 拿当前文档 + `DocumentLock`。.NET 4.5+ 即可 |

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
│ 辉火云管家 skill              │  ──────────────────────────►  │ AutoCAD.exe (主进程)         │
│ huo15-openclaw-autocad        │  POST /find?q=配电柜&fuzzy=1  │ ┌──────────────────────────┐ │
│                              │  GET  /zoom?handle=A3F        │ │ YourPlugin.dll (.NET 4.7.2) │
│ 工具集:                       │  POST /zoom?factor=2          │ │ IExtensionApplication       │
│  - find_entity_by_name        │  ◄─── JSON 结果 ────────────  │ │ + HttpSelfHostServer        │
│  - zoom_to_entity             │                                │ │   (Web API 2.1)             │
│  - zoom_in / zoom_out         │                                │ └──────────────────────────┘ │
└──────────────────────────────┘                                └──────────────────────────────┘
                                                                  netload 一次,常驻
```

### 4.1 关键设计点

1. **走 HTTP self-host,不走 File IPC**
   既然选了 .NET,有 `HttpSelfHostServer`(.NET 4.5+),不必学 puran-water 那套 `PostMessageW` 黑魔法。开发调试都更直观。

2. **关键坑 — HTTP server 非 UI 线程,拿不到 MdiActiveDocument**
   Drive-CAD 博客明确指出:`Application.DocumentManager.MdiActiveDocument` 在 HTTP 回调线程里拿不到。必须用:
   ```csharp
   Database db = HostApplicationServices.WorkingDatabase;
   Document doc = Application.DocumentManager.GetDocument(db);
   using (DocumentLock l = doc.LockDocument())
   {
       // 执行操作
   }
   ```

3. **辉火云管家(OpenClaw)skill 调用方式**
   skill 不能跨进程直调 AutoCAD,走 **return-cliCmd 模式**(参考辉火云管家插件铁律 §6.2:禁 `child_process`):输出 `curl http://localhost:54321/find?q=...`,或注册 HTTP fetch 工具让 LLM 调用。

4. **端口约定 & 服务发现**
   端口写进 skill 的 `SKILL.md`,允许用户改;启动时插件把端口落到 `~/.openclaw/kb/shared/wiki/autocad-runtime.md`,skill 读取后拼 URL。

5. **三个工具最小集**

   | 工具 | 入参 | 返回 |
   |---|---|---|
   | `find_entity` | `query: string, fuzzy: bool=true, layer?: string` | `[{handle, layer, text, position{x,y,z}}]` |
   | `zoom_to` | `handle: string` | `{ok, view: {center, width, height}}` |
   | `zoom_by` | `factor: float, center?: {x,y}` | `{ok, view: ...}` |

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

| 阶段 | 内容 |
|---|---|
| M1 — 项目骨架 | clone `gileCAD/AutoCAD-Csharp-Project-Template` → TFM 切回 `net472` → 验证 `netload` 加载 |
| M2 — HTTP 服务 | 集成 `Microsoft.AspNet.WebApi.SelfHost`,起 `HttpSelfHostServer`,加 `/ping` 接口 |
| M3 — find_entity | 遍历 ModelSpace + BlockReference,提取 DBText/MText/Attribute 文本,实现模糊匹配 + JSON 返回 |
| M4 — zoom_to / zoom_by | 实现 `ViewTableRecord` 三种缩放,处理 Paperspace 分支 |
| M5 — skill 封装 | 写 `huo15-openclaw-autocad` skill,`SKILL.md` 描述三个工具,curl 模板放 `scripts/` |
| M6 — 文档与发版 | 双发(npm + ClawHub),写"如何安装插件 + netload + 启动 skill"的对接手册 |

---

## 6. 几条避坑提醒

- **辉火云管家(OpenClaw)插件铁律 §6.2**:skill 这端**不能** `child_process.spawn("acad.exe")`;启动 AutoCAD 由用户做,skill 只通过 HTTP 通信
- **辉火云管家(OpenClaw)插件铁律 §6.4**:skill 检测端口不通时只提示用户 `netload YourPlugin.dll`,不要自动写注册表/启动项
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

<div align="center">

**公司名称:** 青岛火一五信息科技有限公司

**联系邮箱:** postmaster@huo15.com | **QQ群:** 1093992108

---

**关注逸寻智库公众号,获取更多资讯**

<img src="https://tools.huo15.com/uploads/images/system/qrcode_yxzk.jpg" alt="逸寻智库公众号二维码" style="width: 200px; height: auto; margin: 10px 0;" />

</div>

---

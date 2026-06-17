# CoalClaw CAD HTTP API 契约

版本：`apiVersion: "1"`（见 `/ping` 响应）

基础 URL：`http://127.0.0.1:54321`（可通过 `COALCLAW_HTTP_PORT` 或 `~/.openclaw/kb/shared/wiki/cad-runtime.md` 覆盖）

## GET /ping

**200**

```json
{
  "ok": true,
  "host": "autocad",
  "platform": "windows",
  "apiVersion": "1",
  "hostVersion": "AutoCAD 2025",
  "port": 54321,
  "threading": "threadfix-v5",
  "autocadVersion": "AutoCAD 2025"
}
```

`autocadVersion` 保留用于向后兼容；新客户端请读 `hostVersion`。

Linux 中望示例：`host: "zwcad"`, `platform: "linux"`, `hostVersion: "ZWCAD Linux 2026"`。

## GET /health

**200** — 有活动文档

```json
{
  "ok": true,
  "hasActiveDocument": true,
  "documentName": "C:\\drawings\\plan.dwg",
  "error": null
}
```

**200** — 无文档

```json
{
  "ok": false,
  "hasActiveDocument": false,
  "documentName": null,
  "error": "No working database is available."
}
```

## POST /document/open

**Request**

```json
{ "path": "/absolute/path/to/file.dwg" }
```

**200**

```json
{ "ok": true, "documentName": "/absolute/path/to/file.dwg" }
```

**400** — 文件不存在或打开失败

## GET /find

参数：

| 参数 | 必填 | 说明 |
|------|------|------|
| `q` 或 `query` | 是 | 搜索关键字 |
| `exact` | 否 | `true` 精确匹配 |
| `layer` | 否 | 图层过滤 |
| `limit` | 否 | 结果数上限，默认 `100`；`0` 表示不限 |

**200**

```json
{
  "matches": [
    {
      "handle": "A3F",
      "entityType": "DBText",
      "matchedField": "text",
      "matchedValue": "配电柜-01",
      "layer": "0",
      "position": { "x": 100.0, "y": 200.0, "z": 0.0 }
    }
  ],
  "count": 1,
  "total": 1,
  "truncated": false
}
```

`count` = 本次返回条数；`total` = 命中总数；`truncated` = 是否被 `limit` 截断。

## GET /zoom/to?handle=

**200**

```json
{
  "ok": true,
  "view": {
    "centerX": 100.0,
    "centerY": 200.0,
    "width": 50.0,
    "height": 30.0
  },
  "error": null
}
```

**404** — handle 不存在

### zoom 端点共同语义（重要）

- 缩放通过 CAD 命令队列**异步**执行：HTTP 200 表示命令已成功排队，**不保证**返回时缩放已完成；
- `view` 是按目标范围计算的**期望视图**（含 padding），不是从 CAD 实际回读的视图；`/zoom/by` 无目标范围，`view` 为 `null`；
- 连续 zoom 调用按命令队列顺序串行消化；调用后立即截屏可能仍是旧画面，需要确定性时序时请稍候或回读 `/health`。

## POST /zoom/by?factor=

参数：`factor`（必填）、`centerx`、`centery`（可选）

## GET /zoom/extents

缩放到全图范围。

## POST /zoom/window

**Request**

```json
{
  "min": { "x": 0, "y": 0 },
  "max": { "x": 100, "y": 100 }
}
```

## 错误码

| HTTP | 含义 |
|------|------|
| 400 | 参数错误或业务失败 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |
| 504 | UI 线程 / CAD 操作超时 |

## 扩展约定

- 新增端点应在此文档登记，并保持 camelCase JSON 字段
- 新增宿主通过 `/ping` 的 `host` / `platform` 区分，不改变现有端点语义

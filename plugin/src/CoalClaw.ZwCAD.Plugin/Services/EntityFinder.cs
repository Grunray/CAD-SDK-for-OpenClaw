using System.Globalization;
using System.Text.RegularExpressions;
using CoalClaw.Cad.Abstractions.Models;
using CoalClaw.ZwCAD.Plugin.Infrastructure;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace CoalClaw.ZwCAD.Plugin.Services;

public static class EntityFinder
{
    private static readonly Regex MTextFormatting = new(@"\{\\[^;]*;([^}]*)\}|\\[A-Za-z~|;][^;]*;|\\[Pp]", RegexOptions.Compiled);

    public static IReadOnlyList<FindMatchDto> Find(
        Database db,
        Transaction tr,
        string query,
        bool exact,
        string? layerFilter)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        var matches = new List<FindMatchDto>();
        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        // 块定义命中模板按 (query,exact,layerFilter) 缓存——这三者在单次 Find 内不变，故 cache/processing
        // 都是本次调用的局部状态，绝不跨调用复用。ModelSpace 是顶层、只遍历一次，不进缓存。
        var cache = new Dictionary<ObjectId, IReadOnlyList<FindMatchDto>>();
        var processing = new HashSet<ObjectId>();
        CollectInto(tr, modelSpace, query, exact, layerFilter, ZwCadGeometryCompat.IdentityMatrix, matches, cache, processing);
        return matches;
    }

    // 遍历一个容器，把命中以 transform 变换后写入 output。
    // 与旧的逐实例深度递归相比：块定义子树只遍历一次（GetBlockDefinitionTemplates 记忆化），
    // 重复引用的标准块（图框/图例/设备符号）成本从 O(实例数×块内实体数) 降到 O(块内实体数 + 实例数×命中数)。
    private static void CollectInto(
        Transaction tr,
        BlockTableRecord record,
        string query,
        bool exact,
        string? layerFilter,
        Matrix3d transform,
        List<FindMatchDto> output,
        Dictionary<ObjectId, IReadOnlyList<FindMatchDto>> cache,
        HashSet<ObjectId> processing)
    {
        foreach (ObjectId objectId in record)
        {
            if (objectId.IsNull || !objectId.IsValid)
                continue;

            var entity = tr.GetObject(objectId, OpenMode.ForRead, false) as Entity;
            if (entity == null)
                continue;

            if (!string.IsNullOrWhiteSpace(layerFilter) &&
                !string.Equals(entity.Layer, layerFilter, StringComparison.OrdinalIgnoreCase))
            {
                // 块引用即便自身图层不匹配也要进定义体找内部匹配项；其余实体直接跳过
                if (entity is not BlockReference)
                    continue;
            }

            // 实例级内容：blockRef.Name 与挂在本实例上的属性（per-instance），用当前 transform
            CollectMatches(tr, entity, query, exact, layerFilter, output, transform);

            if (entity is BlockReference blockRef)
            {
                var templates = GetBlockDefinitionTemplates(tr, blockRef.BlockTableRecord, query, exact, layerFilter, cache, processing);
                if (templates.Count == 0)
                    continue;

                // 模板坐标在块定义系下；矩阵结合律保证 p.TransformBy(inner).TransformBy(outer)
                // == p.TransformBy(inner.PreMultiplyBy(outer))，故展平缓存与逐层递归结果一致。
                var instanceTransform = blockRef.BlockTransform.PreMultiplyBy(transform);
                foreach (var dto in templates)
                {
                    var rel = new Point3d(dto.Position.X, dto.Position.Y, dto.Position.Z);
                    var abs = rel.TransformBy(instanceTransform);
                    output.Add(dto with { Position = new Point3Dto(abs.X, abs.Y, abs.Z) });
                }
            }
        }
    }

    // 块定义子树在该定义坐标系下的命中模板（递归展平、记忆化）。
    // 缓存内容仅含定义级数据：块内 DBText/MText/Dimension、嵌套块名、以及定义内嵌套块引用挂带的属性——
    // 这些对该定义的所有插入实例物理同一份，故可安全重用；真正 per-instance 的属性只在顶层 ModelSpace
    // 持有者上，由 CollectInto 直接读取、不走此缓存。
    private static IReadOnlyList<FindMatchDto> GetBlockDefinitionTemplates(
        Transaction tr,
        ObjectId blockDefId,
        string query,
        bool exact,
        string? layerFilter,
        Dictionary<ObjectId, IReadOnlyList<FindMatchDto>> cache,
        HashSet<ObjectId> processing)
    {
        if (cache.TryGetValue(blockDefId, out var cached))
            return cached;

        // 损坏文件可能含循环块引用：处理中再次遇到则返回空，避免无限递归（旧实现没有这层防护）
        if (!processing.Add(blockDefId))
            return Array.Empty<FindMatchDto>();

        var template = new List<FindMatchDto>();
        if (tr.GetObject(blockDefId, OpenMode.ForRead) is BlockTableRecord record)
            CollectInto(tr, record, query, exact, layerFilter, ZwCadGeometryCompat.IdentityMatrix, template, cache, processing);

        processing.Remove(blockDefId);
        cache[blockDefId] = template;
        return template;
    }

    private static void CollectMatches(
        Transaction tr,
        Entity entity,
        string query,
        bool exact,
        string? layerFilter,
        List<FindMatchDto> matches,
        Matrix3d transform)
    {
        if (!string.IsNullOrWhiteSpace(layerFilter) &&
            !string.Equals(entity.Layer, layerFilter, StringComparison.OrdinalIgnoreCase))
            return;

        switch (entity)
        {
            case AttributeReference attribute:
                TryAddMatch(matches, entity, "attribute", attribute.TextString, () => attribute.Position.TransformBy(transform), query, exact);
                break;

            case DBText dbText:
                TryAddMatch(matches, entity, "text", dbText.TextString, () => dbText.Position.TransformBy(transform), query, exact);
                break;

            case MText mText:
                TryAddMatch(matches, entity, "text", GetPlainMText(mText), () => mText.Location.TransformBy(transform), query, exact);
                break;

            case BlockReference blockRef:
                TryAddMatch(matches, entity, "blockName", blockRef.Name, () => GetEntityPosition(blockRef, transform), query, exact);
                foreach (ObjectId attributeId in blockRef.AttributeCollection)
                {
                    if (attributeId.IsNull)
                        continue;

                    var attr = tr.GetObject(attributeId, OpenMode.ForRead, false) as AttributeReference;
                    if (attr == null)
                        continue;

                    TryAddMatch(matches, attr, "attribute", attr.TextString, () => attr.Position.TransformBy(transform), query, exact);
                }
                break;

            case Dimension dimension:
                var dimensionText = GetDimensionText(dimension);
                TryAddMatch(matches, entity, "dimensionText", dimensionText, () => GetEntityPosition(dimension, transform), query, exact);
                var measurement = dimension.Measurement.ToString(CultureInfo.InvariantCulture);
                TryAddMatch(matches, entity, "dimensionValue", measurement, () => GetEntityPosition(dimension, transform), query, exact);
                break;
        }

        // 不把图层名当匹配内容：query 撞上常见图层名（如 "0"）会让整层实体全部命中。
        // 按图层筛选请用 layer 查询参数。
    }

    private static void TryAddMatch(
        List<FindMatchDto> matches,
        Entity entity,
        string matchedField,
        string? candidate,
        Func<Point3d> positionFactory,
        string query,
        bool exact)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        if (!IsMatch(candidate, query, exact))
            return;

        // position 延迟求值：GeometricExtents 相对昂贵，只为真正命中的实体计算
        var position = positionFactory();
        matches.Add(new FindMatchDto(
            Handle: entity.Handle.ToString(),
            EntityType: entity.GetType().Name,
            MatchedField: matchedField,
            MatchedValue: candidate,
            Layer: entity.Layer,
            Position: new Point3Dto(position.X, position.Y, position.Z)));
    }

    private static bool IsMatch(string candidate, string query, bool exact)
    {
        return exact
            ? string.Equals(candidate, query, StringComparison.OrdinalIgnoreCase)
            : candidate.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPlainMText(MText mText)
    {
        var text = string.IsNullOrWhiteSpace(mText.Text) ? mText.Contents : mText.Text;
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = MTextFormatting.Replace(text, "$1");
        return text.Replace("\\P", " ").Replace("\\p", " ").Trim();
    }

    private static string GetDimensionText(Dimension dimension)
    {
        var text = dimension.DimensionText;
        if (string.IsNullOrWhiteSpace(text) || text == "<>")
            return dimension.Measurement.ToString(CultureInfo.InvariantCulture);

        return text;
    }

    private static Point3d GetEntityPosition(Entity entity, Matrix3d transform)
    {
        try
        {
            var ext = entity.GeometricExtents;
            var center = ext.MinPoint + ((ext.MaxPoint - ext.MinPoint) * 0.5);
            return center.TransformBy(transform);
        }
        catch
        {
            return ZwCadGeometryCompat.OriginPoint.TransformBy(transform);
        }
    }
}

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

        SearchBlockTableRecord(tr, modelSpace, query, exact, layerFilter, matches, ZwCadGeometryCompat.IdentityMatrix);
        return matches;
    }

    private static void SearchBlockTableRecord(
        Transaction tr,
        BlockTableRecord blockRecord,
        string query,
        bool exact,
        string? layerFilter,
        List<FindMatchDto> matches,
        Matrix3d transform)
    {
        foreach (ObjectId objectId in blockRecord)
        {
            if (objectId.IsNull || !objectId.IsValid)
                continue;

            var entity = tr.GetObject(objectId, OpenMode.ForRead, false) as Entity;
            if (entity == null)
                continue;

            if (!string.IsNullOrWhiteSpace(layerFilter) &&
                !string.Equals(entity.Layer, layerFilter, StringComparison.OrdinalIgnoreCase))
            {
                if (entity is not BlockReference)
                    continue;
            }

            CollectMatches(tr, entity, query, exact, layerFilter, matches, transform);

            if (entity is BlockReference blockRef)
            {
                var nestedRecord = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
                SearchBlockTableRecord(
                    tr,
                    nestedRecord,
                    query,
                    exact,
                    layerFilter,
                    matches,
                    blockRef.BlockTransform.PreMultiplyBy(transform));
            }
        }
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

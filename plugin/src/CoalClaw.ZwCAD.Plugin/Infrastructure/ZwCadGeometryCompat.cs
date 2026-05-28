using ZwSoft.ZwCAD.Geometry;

namespace CoalClaw.ZwCAD.Plugin.Infrastructure;

/// <summary>
/// 中望 CAD 2026 与 AutoCAD 几何 API 差异适配。
/// 2026 起 Matrix3d.Identity / Point3d.Origin 等不再为 static，需经实例访问或显式构造。
/// </summary>
internal static class ZwCadGeometryCompat
{
    internal static Matrix3d IdentityMatrix => new Matrix3d().Identity;

    internal static Point3d OriginPoint => new Point3d(0, 0, 0);
}

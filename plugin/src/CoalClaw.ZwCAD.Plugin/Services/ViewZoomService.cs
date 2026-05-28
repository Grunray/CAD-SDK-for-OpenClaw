using CoalClaw.Cad.Abstractions.Models;
using CoalClaw.Cad.Core.Services;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace CoalClaw.ZwCAD.Plugin.Services;

public static class ViewZoomService
{
    public static (Point3d Min, Point3d Max) GetPaddedExtentsForHandle(Database db, Transaction tr, string handleHex)
    {
        var objectId = ResolveHandle(db, tr, handleHex);
        var entity = (Entity)tr.GetObject(objectId, OpenMode.ForRead);
        return PadExtents(entity.GeometricExtents);
    }

    public static (Point3d Min, Point3d Max) GetDrawingExtents(Database db)
    {
        if (db.Extmin.DistanceTo(db.Extmax) <= ZoomCommandBuilder.MinViewSize)
            return (new Point3d(-100, -100, 0), new Point3d(100, 100, 0));

        return PadExtents(new Extents3d(db.Extmin, db.Extmax));
    }

    public static string BuildZoomWindowCommand(Point3d min, Point3d max)
    {
        return ZoomCommandBuilder.BuildZoomWindowCommand(min.X, min.Y, max.X, max.Y);
    }

    public static string BuildZoomExtentsCommand()
    {
        return ZoomCommandBuilder.BuildZoomExtentsCommand();
    }

    public static string BuildZoomScaleCommand(double factor, double? centerX, double? centerY)
    {
        return ZoomCommandBuilder.BuildZoomScaleCommand(factor, centerX, centerY);
    }

    public static void SendZoomCommand(Document doc, string command)
    {
        doc.SendStringToExecute(command, true, false, false);
    }

    public static ZoomResponse ResponseFromExtents(Point3d min, Point3d max)
    {
        var view = ZoomCommandBuilder.BuildViewState(min.X, min.Y, max.X, max.Y);
        return new ZoomResponse(Ok: true, View: view, Error: null);
    }

    public static ZoomResponse ResponseAcknowledged()
    {
        return new ZoomResponse(Ok: true, View: null, Error: null);
    }

    public static ObjectId ResolveHandle(Database db, Transaction tr, string handleHex)
    {
        if (string.IsNullOrWhiteSpace(handleHex))
            throw new ArgumentException("Handle is required.", nameof(handleHex));

        var handleValue = Convert.ToInt64(handleHex.Trim(), 16);
        var handle = new Handle(handleValue);
        if (!db.TryGetObjectId(handle, out var objectId) || objectId.IsNull)
            throw new KeyNotFoundException($"Entity with handle '{handleHex}' was not found.");

        tr.GetObject(objectId, OpenMode.ForRead);
        return objectId;
    }

    private static (Point3d Min, Point3d Max) PadExtents(Extents3d ext)
    {
        var padded = ZoomCommandBuilder.PadExtents(
            ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y, ext.MinPoint.Z);
        return (new Point3d(padded.MinX, padded.MinY, ext.MinPoint.Z),
            new Point3d(padded.MaxX, padded.MaxY, ext.MaxPoint.Z));
    }
}

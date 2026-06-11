using CoalClaw.Cad.Abstractions;
using CoalClaw.Cad.Abstractions.Models;
using CoalClaw.ZwCAD.Plugin.Services;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace CoalClaw.ZwCAD.Plugin.Infrastructure;

public sealed class ZwCadHostContext : ICadHostContext
{
    public const string ApiThreadingVersion = "threadfix-v2";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public CadHostMetadata Metadata { get; }

    public ZwCadHostContext(int port)
    {
        Metadata = new CadHostMetadata(
            HostId: "zwcad",
            Platform: "linux",
            HostVersion: "ZWCAD Linux 2026",
            ApiThreadingVersion: ApiThreadingVersion,
            Port: port);
    }

    public bool TryGetDocumentInfo(out string? documentName, out string? error)
    {
        documentName = null;
        error = null;

        try
        {
            var name = ZwCadUiContext.Run(() =>
            {
                var db = HostApplicationServices.WorkingDatabase;
                if (db == null)
                    return null;

                return string.IsNullOrWhiteSpace(db.Filename) ? "(unsaved)" : db.Filename;
            }, DefaultTimeout);

            if (name == null)
            {
                error = "No working database is available.";
                return false;
            }

            documentName = name;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public IReadOnlyList<FindMatchDto> FindEntities(string query, bool exact, string? layer)
    {
        return RunInDatabase((db, tr) => EntityFinder.Find(db, tr, query, exact, layer));
    }

    public ZoomResponse ZoomToHandle(string handle)
    {
        var (min, max) = RunInDatabase((db, tr) =>
            ViewZoomService.GetPaddedExtentsForHandle(db, tr, handle));

        var cmd = ViewZoomService.BuildZoomWindowCommand(min, max);
        return RunZoomCommand(cmd, min, max);
    }

    public ZoomResponse ZoomExtents()
    {
        var (min, max) = RunInDatabase((db, _) => ViewZoomService.GetDrawingExtents(db));
        var cmd = ViewZoomService.BuildZoomExtentsCommand();
        return RunZoomCommand(cmd, min, max);
    }

    public ZoomResponse ZoomBy(double factor, double? centerX, double? centerY)
    {
        var cmd = ViewZoomService.BuildZoomScaleCommand(factor, centerX, centerY);
        return RunZoomCommand(cmd);
    }

    public ZoomResponse ZoomWindow(double minX, double minY, double maxX, double maxY)
    {
        var min = new Point3d(minX, minY, 0);
        var max = new Point3d(maxX, maxY, 0);
        var cmd = ViewZoomService.BuildZoomWindowCommand(min, max);
        return RunZoomCommand(cmd, min, max);
    }

    public OpenDocumentResult OpenDocument(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            return new OpenDocumentResult(false, null, $"File not found: {fullPath}");

        return ZwCadUiContext.Run(() =>
        {
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                if (string.Equals(doc.Name, fullPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(doc.Database.Filename, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    dm.MdiActiveDocument = doc;
                    return new OpenDocumentResult(true, fullPath, null);
                }
            }

            var opened = dm.Open(fullPath, false);
            dm.MdiActiveDocument = opened;
            return new OpenDocumentResult(true, fullPath, null);
        }, DefaultTimeout);
    }

    public void LogError(string message)
    {
        try
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage($"\n[CoalClaw] {message}\n");
        }
        catch { }
    }

    private static T RunInDatabase<T>(Func<Database, Transaction, T> action)
    {
        // Database 读取同样必须经 UI/应用上下文：HTTP 工作线程直接开 Transaction
        // 会与用户正在进行的编辑/文档切换竞争（与 OpenDocument / RunZoomCommand 同一纪律）
        return ZwCadUiContext.Run(() =>
        {
            var db = HostApplicationServices.WorkingDatabase
                     ?? throw new InvalidOperationException("No working database.");

            using var tr = db.TransactionManager.StartTransaction();
            try
            {
                var result = action(db, tr);
                tr.Commit();
                return result;
            }
            catch
            {
                tr.Abort();
                throw;
            }
        }, DefaultTimeout);
    }

    private static ZoomResponse RunZoomCommand(string command, Point3d? viewMin = null, Point3d? viewMax = null)
    {
        return ZwCadUiContext.Run(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument
                      ?? throw new InvalidOperationException("No active document is open.");

            ViewZoomService.SendZoomCommand(doc, command);

            if (viewMin.HasValue && viewMax.HasValue)
                return ViewZoomService.ResponseFromExtents(viewMin.Value, viewMax.Value);

            return ViewZoomService.ResponseAcknowledged();
        }, DefaultTimeout);
    }
}

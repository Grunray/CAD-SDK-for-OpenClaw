using System.Globalization;
using CoalClaw.Cad.Abstractions;
using CoalClaw.Cad.Abstractions.Models;
using CoalClaw.Cad.Core.Json;

namespace CoalClaw.Cad.Core.Api;

public sealed class ApiRouter
{
    private readonly ICadHostContext _host;

    public ApiRouter(ICadHostContext host)
    {
        _host = host;
    }

    public (int Status, string Body) Route(string method, string path, IReadOnlyDictionary<string, string> query, string body)
    {
        try
        {
            if (path == "/ping" && method == "GET")
                return (200, SimpleJson.Ping(_host.Metadata));

            if (path == "/health" && method == "GET")
                return Health();

            if (path == "/document/open" && method == "POST")
                return OpenDocument(body);

            if (path == "/find" && method == "GET")
            {
                var q = query.GetValueOrDefault("q") ?? query.GetValueOrDefault("query") ?? "";
                var exact = query.GetValueOrDefault("exact")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
                var layer = query.GetValueOrDefault("layer");
                return FindEntity(q, exact, layer);
            }

            if (path.StartsWith("/zoom/to", StringComparison.Ordinal) && method == "GET")
            {
                var handle = query.GetValueOrDefault("handle") ?? "";
                return ZoomTo(handle);
            }

            if (path == "/zoom/extents" && method == "GET")
                return ZoomExtents();

            if (path == "/zoom/by" && (method == "GET" || method == "POST"))
            {
                if (!double.TryParse(query.GetValueOrDefault("factor") ?? "1", NumberStyles.Any, CultureInfo.InvariantCulture, out var factor))
                    return (400, SimpleJson.Err("Invalid 'factor' parameter."));

                double? cx = null, cy = null;
                if (double.TryParse(query.GetValueOrDefault("centerx"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cxVal)) cx = cxVal;
                if (double.TryParse(query.GetValueOrDefault("centery"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cyVal)) cy = cyVal;

                return ZoomBy(factor, cx, cy);
            }

            if (path == "/zoom/window" && method == "POST")
            {
                if (string.IsNullOrWhiteSpace(body))
                    return (400, SimpleJson.Err("Request body is required for /zoom/window."));

                if (!TryParseZoomWindow(body, out var minX, out var minY, out var maxX, out var maxY))
                    return (400, SimpleJson.Err("Body must contain 'min' and 'max' with x,y properties."));

                return ZoomWindow(minX, minY, maxX, maxY);
            }
        }
        catch (Exception ex)
        {
            return (500, SimpleJson.Err(ex.Message));
        }

        return (404, SimpleJson.Err("Not found"));
    }

    private (int Status, string Body) Health()
    {
        if (!_host.TryGetDocumentInfo(out var name, out var error))
            return (200, SimpleJson.Health(false, false, null, error));

        return (200, SimpleJson.Health(true, true, name, null));
    }

    private (int Status, string Body) OpenDocument(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (400, SimpleJson.Err("Request body is required for /document/open."));

        var path = SimpleJson.ExtractString(body, "path");
        if (string.IsNullOrWhiteSpace(path))
            return (400, SimpleJson.Err("Field 'path' is required."));

        try
        {
            var result = _host.OpenDocument(path);
            if (!result.Ok)
                return (400, SimpleJson.ErrObj(result.Error ?? "Failed to open document."));

            return (200, SimpleJson.OpenOk(result.DocumentName ?? path));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.ErrObj(ex.Message));
        }
    }

    private (int Status, string Body) FindEntity(string query, bool exact, string? layer)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (400, SimpleJson.Err("Query parameter 'q' is required."));

        try
        {
            var matches = _host.FindEntities(query, exact, layer);
            return (200, SimpleJson.Find(matches));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.Err(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return (400, SimpleJson.Err(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.Err(ex.Message));
        }
    }

    private (int Status, string Body) ZoomTo(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return (400, SimpleJson.Err("Handle parameter is required."));

        try
        {
            return (200, SimpleJson.Zoom(_host.ZoomToHandle(handle)));
        }
        catch (KeyNotFoundException ex)
        {
            return (404, SimpleJson.ErrObj(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.ErrObj(ex.Message));
        }
    }

    private (int Status, string Body) ZoomExtents()
    {
        try
        {
            return (200, SimpleJson.Zoom(_host.ZoomExtents()));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.ErrObj(ex.Message));
        }
    }

    private (int Status, string Body) ZoomBy(double factor, double? centerX, double? centerY)
    {
        try
        {
            return (200, SimpleJson.Zoom(_host.ZoomBy(factor, centerX, centerY)));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.ErrObj(ex.Message));
        }
    }

    private (int Status, string Body) ZoomWindow(double minX, double minY, double maxX, double maxY)
    {
        try
        {
            return (200, SimpleJson.Zoom(_host.ZoomWindow(minX, minY, maxX, maxY)));
        }
        catch (InvalidOperationException ex)
        {
            return (400, SimpleJson.ErrObj(ex.Message));
        }
        catch (TimeoutException ex)
        {
            return (504, SimpleJson.ErrObj(ex.Message));
        }
    }

    private static bool TryParseZoomWindow(string body, out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = minY = maxX = maxY = 0;
        var minIdx = body.IndexOf("\"min\"", StringComparison.OrdinalIgnoreCase);
        var maxIdx = body.IndexOf("\"max\"", StringComparison.OrdinalIgnoreCase);
        if (minIdx < 0 || maxIdx < 0) return false;

        if (!SimpleJson.TryExtractDouble(body.Substring(minIdx, Math.Min(120, body.Length - minIdx)), "x", out minX)) return false;
        if (!SimpleJson.TryExtractDouble(body.Substring(minIdx, Math.Min(120, body.Length - minIdx)), "y", out minY)) return false;
        if (!SimpleJson.TryExtractDouble(body.Substring(maxIdx, Math.Min(120, body.Length - maxIdx)), "x", out maxX)) return false;
        if (!SimpleJson.TryExtractDouble(body.Substring(maxIdx, Math.Min(120, body.Length - maxIdx)), "y", out maxY)) return false;
        return true;
    }
}

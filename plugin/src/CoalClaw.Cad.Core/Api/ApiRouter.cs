using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using CoalClaw.Cad.Abstractions;
using CoalClaw.Cad.Abstractions.Models;

namespace CoalClaw.Cad.Core.Api;

public sealed class ApiRouter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ICadHostContext _host;

    public ApiRouter(ICadHostContext host)
    {
        _host = host;
    }

    public (int Status, object? Body) Route(string method, string path, IReadOnlyDictionary<string, string> query, string body)
    {
        try
        {
            if (path == "/ping" && method == "GET")
                return Ping();

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
                    return (400, new { error = "Invalid 'factor' parameter." });

                double? cx = null, cy = null;
                if (double.TryParse(query.GetValueOrDefault("centerx"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cxVal)) cx = cxVal;
                if (double.TryParse(query.GetValueOrDefault("centery"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cyVal)) cy = cyVal;

                return ZoomBy(factor, cx, cy);
            }

            if (path == "/zoom/window" && method == "POST")
            {
                if (string.IsNullOrWhiteSpace(body))
                    return (400, new { error = "Request body is required for /zoom/window." });

                var win = JsonSerializer.Deserialize<ZoomWindowBody>(body, JsonOpts);
                if (win?.Min == null || win?.Max == null)
                    return (400, new { error = "Body must contain 'min' and 'max' with x,y properties." });

                return ZoomWindow(win.Min, win.Max);
            }
        }
        catch (Exception ex)
        {
            return (500, new { error = ex.Message });
        }

        return (404, new { error = "Not found" });
    }

    private (int Status, object? Body) Ping()
    {
        var meta = _host.Metadata;
        return (200, new
        {
            ok = true,
            host = meta.HostId,
            platform = meta.Platform,
            apiVersion = meta.ApiVersion,
            hostVersion = meta.HostVersion,
            port = meta.Port,
            threading = meta.ApiThreadingVersion,
            autocadVersion = meta.HostVersion
        });
    }

    private (int Status, object? Body) Health()
    {
        if (!_host.TryGetDocumentInfo(out var name, out var error))
        {
            return (200, new { ok = false, hasActiveDocument = false, documentName = (string?)null, error });
        }

        return (200, new { ok = true, hasActiveDocument = true, documentName = name, error = (string?)null });
    }

    private (int Status, object? Body) OpenDocument(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (400, new { error = "Request body is required for /document/open." });

        OpenDocumentRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<OpenDocumentRequest>(body, JsonOpts);
        }
        catch
        {
            return (400, new { error = "Invalid JSON body." });
        }

        if (string.IsNullOrWhiteSpace(req?.Path))
            return (400, new { error = "Field 'path' is required." });

        try
        {
            var result = _host.OpenDocument(req.Path);
            if (!result.Ok)
                return (400, new { ok = false, error = result.Error ?? "Failed to open document." });

            return (200, new { ok = true, documentName = result.DocumentName });
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { ok = false, error = ex.Message });
        }
    }

    private (int Status, object? Body) FindEntity(string query, bool exact, string? layer)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (400, new { error = "Query parameter 'q' is required." });

        try
        {
            var matches = _host.FindEntities(query, exact, layer);
            return (200, new { matches, count = matches.Count });
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return (400, new { error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { error = ex.Message });
        }
    }

    private (int Status, object? Body) ZoomTo(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return (400, new { error = "Handle parameter is required." });

        try
        {
            return (200, _host.ZoomToHandle(handle));
        }
        catch (KeyNotFoundException ex)
        {
            return (404, new { ok = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { ok = false, error = ex.Message });
        }
    }

    private (int Status, object? Body) ZoomExtents()
    {
        try
        {
            return (200, _host.ZoomExtents());
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { ok = false, error = ex.Message });
        }
    }

    private (int Status, object? Body) ZoomBy(double factor, double? centerX, double? centerY)
    {
        try
        {
            return (200, _host.ZoomBy(factor, centerX, centerY));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { ok = false, error = ex.Message });
        }
    }

    private (int Status, object? Body) ZoomWindow(Point2Dto min, Point2Dto max)
    {
        try
        {
            return (200, _host.ZoomWindow(min.X, min.Y, max.X, max.Y));
        }
        catch (InvalidOperationException ex)
        {
            return (400, new { ok = false, error = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return (504, new { ok = false, error = ex.Message });
        }
    }

    private sealed class OpenDocumentRequest
    {
        public string? Path { get; set; }
    }

    private sealed class ZoomWindowBody
    {
        public Point2Dto? Min { get; set; }
        public Point2Dto? Max { get; set; }
    }
}

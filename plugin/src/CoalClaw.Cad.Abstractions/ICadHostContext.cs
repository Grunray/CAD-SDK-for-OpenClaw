using System.Collections.Generic;
using CoalClaw.Cad.Abstractions.Models;

namespace CoalClaw.Cad.Abstractions;

public interface ICadHostContext
{
    CadHostMetadata Metadata { get; }

    bool TryGetDocumentInfo(out string? documentName, out string? error);

    IReadOnlyList<FindMatchDto> FindEntities(string query, bool exact, string? layer);

    ZoomResponse ZoomToHandle(string handle);

    ZoomResponse ZoomExtents();

    ZoomResponse ZoomBy(double factor, double? centerX, double? centerY);

    ZoomResponse ZoomWindow(double minX, double minY, double maxX, double maxY);

    OpenDocumentResult OpenDocument(string path);

    void LogError(string message);
}

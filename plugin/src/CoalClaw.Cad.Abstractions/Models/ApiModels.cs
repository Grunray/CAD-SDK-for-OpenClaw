namespace CoalClaw.Cad.Abstractions.Models;

public sealed record Point3Dto(double X, double Y, double Z);

public sealed record Point2Dto(double X, double Y);

public sealed record FindMatchDto(
    string Handle,
    string EntityType,
    string MatchedField,
    string MatchedValue,
    string Layer,
    Point3Dto Position);

public sealed record ViewStateDto(double CenterX, double CenterY, double Width, double Height);

public sealed record ZoomResponse(bool Ok, ViewStateDto? View, string? Error);

public sealed record OpenDocumentResult(bool Ok, string? DocumentName, string? Error);

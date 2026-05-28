using System;
using System.Globalization;

namespace CoalClaw.Cad.Core.Services;

public static class ZoomCommandBuilder
{
    public const double MinViewSize = 1e-6;
    public const double PaddingFactor = 1.1;

    public static string BuildZoomWindowCommand(double minX, double minY, double maxX, double maxY)
    {
        var min2dX = Math.Min(minX, maxX);
        var min2dY = Math.Min(minY, maxY);
        var max2dX = Math.Max(minX, maxX);
        var max2dY = Math.Max(minY, maxY);
        return string.Format(
            CultureInfo.InvariantCulture,
            "\x03\x03_.ZOOM _W {0},{1} {2},{3} ",
            min2dX, min2dY, max2dX, max2dY);
    }

    public static string BuildZoomExtentsCommand()
    {
        return "\x03\x03_.ZOOM _E ";
    }

    public static string BuildZoomScaleCommand(double factor, double? centerX, double? centerY)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Factor must be greater than zero.");

        if (centerX.HasValue && centerY.HasValue)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\x03\x03_.ZOOM _S {0} {1},{2} ",
                factor, centerX.Value, centerY.Value);
        }

        return string.Format(CultureInfo.InvariantCulture, "\x03\x03_.ZOOM _S {0} ", factor);
    }

    public static (double MinX, double MinY, double MaxX, double MaxY) PadExtents(
        double minX, double minY, double maxX, double maxY, double minZ = 0)
    {
        var cx = (minX + maxX) * 0.5;
        var cy = (minY + maxY) * 0.5;
        var halfW = Math.Max((maxX - minX) * PaddingFactor * 0.5, MinViewSize);
        var halfH = Math.Max((maxY - minY) * PaddingFactor * 0.5, MinViewSize);
        return (cx - halfW, cy - halfH, cx + halfW, cy + halfH);
    }

    public static Abstractions.Models.ViewStateDto BuildViewState(
        double minX, double minY, double maxX, double maxY)
    {
        var min2dX = Math.Min(minX, maxX);
        var min2dY = Math.Min(minY, maxY);
        var max2dX = Math.Max(minX, maxX);
        var max2dY = Math.Max(minY, maxY);
        var width = Math.Max((max2dX - min2dX) * PaddingFactor, MinViewSize);
        var height = Math.Max((max2dY - min2dY) * PaddingFactor, MinViewSize);
        var centerX = min2dX + ((max2dX - min2dX) * 0.5);
        var centerY = min2dY + ((max2dY - min2dY) * 0.5);
        return new Abstractions.Models.ViewStateDto(centerX, centerY, width, height);
    }
}

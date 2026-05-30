using System.Globalization;
using System.Text;
using CoalClaw.Cad.Abstractions.Models;

namespace CoalClaw.Cad.Core.Json;

/// <summary>
/// 手工 JSON（不依赖 System.Text.Json），兼容 ZWCAD 受限 AssemblyLoadContext。
/// </summary>
internal static class SimpleJson
{
    public static string Esc(string? value)
    {
        if (value == null) return "";
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.AppendFormat("\\u{0:X4}", (int)c);
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public static string Str(string? value) => $"\"{Esc(value)}\"";
    public static string Bool(bool value) => value ? "true" : "false";
    public static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Err(string message) => $"{{\"error\":{Str(message)}}}";

    public static string ErrObj(string message) => $"{{\"ok\":false,\"error\":{Str(message)}}}";

    public static string? ExtractString(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var pattern = $"\"{key}\"";
        var idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx = json.IndexOf(':', idx + pattern.Length);
        if (idx < 0) return null;
        idx++;
        while (idx < json.Length && char.IsWhiteSpace(json[idx])) idx++;
        if (idx >= json.Length || json[idx] != '"') return null;
        idx++;
        var sb = new StringBuilder();
        while (idx < json.Length)
        {
            var c = json[idx++];
            if (c == '"') break;
            if (c == '\\' && idx < json.Length)
            {
                var n = json[idx++];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    default: sb.Append(n); break;
                }
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    public static bool TryExtractDouble(string json, string key, out double value)
    {
        value = 0;
        if (string.IsNullOrEmpty(json)) return false;
        var pattern = $"\"{key}\"";
        var idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        idx = json.IndexOf(':', idx + pattern.Length);
        if (idx < 0) return false;
        idx++;
        while (idx < json.Length && char.IsWhiteSpace(json[idx])) idx++;
        var end = idx;
        while (end < json.Length && "0123456789.-eE+".IndexOf(json[end]) >= 0) end++;
        return end > idx && double.TryParse(json.Substring(idx, end - idx), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    public static string Ping(CadHostMetadata meta) =>
        "{" +
        $"\"ok\":true," +
        $"\"host\":{Str(meta.HostId)}," +
        $"\"platform\":{Str(meta.Platform)}," +
        $"\"apiVersion\":{Str(meta.ApiVersion)}," +
        $"\"hostVersion\":{Str(meta.HostVersion)}," +
        $"\"port\":{meta.Port}," +
        $"\"threading\":{Str(meta.ApiThreadingVersion)}," +
        $"\"autocadVersion\":{Str(meta.HostVersion)}" +
        "}";

    public static string Health(bool ok, bool hasDoc, string? docName, string? error) =>
        "{" +
        $"\"ok\":{Bool(ok)}," +
        $"\"hasActiveDocument\":{Bool(hasDoc)}," +
        $"\"documentName\":{(docName == null ? "null" : Str(docName))}," +
        $"\"error\":{(error == null ? "null" : Str(error))}" +
        "}";

    public static string OpenOk(string docName) =>
        $"{{\"ok\":true,\"documentName\":{Str(docName)}}}";

    public static string Find(IReadOnlyList<FindMatchDto> matches)
    {
        var sb = new StringBuilder();
        sb.Append("{\"matches\":[");
        for (var i = 0; i < matches.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var m = matches[i];
            sb.Append('{');
            sb.Append($"\"handle\":{Str(m.Handle)},");
            sb.Append($"\"entityType\":{Str(m.EntityType)},");
            sb.Append($"\"matchedField\":{Str(m.MatchedField)},");
            sb.Append($"\"matchedValue\":{Str(m.MatchedValue)},");
            sb.Append($"\"layer\":{Str(m.Layer)},");
            sb.Append($"\"position\":{{\"x\":{Num(m.Position.X)},\"y\":{Num(m.Position.Y)},\"z\":{Num(m.Position.Z)}}}");
            sb.Append('}');
        }
        sb.Append("],\"count\":").Append(matches.Count).Append('}');
        return sb.ToString();
    }

    public static string Zoom(ZoomResponse r)
    {
        if (r.View == null)
            return $"{{\"ok\":{Bool(r.Ok)},\"view\":null,\"error\":{(r.Error == null ? "null" : Str(r.Error))}}}";

        var v = r.View;
        return "{" +
               $"\"ok\":{Bool(r.Ok)}," +
               $"\"view\":{{\"centerX\":{Num(v.CenterX)},\"centerY\":{Num(v.CenterY)},\"width\":{Num(v.Width)},\"height\":{Num(v.Height)}}}," +
               $"\"error\":null" +
               "}";
    }
}

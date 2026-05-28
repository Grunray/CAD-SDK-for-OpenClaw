using System;
using System.IO;

namespace CoalClaw.Cad.Core.Runtime;

public static class CadRuntimeWriter
{
    public static void Write(string hostId, string platform, int port, string pluginAssemblyName)
    {
        try
        {
            var wikiDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".openclaw",
                "kb",
                "shared",
                "wiki");
            Directory.CreateDirectory(wikiDir);

            var runtimeFile = Path.Combine(wikiDir, "cad-runtime.md");
            var content =
                $"# CAD Runtime\n\n" +
                $"- baseUrl: http://127.0.0.1:{port}\n" +
                $"- host: {hostId}\n" +
                $"- platform: {platform}\n" +
                $"- plugin: {pluginAssemblyName}\n";

            File.WriteAllText(runtimeFile, content);

            // Backward compatibility for one release cycle
            if (string.Equals(hostId, "autocad", StringComparison.OrdinalIgnoreCase))
            {
                var legacyFile = Path.Combine(wikiDir, "autocad-runtime.md");
                File.WriteAllText(legacyFile,
                    $"# AutoCAD Runtime\n\n- baseUrl: http://127.0.0.1:{port}\n- plugin: {pluginAssemblyName}\n");
            }
        }
        catch
        {
            // Non-fatal if wiki path is unavailable.
        }
    }
}

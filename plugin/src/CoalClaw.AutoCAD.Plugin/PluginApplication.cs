using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using CoalClaw.Cad.Core.Http;
using CoalClaw.Cad.Core.Runtime;
using CoalClaw.AutoCAD.Plugin.Infrastructure;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(CoalClaw.AutoCAD.Plugin.PluginApplication))]

namespace CoalClaw.AutoCAD.Plugin;

public sealed class PluginApplication : IExtensionApplication
{
    private const int DefaultPort = 54321;
    private static readonly AutoCadHostContext HostContext = new();
    private HttpServerHost? _httpHost;

    public void Initialize()
    {
        AcadUiContext.Initialize();
        _httpHost = new HttpServerHost(HostContext, DefaultPort);
        _httpHost.Start();
        CadRuntimeWriter.Write("autocad", "windows", DefaultPort, "CoalClaw.AutoCAD.Plugin.dll");
        Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
            $"\n[CoalClaw] HTTP API started at http://127.0.0.1:{DefaultPort}\n");
    }

    public void Terminate()
    {
        _httpHost?.Dispose();
        _httpHost = null;
    }
}

public static class PluginCommands
{
    [CommandMethod("COALCLAW_PING")]
    public static void PingCommand()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage("\n[CoalClaw] Plugin loaded. HTTP API: http://127.0.0.1:54321/ping\n");
    }

    [CommandMethod("COALCLAW_STATUS")]
    public static void StatusCommand()
    {
        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
        if (ed == null)
            return;

        ed.WriteMessage("\n[CoalClaw] 插件程序集已在当前 AutoCAD 进程中加载。");
        ed.WriteMessage("\n  HTTP: http://127.0.0.1:54321/ping");
        ed.WriteMessage("\n  若需加载新版本 DLL：请先完全退出 AutoCAD，重新 build，再启动一次（不要重复 NETLOAD）。");
        ed.WriteMessage("\n  若出现「Assembly with same name is already loaded」= 重复 NETLOAD，可忽略并直接测试 HTTP。\n");
    }
}

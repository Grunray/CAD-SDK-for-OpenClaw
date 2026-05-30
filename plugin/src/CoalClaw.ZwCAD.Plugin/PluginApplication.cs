using CoalClaw.Cad.Core.Http;
using CoalClaw.Cad.Core.Runtime;
using CoalClaw.ZwCAD.Plugin.Infrastructure;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Runtime;
using Application = ZwSoft.ZwCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(CoalClaw.ZwCAD.Plugin.PluginApplication))]

namespace CoalClaw.ZwCAD.Plugin;

public sealed class PluginApplication : IExtensionApplication
{
    private const int DefaultPort = 54321;
    private static readonly ZwCadHostContext HostContext = new();
    private HttpServerHost? _httpHost;

    public void Initialize()
    {
        ZwCadUiContext.Initialize();
        _httpHost = new HttpServerHost(HostContext, DefaultPort);
        _httpHost.Start();
        CadRuntimeWriter.Write("zwcad", "linux", DefaultPort, $"{typeof(PluginApplication).Assembly.GetName().Name}.dll");
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

        ed.WriteMessage("\n[CoalClaw] 插件已在当前中望 CAD 进程中加载。");
        ed.WriteMessage("\n  HTTP: http://127.0.0.1:54321/ping");
        ed.WriteMessage("\n  更新 DLL 后请重启中望 CAD，不要重复 NETLOAD。\n");
    }
}

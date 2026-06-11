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
    internal static int Port { get; private set; } = DefaultPort;
    private HttpServerHost? _httpHost;

    public void Initialize()
    {
        ZwCadUiContext.Initialize();
        Port = ResolvePort();
        _httpHost = new HttpServerHost(new ZwCadHostContext(Port), Port);
        _httpHost.Start();
        CadRuntimeWriter.Write("zwcad", "linux", Port, $"{typeof(PluginApplication).Assembly.GetName().Name}.dll");
        Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
            $"\n[CoalClaw] HTTP API started at http://127.0.0.1:{Port}\n");
    }

    // CAD 进程继承启动 shell 的环境，skill 的 config.sh 已 export COALCLAW_HTTP_PORT
    private static int ResolvePort()
    {
        var raw = Environment.GetEnvironmentVariable("COALCLAW_HTTP_PORT");
        if (int.TryParse(raw, out var port) && port is > 0 and < 65536)
            return port;
        return DefaultPort;
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
        doc?.Editor.WriteMessage($"\n[CoalClaw] Plugin loaded. HTTP API: http://127.0.0.1:{PluginApplication.Port}/ping\n");
    }

    [CommandMethod("COALCLAW_STATUS")]
    public static void StatusCommand()
    {
        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
        if (ed == null)
            return;

        ed.WriteMessage("\n[CoalClaw] 插件已在当前中望 CAD 进程中加载。");
        ed.WriteMessage($"\n  HTTP: http://127.0.0.1:{PluginApplication.Port}/ping");
        ed.WriteMessage("\n  更新 DLL 后请重启中望 CAD，不要重复 NETLOAD。\n");
    }
}

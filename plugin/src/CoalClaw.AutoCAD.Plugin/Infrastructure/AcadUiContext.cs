using Autodesk.AutoCAD.ApplicationServices;

namespace CoalClaw.AutoCAD.Plugin.Infrastructure;

/// <summary>
/// 将工作投递到 AutoCAD UI 线程。插件加载时捕获 SynchronizationContext，HTTP 线程用 Post 等待，不访问 MainWindow。
/// </summary>
internal static class AcadUiContext
{
    private static SynchronizationContext? _uiContext;

    internal static void Initialize()
    {
        _uiContext = SynchronizationContext.Current;
    }

    internal static T Run<T>(Func<T> action, TimeSpan timeout)
    {
        var ctx = _uiContext;
        if (ctx != null)
            return RunViaSynchronizationContext(ctx, action, timeout);

        return RunViaApplicationContext(action, timeout);
    }

    private static T RunViaSynchronizationContext<T>(
        SynchronizationContext ctx,
        Func<T> action,
        TimeSpan timeout)
    {
        T? result = default;
        Exception? error = null;
        using var done = new ManualResetEventSlim(false);

        ctx.Post(_ =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        }, null);

        if (!done.Wait(timeout))
            throw new TimeoutException($"AutoCAD UI context timed out after {timeout.TotalSeconds:F0}s.");

        if (error != null)
            throw error;

        return result!;
    }

    /// <summary>
    /// 旧版 AutoCAD 无 SynchronizationContext 时的回退；仅轮询，不 PostMessage / MainWindow。
    /// </summary>
    private static T RunViaApplicationContext<T>(Func<T> action, TimeSpan timeout)
    {
        T? result = default;
        Exception? error = null;
        var completed = 0; // 0=pending, 1=ok, 2=error

        Application.DocumentManager.ExecuteInApplicationContext(_ =>
        {
            try
            {
                result = action();
                Interlocked.Exchange(ref completed, 1);
            }
            catch (Exception ex)
            {
                error = ex;
                Interlocked.Exchange(ref completed, 2);
            }
        }, null);

        var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            var state = Volatile.Read(ref completed);
            if (state == 1)
                return result!;

            if (state == 2)
                throw error ?? new InvalidOperationException("AutoCAD application context failed.");

            Thread.Sleep(20);
        }

        throw new TimeoutException($"AutoCAD application context timed out after {timeout.TotalSeconds:F0}s.");
    }
}

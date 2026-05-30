using System.Text;
using CoalClaw.Cad.Abstractions;
using CoalClaw.Cad.Core.Api;

namespace CoalClaw.Cad.Core.Http;

public sealed class HttpServerHost : IDisposable
{
    private readonly ICadHostContext _host;
    private readonly ApiRouter _router;
    private readonly int _port;
    private int _listenFd = -1;
    private Thread? _serverThread;
    private volatile bool _stopRequested;

    public HttpServerHost(ICadHostContext host, int port)
    {
        _host = host;
        _port = port;
        _router = new ApiRouter(host);
    }

    public int Port => _port;

    public void Start()
    {
        if (_serverThread != null)
            return;

        _stopRequested = false;
        _serverThread = new Thread(ServerLoop)
        {
            IsBackground = true,
            Name = $"CoalClaw-HTTP-{_port}"
        };
        _serverThread.Start();
    }

    private void ServerLoop()
    {
        try
        {
            _listenFd = NativeSocket.CreateListenSocket(_port);

            while (!_stopRequested)
            {
                var clientFd = NativeSocket.Accept(_listenFd);
                if (clientFd < 0)
                {
                    if (_stopRequested) break;
                    Thread.Sleep(50);
                    continue;
                }

                var fd = clientFd;
                var thread = new Thread(() => HandleClient(fd))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
        catch (Exception ex)
        {
            _host.LogError($"HTTP server error: {ex.Message}");
        }
        finally
        {
            if (_listenFd >= 0)
            {
                NativeSocket.Close(_listenFd);
                _listenFd = -1;
            }
        }
    }

    private void HandleClient(int clientFd)
    {
        try
        {
            var buffer = new byte[8192];
            var totalRead = NativeSocket.ReadAvailable(clientFd, buffer, 0, buffer.Length, 5000);
            if (totalRead == 0) return;

            var requestText = Encoding.ASCII.GetString(buffer, 0, totalRead);
            var request = ParseRequest(requestText);
            if (request == null) return;

            var (status, responseBody) = _router.Route(
                request.Method,
                request.Path,
                request.Query,
                request.Body);

            var statusText = status switch
            {
                200 => "OK",
                400 => "Bad Request",
                404 => "Not Found",
                500 => "Internal Server Error",
                504 => "Gateway Timeout",
                _ => "Unknown"
            };

            var responseStr = $"HTTP/1.1 {status} {statusText}\r\n" +
                              $"Content-Type: application/json; charset=utf-8\r\n" +
                              $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                              $"Connection: close\r\n" +
                              $"\r\n" +
                              responseBody;

            NativeSocket.SendAll(clientFd, Encoding.UTF8.GetBytes(responseStr));
        }
        catch (Exception ex)
        {
            _host.LogError($"HandleClient error: {ex.Message}");
        }
        finally
        {
            NativeSocket.Close(clientFd);
        }
    }

    private sealed class HttpRequestInfo
    {
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public Dictionary<string, string> Query { get; set; } = new();
        public string Body { get; set; } = "";
    }

    private static HttpRequestInfo? ParseRequest(string raw)
    {
        var lines = raw.Split("\r\n");
        if (lines.Length == 0) return null;

        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2) return null;

        var method = requestLine[0];
        var fullPath = requestLine[1];

        var info = new HttpRequestInfo { Method = method };

        var qIdx = fullPath.IndexOf('?');
        if (qIdx >= 0)
        {
            info.Path = fullPath[..qIdx];
            var qs = fullPath[(qIdx + 1)..];
            foreach (var pair in qs.Split('&'))
            {
                var eq = pair.IndexOf('=');
                if (eq >= 0)
                {
                    var key = NativeSocket.PercentDecode(pair[..eq]).ToLowerInvariant();
                    var val = NativeSocket.PercentDecode(pair[(eq + 1)..]);
                    info.Query[key] = val;
                }
            }
        }
        else
        {
            info.Path = fullPath;
        }

        var bodyStart = raw.IndexOf("\r\n\r\n");
        if (bodyStart >= 0 && bodyStart + 4 < raw.Length)
            info.Body = raw[(bodyStart + 4)..];

        return info;
    }

    public void Stop()
    {
        _stopRequested = true;
        if (_listenFd >= 0)
            NativeSocket.Close(_listenFd);
        _listenFd = -1;
        _serverThread = null;
    }

    public void Dispose()
    {
        Stop();
    }
}

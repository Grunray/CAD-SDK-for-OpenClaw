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

    private const int MaxRequestBytes = 1024 * 1024;
    private const int RecvTimeoutMs = 5000;

    private void HandleClient(int clientFd)
    {
        try
        {
            NativeSocket.SetRecvTimeout(clientFd, RecvTimeoutMs);
            var requestBytes = ReadRequest(clientFd);
            if (requestBytes.Length == 0) return;

            // UTF-8 而非 ASCII：POST body 可能含非 ASCII 路径（如中文）；UTF-8 兼容 ASCII 请求行与 header
            var requestText = Encoding.UTF8.GetString(requestBytes);
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

    /// <summary>
    /// 协议驱动读取：增量 recv，定位 header 结束符后按 Content-Length 读满即停。
    /// 旧实现按「填满缓冲区」循环——完整请求到达后仍会再次阻塞在 recv 上，且 >8KB 请求被截断。
    /// </summary>
    private static byte[] ReadRequest(int clientFd)
    {
        var chunk = new byte[8192];
        using var data = new MemoryStream();
        var headerEnd = -1;
        var contentLength = 0;

        while (data.Length < MaxRequestBytes)
        {
            var n = NativeSocket.Recv(clientFd, chunk, chunk.Length);
            if (n <= 0) break; // 对端关闭或 recv 超时；已读部分尽力解析

            data.Write(chunk, 0, n);

            if (headerEnd < 0)
            {
                headerEnd = FindHeaderEnd(data);
                if (headerEnd >= 0)
                    contentLength = ParseContentLength(data, headerEnd);
            }

            if (headerEnd >= 0 && data.Length >= headerEnd + 4 + (long)contentLength)
                break;
        }

        return data.ToArray();
    }

    private static int FindHeaderEnd(MemoryStream data)
    {
        var buf = data.GetBuffer();
        var len = (int)data.Length;
        for (var i = 3; i < len; i++)
        {
            if (buf[i - 3] == (byte)'\r' && buf[i - 2] == (byte)'\n' &&
                buf[i - 1] == (byte)'\r' && buf[i] == (byte)'\n')
                return i - 3;
        }
        return -1;
    }

    private static int ParseContentLength(MemoryStream data, int headerEnd)
    {
        var headerText = Encoding.ASCII.GetString(data.GetBuffer(), 0, headerEnd);
        foreach (var line in headerText.Split("\r\n"))
        {
            var sep = line.IndexOf(':');
            if (sep <= 0) continue;
            if (!line[..sep].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(line[(sep + 1)..].Trim(), out var value) && value >= 0)
                return Math.Min(value, MaxRequestBytes);
        }
        return 0;
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

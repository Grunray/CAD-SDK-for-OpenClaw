using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using CoalClaw.Cad.Abstractions;
using CoalClaw.Cad.Core.Api;

namespace CoalClaw.Cad.Core.Http;

public sealed class HttpServerHost : IDisposable
{
    private readonly ICadHostContext _host;
    private readonly ApiRouter _router;
    private readonly int _port;
    private TcpListener? _listener;
    private Thread? _serverThread;
    private CancellationTokenSource? _cts;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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

        _cts = new CancellationTokenSource();
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
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            while (!_cts!.Token.IsCancellationRequested)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (ObjectDisposedException) { break; }
            }
        }
        catch (Exception ex)
        {
            _host.LogError($"HTTP server error: {ex.Message}");
        }
    }

    private void HandleClient(object? state)
    {
        var client = (TcpClient)state!;
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var totalRead = 0;
                var timeoutAt = Environment.TickCount + 5000;

                while (totalRead < buffer.Length)
                {
                    var available = client.Available;
                    if (available > 0)
                    {
                        var chunkSize = Math.Min(available, buffer.Length - totalRead);
                        var read = stream.Read(buffer, totalRead, chunkSize);
                        if (read == 0) break;
                        totalRead += read;

                        var text = Encoding.ASCII.GetString(buffer, 0, totalRead);
                        if (text.Contains("\r\n\r\n")) break;
                    }
                    else
                    {
                        if (Environment.TickCount > timeoutAt) break;
                        Thread.Sleep(10);
                    }
                }

                if (totalRead == 0) return;

                var requestText = Encoding.ASCII.GetString(buffer, 0, totalRead);
                var request = ParseRequest(requestText);
                if (request == null) return;

                var (status, body) = _router.Route(
                    request.Method,
                    request.Path,
                    request.Query,
                    request.Body);

                var responseBody = body != null
                    ? JsonSerializer.Serialize(body, JsonOpts)
                    : "";
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

                var responseBytes = Encoding.UTF8.GetBytes(responseStr);
                stream.Write(responseBytes, 0, responseBytes.Length);
                stream.Flush();
            }
        }
        catch (Exception ex)
        {
            _host.LogError($"HandleClient error: {ex.Message}");
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
                    var key = Uri.UnescapeDataString(pair[..eq]).ToLowerInvariant();
                    var val = Uri.UnescapeDataString(pair[(eq + 1)..]);
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
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        _serverThread = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        Stop();
    }
}

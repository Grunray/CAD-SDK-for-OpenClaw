using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace CoalClaw.Cad.Core.Http;

/// <summary>
/// 跨平台 TCP（P/Invoke），避免 System.Net.Sockets 在 ZWCAD NETLOAD 上下文中不可用。
/// </summary>
internal static class NativeSocket
{
    private const int AfInet = 2;
    private const int SockStream = 1;
    private const int SolSocket = 1;
    private const int SoReuseAddr = 2;
    private const int ListenBacklog = 8;
    private const int InvalidSocket = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct SockAddrIn
    {
        public ushort sin_family;
        public ushort sin_port;
        public uint sin_addr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] sin_zero;
    }

    public static int CreateListenSocket(int port)
    {
        Platform.EnsureWsa();
        var fd = Platform.socket(AfInet, SockStream, 0);
        if (fd == InvalidSocket) throw new InvalidOperationException("socket() failed.");

        var reuse = 1;
        Platform.setsockopt(fd, SolSocket, SoReuseAddr, ref reuse, Marshal.SizeOf<int>());

        var addr = new SockAddrIn
        {
            sin_family = AfInet,
            sin_port = htons((ushort)port),
            sin_addr = htonl(0x7f000001u),
            sin_zero = new byte[8]
        };

        if (Platform.bind(fd, ref addr, Marshal.SizeOf<SockAddrIn>()) != 0)
        {
            Platform.close(fd);
            throw new InvalidOperationException("bind() failed.");
        }

        if (Platform.listen(fd, ListenBacklog) != 0)
        {
            Platform.close(fd);
            throw new InvalidOperationException("listen() failed.");
        }

        return fd;
    }

    public static int Accept(int listenFd) => Platform.accept(listenFd, IntPtr.Zero, IntPtr.Zero);

    public static void Close(int fd)
    {
        if (fd != InvalidSocket)
            Platform.close(fd);
    }

    public static int ReadAvailable(int fd, byte[] buffer, int offset, int count, int timeoutMs)
    {
        var deadline = Environment.TickCount + timeoutMs;
        var total = 0;
        while (total < count && Environment.TickCount < deadline)
        {
            var n = Platform.recv(fd, buffer, offset + total, count - total, 0);
            if (n > 0)
            {
                total += n;
                continue;
            }
            if (n == 0) break;
            Thread.Sleep(10);
        }
        return total;
    }

    public static void SendAll(int fd, byte[] data)
    {
        var sent = 0;
        while (sent < data.Length)
        {
            var n = Platform.send(fd, data, sent, data.Length - sent, 0);
            if (n <= 0) throw new InvalidOperationException("send() failed.");
            sent += n;
        }
    }

    public static string PercentDecode(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var bytes = new List<byte>();
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '%' && i + 2 < value.Length)
            {
                var hex = value.Substring(i + 1, 2);
                if (byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                {
                    bytes.Add(b);
                    i += 2;
                    continue;
                }
            }
            if (value[i] == '+') bytes.Add((byte)' ');
            else bytes.Add((byte)value[i]);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static ushort htons(ushort port) =>
        (ushort)(((port & 0xFF) << 8) | ((port >> 8) & 0xFF));

    private static uint htonl(uint addr) =>
        ((addr & 0xFF) << 24) | ((addr & 0xFF00) << 8) | ((addr & 0xFF0000) >> 8) | ((addr >> 24) & 0xFF);

    private static class Platform
    {
        public static void EnsureWsa()
        {
            if (OperatingSystem.IsWindows())
                Win.EnsureWsa();
        }

        public static int socket(int af, int type, int protocol) =>
            OperatingSystem.IsWindows() ? Win.socket(af, type, protocol) : Lin.socket(af, type, protocol);

        public static int bind(int s, ref SockAddrIn addr, int addrlen) =>
            OperatingSystem.IsWindows() ? Win.bind(s, ref addr, addrlen) : Lin.bind(s, ref addr, addrlen);

        public static int listen(int s, int backlog) =>
            OperatingSystem.IsWindows() ? Win.listen(s, backlog) : Lin.listen(s, backlog);

        public static int accept(int s, IntPtr addr, IntPtr addrlen) =>
            OperatingSystem.IsWindows() ? Win.accept(s, addr, addrlen) : Lin.accept(s, addr, addrlen);

        public static int setsockopt(int s, int level, int optname, ref int optval, int optlen) =>
            OperatingSystem.IsWindows() ? Win.setsockopt(s, level, optname, ref optval, optlen) : Lin.setsockopt(s, level, optname, ref optval, optlen);

        public static int recv(int s, byte[] buf, int offset, int len, int flags) =>
            OperatingSystem.IsWindows() ? Win.recv(s, buf, offset, len, flags) : Lin.recv(s, buf, offset, len, flags);

        public static int send(int s, byte[] buf, int offset, int len, int flags) =>
            OperatingSystem.IsWindows() ? Win.send(s, buf, offset, len, flags) : Lin.send(s, buf, offset, len, flags);

        public static int close(int s) =>
            OperatingSystem.IsWindows() ? Win.close(s) : Lin.close(s);
    }

    private static class Win
    {
        private static bool _started;

        public static void EnsureWsa()
        {
            if (_started) return;
            var data = new byte[408];
            if (WSAStartup(0x0202, data) != 0)
                throw new InvalidOperationException("WSAStartup failed.");
            _started = true;
        }

        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int WSAStartup(ushort v, byte[] d);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int socket(int af, int type, int protocol);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int bind(int s, ref SockAddrIn addr, int addrlen);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int listen(int s, int backlog);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int accept(int s, IntPtr addr, IntPtr addrlen);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int setsockopt(int s, int level, int optname, ref int optval, int optlen);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int recv(int s, byte[] buf, int offset, int len, int flags);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int send(int s, byte[] buf, int offset, int len, int flags);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int close(int s);
    }

    private static class Lin
    {
        private const string Libc = "libc";

        [DllImport(Libc, SetLastError = true)] public static extern int socket(int af, int type, int protocol);
        [DllImport(Libc, SetLastError = true)] public static extern int bind(int s, ref SockAddrIn addr, int addrlen);
        [DllImport(Libc, SetLastError = true)] public static extern int listen(int s, int backlog);
        [DllImport(Libc, SetLastError = true)] public static extern int accept(int s, IntPtr addr, IntPtr addrlen);
        [DllImport(Libc, SetLastError = true)] public static extern int setsockopt(int s, int level, int optname, ref int optval, int optlen);
        [DllImport(Libc, SetLastError = true)] public static extern int recv(int s, byte[] buf, int offset, int len, int flags);
        [DllImport(Libc, SetLastError = true)] public static extern int send(int s, byte[] buf, int offset, int len, int flags);
        [DllImport(Libc, SetLastError = true)] public static extern int close(int s);
    }
}

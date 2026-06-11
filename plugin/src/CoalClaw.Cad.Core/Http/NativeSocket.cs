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
    private const int SoRcvTimeoLinux = 20;
    private const int SolSocketWin = 0xFFFF;
    private const int SoRcvTimeoWin = 0x1006;
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

    /// <summary>单次 recv；返回 0=对端关闭，&lt;0=错误或超时（需先 SetRecvTimeout）。</summary>
    public static int Recv(int fd, byte[] buffer, int count) => Platform.Recv(fd, buffer, 0, count, 0);

    /// <summary>SO_RCVTIMEO：让阻塞 recv 在 timeoutMs 后返回 -1，避免慢速/异常客户端永久占住处理线程。</summary>
    public static void SetRecvTimeout(int fd, int timeoutMs)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows 取 DWORD 毫秒；SOL_SOCKET=0xFFFF 与 Linux 取值不同
            var ms = timeoutMs;
            Win.setsockopt(fd, SolSocketWin, SoRcvTimeoWin, ref ms, Marshal.SizeOf<int>());
        }
        else
        {
            // Linux 取 struct timeval { long tv_sec; long tv_usec; }（x86_64 下 16 字节）
            var tv = new byte[16];
            BitConverter.GetBytes((long)(timeoutMs / 1000)).CopyTo(tv, 0);
            BitConverter.GetBytes((long)(timeoutMs % 1000 * 1000)).CopyTo(tv, 8);
            Lin.setsockopt_timeval(fd, SolSocket, SoRcvTimeoLinux, tv, tv.Length);
        }
    }

    public static void SendAll(int fd, byte[] data)
    {
        var sent = 0;
        while (sent < data.Length)
        {
            var chunkLen = data.Length - sent;
            var n = Platform.Send(fd, data, sent, chunkLen, 0);
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

        public static int Recv(int s, byte[] buf, int offset, int len, int flags)
        {
            if (len <= 0) return 0;
            if (offset == 0)
                return OperatingSystem.IsWindows() ? Win.recv(s, buf, len, flags) : Lin.recv(s, buf, len, flags);

            var temp = new byte[len];
            var n = OperatingSystem.IsWindows() ? Win.recv(s, temp, len, flags) : Lin.recv(s, temp, len, flags);
            if (n > 0)
                Buffer.BlockCopy(temp, 0, buf, offset, n);
            return n;
        }

        public static int Send(int s, byte[] buf, int offset, int len, int flags)
        {
            if (len <= 0) return 0;
            if (offset == 0)
                return OperatingSystem.IsWindows() ? Win.send(s, buf, len, flags) : Lin.send(s, buf, len, flags);

            var slice = new byte[len];
            Buffer.BlockCopy(buf, offset, slice, 0, len);
            return OperatingSystem.IsWindows() ? Win.send(s, slice, len, flags) : Lin.send(s, slice, len, flags);
        }

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
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int recv(int s, byte[] buf, int len, int flags);
        [DllImport("ws2_32.dll", SetLastError = true)] public static extern int send(int s, byte[] buf, int len, int flags);
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
        [DllImport(Libc, EntryPoint = "setsockopt", SetLastError = true)] public static extern int setsockopt_timeval(int s, int level, int optname, byte[] optval, int optlen);
        [DllImport(Libc, SetLastError = true)] public static extern int recv(int s, byte[] buf, int len, int flags);
        [DllImport(Libc, SetLastError = true)] public static extern int send(int s, byte[] buf, int len, int flags);
        [DllImport(Libc, SetLastError = true)] public static extern int close(int s);
    }
}

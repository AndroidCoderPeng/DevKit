using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace DevKit.Transports
{
    public class UdpClientTransport : IClientTransport
    {
        private readonly object _lock = new object();
        private UdpSession _session;
        private IPEndPoint _remoteEndPoint;
        private AddressFamily _boundAddressFamily;
        private bool _disposed;

        public event Action<byte[]> DataReceived;

        public UdpClientTransport(string remoteAddress, int remotePort)
        {
            SetRemoteEndPoint(remoteAddress, remotePort);
        }

        public void SetRemoteEndPoint(string address, int port, string path = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(@"对端地址不能为空", nameof(address));
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            var endPoint = ResolveEndPoint(address.Trim(), port);

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(UdpClientTransport));
                }

                // 运行中允许随时更换对端；IPv4/IPv6 之间切换需重建底层 socket
                var session = _session;
                if (session != null && _boundAddressFamily != endPoint.AddressFamily)
                {
                    throw new InvalidOperationException("对端地址族（IPv4/IPv6）发生变化，请先停止收发再重新启动");
                }

                _remoteEndPoint = endPoint;
            }
        }

        /// <summary>
        /// 启动收发：绑定本地随机端口并开始接收循环
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            UdpSession session;
            IPAddress anyAddress;

            lock (_lock)
            {
                if (_session != null)
                {
                    throw new InvalidOperationException("UdpClientTransport 已启动");
                }

                var endPoint = _remoteEndPoint;
                anyAddress = endPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;

                session = new UdpSession();
                _session = session;
                _boundAddressFamily = endPoint.AddressFamily;
            }

            try
            {
                // 客户端必须绑定本地地址（0 端口由系统分配），否则收不到数据
                var host = new IPHost(anyAddress, 0);
                await session.SetupAsync(new TouchSocketConfig().SetBindIPHost(host)).ConfigureAwait(false);
                session.Received = (c, e) =>
                {
                    IPEndPoint current;
                    lock (_lock)
                    {
                        current = _remoteEndPoint;
                    }

                    // 仅处理来自当前对端的数据
                    if (Equals(e.EndPoint, current))
                    {
                        DataReceived?.Invoke(e.Memory.ToArray());
                    }

                    return EasyTask.CompletedTask;
                };

                await session.StartAsync(token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_session, session))
                    {
                        _session = null;
                    }
                }

                session.Dispose();
                throw;
            }
        }

        public async Task SendAsync(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            UdpSession session;
            IPEndPoint endPoint;

            lock (_lock)
            {
                ThrowIfDisposed();
                session = _session;
                endPoint = _remoteEndPoint;
            }

            if (session == null)
            {
                throw new InvalidOperationException("UdpClientTransport 尚未启动，请先调用 StartAsync");
            }

            await session.SendAsync(endPoint, payload, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            UdpSession session;

            lock (_lock)
            {
                session = _session;
                if (session == null)
                {
                    return;
                }

                _session = null;
            }

            try
            {
                await session.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"UDP 停止失败：{0}", e);
            }
            finally
            {
                session.Dispose();
            }
        }

        public void Dispose()
        {
            UdpSession session;

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                session = _session;
                _session = null;
            }

            session?.Dispose();
        }

        // 输入限定为 IP 字面量：SetRemoteEndPoint 是同步接口，域名解析会阻塞 UI 线程
        private static IPEndPoint ResolveEndPoint(string address, int port)
        {
            if (!IPAddress.TryParse(address, out var ip))
            {
                throw new ArgumentException($@"仅支持 IP 地址，不支持域名：{address}", nameof(address));
            }

            return new IPEndPoint(ip, port);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UdpClientTransport));
            }
        }
    }
}
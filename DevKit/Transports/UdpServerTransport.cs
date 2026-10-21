using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Models;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace DevKit.Transports
{
    /// <summary>
    /// 基于 TouchSocket UdpSession 的 UDP 服务端。
    /// UDP 无连接，这里把「来源端点 IP:Port」等价视为一个会话。
    /// </summary>
    public class UdpServerTransport : IDisposable
    {
        private UdpSession _session;
        private volatile bool _running;
        private bool _disposed;

        public event Action<ServerClient, byte[]> DataReceived;

        public bool IsRunning => _running;

        public async Task StartAsync(string address, int port, CancellationToken token)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(@"监听地址不能为空", nameof(address));
            }

            if (port is < 1 or > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (_session != null)
            {
                throw new InvalidOperationException("UDP 服务已经启动");
            }

            var session = new UdpSession();
            _session = session;

            try
            {
                var host = new IPHost($"{address.Trim()}:{port}");
                // 复用地址：允许同机多开监听同一端口，与 TCP 服务端保持一致的默认行为
                var config = new TouchSocketConfig().SetBindIPHost(host).SetReuseAddress(true);

                session.Received = (_, e) =>
                {
                    if (e.EndPoint is IPEndPoint remote)
                    {
                        var client = new ServerClient(remote.ToString(), remote.Address.ToString(), remote.Port);
                        DataReceived?.Invoke(client, e.Memory.ToArray());
                    }

                    return EasyTask.CompletedTask;
                };

                await session.SetupAsync(config).ConfigureAwait(false);
                await session.StartAsync(token).ConfigureAwait(false);
                _running = true;
            }
            catch
            {
                if (ReferenceEquals(_session, session))
                {
                    _session = null;
                }

                _running = false;
                session.Dispose();
                throw;
            }
        }

        public async Task SendAsync(IPEndPoint remote, byte[] payload)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(remote);

            ArgumentNullException.ThrowIfNull(payload);

            var session = _session;
            if (session == null || !_running)
            {
                throw new InvalidOperationException("UDP 服务尚未启动");
            }

            await session.SendAsync(remote, payload, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            var session = _session;
            if (session == null)
            {
                return;
            }

            _session = null;
            _running = false;

            try
            {
                await session.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"UDP 服务停止失败：{0}", e);
            }
            finally
            {
                session.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var session = _session;
            _session = null;
            _running = false;
            session?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(UdpServerTransport));
        }
    }
}
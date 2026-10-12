using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DevKit.Transports
{
    public class UdpClientTransport : ITransport
    {
        private readonly object _lock = new object();
        private UdpClient _client;
        private IPEndPoint _remoteEndPoint;
        private CancellationTokenSource _receiveCts;
        private Task _receiveTask = Task.CompletedTask;
        private bool _disposed;

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _receiveCts?.Cancel();
                _client?.Close();
            }
        }

        public event Action<byte[]> DataReceived;

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

                // 运行中允许随时更换对端；但 IPv4/IPv6 之间切换需要重建底层 socket，这里要求调用方先 StopAsync 再 Start，属资源约束而非连接状态
                var client = _client;
                if (client != null && client.Client.AddressFamily != endPoint.AddressFamily)
                {
                    throw new InvalidOperationException("对端地址族（IPv4/IPv6）发生变化，请先停止收发再重新启动");
                }

                _remoteEndPoint = endPoint;
            }
        }

        public UdpClientTransport(string remoteAddress, int remotePort)
        {
            SetRemoteEndPoint(remoteAddress, remotePort);
        }

        /// <summary>
        /// 启动收发：绑定本地随机端口并开始接收循环
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start(CancellationToken token)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                token.ThrowIfCancellationRequested();

                if (_client != null)
                {
                    throw new InvalidOperationException("UdpClientTransport 已启动");
                }

                var endPoint = _remoteEndPoint;
                var loopback = endPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? IPAddress.IPv6Any
                    : IPAddress.Any;

                var client = new UdpClient(new IPEndPoint(loopback, 0));
                _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                _client = client;
                _receiveTask = ReceiveLoopAsync(client, _receiveCts);
            }
        }

        public async Task SendAsync(byte[] payload, CancellationToken token)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            UdpClient client;
            IPEndPoint endPoint;

            lock (_lock)
            {
                ThrowIfDisposed();
                client = _client;
                endPoint = _remoteEndPoint;
            }

            if (client == null)
            {
                throw new InvalidOperationException("UdpClientTransport 尚未启动，请先调用 Start");
            }

            // 发送前检查取消。UDP 单个数据报发送是原子动作且不可撤回，
            // 发送成功后再抛取消只会让调用方误判发送失败，故不做发送后检查
            token.ThrowIfCancellationRequested();

            // .NET Framework 4.8 的 UdpClient.SendAsync 不支持 CancellationToken；
            // 若与 StopAsync 并发，此处会抛 ObjectDisposedException，属正常的停止竞态
            var count = await client.SendAsync(payload, payload.Length, endPoint).ConfigureAwait(false);
            if (count != payload.Length)
            {
                throw new InvalidOperationException($"UDP 数据报发送不完整：预期 {payload.Length} 字节，实际 {count} 字节");
            }
        }

        public async Task StopAsync()
        {
            Task receiveTask;

            lock (_lock)
            {
                if (_client == null)
                {
                    return;
                }

                /* _client 非空则 _receiveCts 必未被 finally 释放（同锁成对置空），Cancel 安全 */
                receiveTask = _receiveTask;
                _receiveCts.Cancel();
                _client.Close();
            }

            // 锁外等待清理：finally 完成后 _client 已置空，StopAsync 返回即可立即 Start
            await receiveTask.ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(UdpClient client, CancellationTokenSource receiveCts)
        {
            var token = receiveCts.Token;

            try
            {
                // 注册取消即关闭 socket：打断挂起的 ReceiveAsync，避免 token 取消后循环仍卡在等待
                using (token.Register(client.Close))
                {
                    while (!token.IsCancellationRequested)
                    {
                        var result = await client.ReceiveAsync().ConfigureAwait(false);

                        // 锁内快照当前对端，与 SetRemoteEndPoint 的发布点构成同步关系；
                        // 每个数据报取一次快照，运行中切换对端后新数据报立即按新端点过滤
                        IPEndPoint endPoint;
                        lock (_lock)
                        {
                            endPoint = _remoteEndPoint;
                        }

                        if (!Equals(result.RemoteEndPoint, endPoint))
                        {
                            continue;
                        }

                        DataReceived?.Invoke(result.Buffer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 主动停止收发
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
                // 主动停止时底层 Socket 已关闭
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
                // 主动停止时 ReceiveAsync 被中断
            }
            catch (Exception e)
            {
                Console.WriteLine(@"UDP 接收失败：{0}", e);
            }
            finally
            {
                lock (_lock)
                {
                    client.Close();
                    receiveCts.Dispose();

                    if (ReferenceEquals(_client, client))
                    {
                        _client = null;
                        _receiveCts = null;
                    }
                }
            }
        }

        // 输入限定为 IP 字面量：SetRemoteEndPoint 是同步接口，域名解析会阻塞 UI 线程。
        // 若日后要支持域名，在 VM 层异步解析成 IP 后再传入
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
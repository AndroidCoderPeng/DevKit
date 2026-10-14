using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Models;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace DevKit.Transports
{
    public class TcpServerTransport : IServerTransport
    {
        private readonly ConcurrentDictionary<string, TcpSessionClient> _clients =
            new ConcurrentDictionary<string, TcpSessionClient>();

        private TcpService _service;
        private bool _disposed;

        public event Action<ServerClient> ClientConnected;
        public event Action<ServerClient> ClientClosed;
        public event Action<ServerClient, byte[]> DataReceived;

        public bool IsRunning => _service != null && _service.ServerState == ServerState.Running;

        public async Task StartAsync(string address, int port, bool reuseAddress, CancellationToken token)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(@"监听地址不能为空", nameof(address));
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (_service != null)
            {
                throw new InvalidOperationException("TCP 服务已经启动");
            }

            var service = new TcpService();
            BindEvents(service);
            _service = service;

            try
            {
                // 监听只支持 IP + 端口，不支持域名
                var host = new IPHost($"{address.Trim()}:{port}");
                var config = new TouchSocketConfig().SetListenIPHosts(host).SetReuseAddress(reuseAddress);
                await service.SetupAsync(config).ConfigureAwait(false);
                await service.StartAsync(token).ConfigureAwait(false);
            }
            catch
            {
                if (ReferenceEquals(_service, service))
                {
                    _service = null;
                }

                _clients.Clear();
                service.Dispose();
                throw;
            }
        }

        public Task StartAsync(string address, int port, string path, CancellationToken token)
        {
            // TcpServerTransport 不支持路径参数，直接忽略
            return EasyTask.CompletedTask;
        }

        private void BindEvents(TcpService service)
        {
            service.Connected = (client, e) =>
            {
                _clients[client.Id] = client;
                ClientConnected?.Invoke(new ServerClient(client.Id, client.IP, client.Port));
                return EasyTask.CompletedTask;
            };

            service.Closed = (client, e) =>
            {
                _clients.TryRemove(client.Id, out _);
                ClientClosed?.Invoke(new ServerClient(client.Id, client.IP, client.Port));
                return EasyTask.CompletedTask;
            };

            service.Received = (client, e) =>
            {
                // ReceivedDataEventArgs 的数据在 e.Memory，必须 ToArray() 拷贝
                DataReceived?.Invoke(new ServerClient(client.Id, client.IP, client.Port), e.Memory.ToArray());
                return EasyTask.CompletedTask;
            };
        }

        public async Task StopAsync()
        {
            var service = _service;
            if (service == null)
            {
                return;
            }

            _service = null;
            _clients.Clear();

            try
            {
                await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"TCP 服务停止失败：{0}", e);
            }
            finally
            {
                service.Dispose();
            }
        }

        public async Task SendAsync(string clientId, byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (!_clients.TryGetValue(clientId, out var client))
            {
                throw new InvalidOperationException($"客户端 {clientId} 不存在或已断开");
            }

            await client.SendAsync(payload, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task DisconnectAll()
        {
            foreach (var client in _clients.Values)
            {
                await client.CloseAsync("服务端主动断开", CancellationToken.None).ConfigureAwait(false);
            }
        }

        public async Task DisconnectClient(string clientId)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                await client.CloseAsync("服务端主动断开", CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var service = _service;
            _service = null;
            _clients.Clear();

            service?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpServerTransport));
            }
        }
    }
}
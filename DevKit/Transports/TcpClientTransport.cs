using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;
using TouchSocket.Core;
using TouchSocket.Sockets;
using TouchTcpClient = TouchSocket.Sockets.TcpClient;

namespace DevKit.Transports
{
    public class TcpClientTransport : ITransport
    {
        private const int ConnectTimeoutMs = 10000;

        private string _remoteAddress;
        private int _remotePort;
        private TouchTcpClient _client;
        private TaskCompletionSource<bool> _closedTcs;
        private bool _activeClose;
        private bool _disposed;

        public event Action<byte[]> DataReceived;
        public event Action<TransportState> StateChanged;

        public TcpClientTransport(string remoteAddress, int remotePort)
        {
            SetRemoteEndPoint(remoteAddress, remotePort);
        }

        public void SetRemoteEndPoint(string address, int port, string path = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(@"服务端地址不能为空", nameof(address));
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpClientTransport));
            }

            if (_client != null)
            {
                throw new InvalidOperationException("请先断开连接，再修改远端地址");
            }

            _remoteAddress = address.Trim();
            _remotePort = port;
        }

        public async Task ConnectAsync()
        {
            ThrowIfDisposed();

            if (_client != null)
            {
                throw new InvalidOperationException("TCP 客户端已经连接");
            }

            StateChanged?.Invoke(TransportState.Connecting);

            var client = new TouchTcpClient();
            var closedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _client = client;
            _closedTcs = closedTcs;

            try
            {
                var host = new IPHost($"{_remoteAddress}:{_remotePort}");
                await client.SetupAsync(new TouchSocketConfig().SetRemoteIPHost(host)).ConfigureAwait(false);

                BindEvents(client, closedTcs);
                _client = client;
                _closedTcs = closedTcs;
                
                // 带超时的连接扩展，避免无 token 时连接挂死
                await client.ConnectAsync(ConnectTimeoutMs).ConfigureAwait(false);
                StateChanged?.Invoke(TransportState.Connected);
            }
            catch
            {
                client.Dispose();
                closedTcs.TrySetResult(true);

                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                    _closedTcs = null;
                }

                StateChanged?.Invoke(TransportState.Error);
                throw;
            }
        }

        public async Task SendAsync(byte[] payload)
        {
            ThrowIfDisposed();

            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var client = _client;
            if (client == null || !client.Online)
            {
                throw new InvalidOperationException("TCP 客户端尚未连接");
            }

            await client.SendAsync(payload, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            var client = _client;
            var closedTcs = _closedTcs;

            if (client == null)
            {
                return;
            }

            try
            {
                await client.CloseAsync("主动断开连接", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"TCP 主动断开异常：{0}", e);
            }

            if (closedTcs != null)
            {
                await closedTcs.Task.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var client = _client;
            _client = null;
            _closedTcs?.TrySetResult(true);

            client?.Dispose();
        }

        private void BindEvents(TouchTcpClient client, TaskCompletionSource<bool> closedTcs)
        {
            client.Received = (c, e) =>
            {
                DataReceived?.Invoke(e.Memory.ToArray());
                return EasyTask.CompletedTask;
            };

            // Closing 仅在主动断开时触发，用它区分主动/被动断开
            client.Closing = (c, e) =>
            {
                _activeClose = true;
                return EasyTask.CompletedTask;
            };

            client.Closed = (c, e) =>
            {
                var isCurrent = ReferenceEquals(_client, c);
                if (isCurrent)
                {
                    _client = null;
                    _closedTcs = null;
                    StateChanged?.Invoke(_activeClose ? TransportState.Disconnected : TransportState.Error);
                }

                closedTcs.TrySetResult(true);
                c.Dispose();
                return EasyTask.CompletedTask;
            };
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpClientTransport));
            }
        }
    }
}
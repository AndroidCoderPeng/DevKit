using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;

namespace DevKit.Transports
{
    public class TcpClientTransport : ITransport
    {
        private readonly string _remoteAddress;
        private readonly int _remotePort;

        private TcpClient _client;
        private CancellationTokenSource _receiveCts;
        private Task _receiveTask = Task.CompletedTask;
        private bool _disposed;

        public TcpClientTransport(string remoteAddress, int remotePort)
        {
            if (string.IsNullOrWhiteSpace(remoteAddress))
            {
                throw new ArgumentException(@"服务端 IP 不能为空", nameof(remoteAddress));
            }

            if (remotePort < 1 || remotePort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(remotePort));
            }

            _remoteAddress = remoteAddress;
            _remotePort = remotePort;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _receiveCts?.Cancel();
            _client?.Close();
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            ThrowIfDisposed();

            if (_client != null)
            {
                throw new InvalidOperationException("TCP 客户端已经连接");
            }

            StateChanged?.Invoke(TransportState.Connecting);

            var client = new TcpClient();

            try
            {
                using (token.Register(client.Close))
                {
                    await client.ConnectAsync(_remoteAddress, _remotePort);
                }

                token.ThrowIfCancellationRequested();

                _client = client;
                _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);

                StateChanged?.Invoke(TransportState.Connected);
                _receiveTask = ReceiveLoopAsync(client, _receiveCts);
            }
            catch
            {
                client.Dispose();
                StateChanged?.Invoke(token.IsCancellationRequested
                    ? TransportState.Disconnected
                    : TransportState.Error);
                throw;
            }
        }

        public async Task SendAsync(byte[] payload, CancellationToken token)
        {
            ThrowIfDisposed();

            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var client = _client;
            if (client == null)
            {
                throw new InvalidOperationException("TCP 客户端尚未连接");
            }

            var stream = client.GetStream();
            await stream.WriteAsync(payload, 0, payload.Length, token);
        }

        public event Action<byte[]> DataReceived;

        public event Action<TransportState> StateChanged;

        public async Task DisconnectAsync()
        {
            var client = _client;
            var receiveCts = _receiveCts;

            if (client == null)
            {
                return;
            }

            receiveCts?.Cancel();
            client.Close();

            await _receiveTask;
        }

        private async Task ReceiveLoopAsync(TcpClient client, CancellationTokenSource receiveCts)
        {
            var failed = false;
            var token = receiveCts.Token;

            try
            {
                var stream = client.GetStream();
                var buffer = new byte[8192];

                while (!token.IsCancellationRequested)
                {
                    var count = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                    if (count == 0)
                    {
                        break;
                    }

                    var bytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, count);
                    DataReceived?.Invoke(bytes);
                }
            }
            catch (OperationCanceledException)
            {
                // 主动断开连接
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
                // 主动断开时底层流已关闭
            }
            catch (Exception e)
            {
                failed = true;
                Console.WriteLine(@"TCP 接收失败：{0}", e);
            }
            finally
            {
                client.Dispose();
                receiveCts.Dispose();

                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                    _receiveCts = null;
                }

                StateChanged?.Invoke(failed ? TransportState.Error : TransportState.Disconnected);
            }
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
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;

namespace DevKit.Transports
{
    public class WsClientTransport : ITransport
    {
        private const int MaxMessageBytes = 4 * 1024 * 1024;
        private Uri _remoteUri;
        private ClientWebSocket _client;
        private bool _disposed;
        private CancellationTokenSource _sessionCts;
        private CancellationTokenSource _receiveCts;
        private Task _receiveTask = Task.CompletedTask;

        public event Action<byte[]> DataReceived;

        public WsClientTransport(string remoteAddress, int remotePort, string remotePath = null)
        {
            SetRemoteEndPoint(remoteAddress, remotePort, remotePath);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CancelSafely(_sessionCts);
            _client?.Abort();
        }

        public void SetRemoteEndPoint(string address, int port, string path = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WsClientTransport));
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(@"服务端地址不能为空", nameof(address));
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (_client != null)
            {
                throw new InvalidOperationException("请先断开连接，再修改远端地址");
            }

            var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
            if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
            {
                normalizedPath = "/" + normalizedPath;
            }

            var builder = new UriBuilder("ws", address.Trim(), port)
            {
                Path = normalizedPath
            };

            _remoteUri = builder.Uri;
        }

        public async Task SendAsync(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WsClientTransport));
            }

            var client = _client;
            if (client == null || client.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket 客户端尚未连接");
            }
        }

        public event Action<TransportState> StateChanged;

        public async Task ConnectAsync(CancellationToken token)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WsClientTransport));
            }

            if (_client != null)
            {
                throw new InvalidOperationException("WebSocket 客户端已经连接或正在连接");
            }

            token.ThrowIfCancellationRequested();

            var remoteUri = _remoteUri;
            if (remoteUri == null)
            {
                throw new InvalidOperationException("尚未设置 WebSocket 远端地址");
            }

            var client = new ClientWebSocket();
            var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var receiveCts = new CancellationTokenSource();
            _client = client;
            _sessionCts = sessionCts;
            _receiveCts = receiveCts;

            try
            {
                StateChanged?.Invoke(TransportState.Connecting);
                await client.ConnectAsync(remoteUri, sessionCts.Token);
                sessionCts.Token.ThrowIfCancellationRequested();

                StateChanged?.Invoke(TransportState.Connected);
            }
            catch
            {
                var wasCanceled = sessionCts.IsCancellationRequested;
                sessionCts.Cancel();
                receiveCts.Cancel();

                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                }

                if (ReferenceEquals(_sessionCts, sessionCts))
                {
                    _sessionCts = null;
                }

                if (ReferenceEquals(_receiveCts, receiveCts))
                {
                    _receiveCts = null;
                }

                client.Dispose();
                receiveCts.Dispose();
                sessionCts.Dispose();
                StateChanged?.Invoke(wasCanceled
                    ? TransportState.Disconnected
                    : TransportState.Error);
                throw;
            }

            _receiveTask = ReceiveLoopAsync(client, sessionCts, receiveCts);
        }

        public async Task DisconnectAsync()
        {
            var client = _client;
            if (client == null)
            {
                return;
            }

            var sessionCts = _sessionCts;
            var receiveTask = _receiveTask;

            CancelSafely(sessionCts);
            client.Abort();

            if (receiveTask != Task.CompletedTask)
            {
                await receiveTask.ConfigureAwait(false);
            }
        }

        private async Task ReceiveLoopAsync(
            ClientWebSocket client,
            CancellationTokenSource sessionCts,
            CancellationTokenSource receiveCts)
        {
            var failed = false;
            var receiveToken = receiveCts.Token;

            try
            {
                using (sessionCts.Token.Register(receiveCts.Cancel))
                {
                    var buffer = new byte[8192];

                    while (!receiveToken.IsCancellationRequested)
                    {
                        using (var message = new MemoryStream())
                        {
                            WebSocketReceiveResult result;

                            do
                            {
                                result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), receiveToken)
                                    .ConfigureAwait(false);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    if (client.State == WebSocketState.CloseReceived)
                                    {
                                        await client.CloseOutputAsync(
                                            result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                            result.CloseStatusDescription ?? string.Empty,
                                            CancellationToken.None).ConfigureAwait(false);
                                    }

                                    return;
                                }

                                if (message.Length + result.Count > MaxMessageBytes)
                                {
                                    throw new InvalidDataException($"WebSocket 消息超过最大长度 {MaxMessageBytes} 字节");
                                }

                                message.Write(buffer, 0, result.Count);
                            } while (!result.EndOfMessage);

                            DataReceived?.Invoke(message.ToArray());
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (
                sessionCts.IsCancellationRequested || receiveCts.IsCancellationRequested)
            {
                // 主动断开连接
            }
            catch (WebSocketException) when (
                sessionCts.IsCancellationRequested || receiveCts.IsCancellationRequested)
            {
                // 主动断开时底层 WebSocket 已关闭
            }
            catch (Exception e)
            {
                failed = true;
                Console.WriteLine(@"WebSocket 接收失败：{0}", e);
            }
            finally
            {
                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                }

                if (ReferenceEquals(_sessionCts, sessionCts))
                {
                    _sessionCts = null;
                }

                if (ReferenceEquals(_receiveCts, receiveCts))
                {
                    _receiveCts = null;
                }

                client.Dispose();
                receiveCts.Dispose();
                sessionCts.Dispose();

                StateChanged?.Invoke(failed ? TransportState.Error : TransportState.Disconnected);
            }
        }

        private static void CancelSafely(CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The receive loop may dispose its sources while disconnect is in progress.
            }
        }
    }
}
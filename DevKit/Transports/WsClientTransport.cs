using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Transports.Enums;
using DevKit.Utils;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;

namespace DevKit.Transports
{
    public class WsClientTransport : IClientTransport
    {
        private const int MaxMessageBytes = 4 * 1024 * 1024;

        private readonly object _receiveLock = new object();
        private readonly List<byte> _pending = new List<byte>();

        private Uri _remoteUri;
        private WebSocketClient _client;
        private TaskCompletionSource<bool> _closedTcs;
        private bool _activeClose;
        private bool _disposed;
        private WsFrameKind _currentKind = WsFrameKind.Binary;

        public event Action<byte[]> DataReceived;
        public event Action<byte[], WsFrameKind> FrameReceived;
        public event Action<TransportState> StateChanged;

        public WsClientTransport(string remoteAddress, int remotePort, string remotePath = null)
        {
            SetRemoteEndPoint(remoteAddress, remotePort, remotePath);
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

        public async Task ConnectAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WsClientTransport));
            }

            if (_client != null)
            {
                throw new InvalidOperationException("WebSocket 客户端已经连接或正在连接");
            }

            var remoteUri = _remoteUri;
            if (remoteUri == null)
            {
                throw new InvalidOperationException("尚未设置 WebSocket 远端地址");
            }

            StateChanged?.Invoke(TransportState.Connecting);

            var client = new WebSocketClient();
            var closedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            BindEvents(client, closedTcs);

            _client = client;
            _closedTcs = closedTcs;
            _activeClose = false;

            try
            {
                var host = new IPHost(remoteUri.ToString());
                await client.SetupAsync(new TouchSocketConfig().SetRemoteIPHost(host)).ConfigureAwait(false);

                await client.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
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

        private void BindEvents(WebSocketClient client, TaskCompletionSource<bool> closedTcs)
        {
            client.Received = (c, e) =>
            {
                AppendFrame(e.DataFrame);
                return EasyTask.CompletedTask;
            };
            
            // Closing 仅主动断开时触发
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
            if (client == null || !client.Online)
            {
                throw new InvalidOperationException("WebSocket 客户端尚未连接");
            }

            // 二进制、单帧完整消息（FIN = true）
            await client.SendAsync(payload, true, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            var client = _client;
            var closedTcs = _closedTcs;

            if (client == null)
            {
                return;
            }

            _activeClose = true;   // 主动断开显式标记，不依赖 Closing 事件
            
            try
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "主动断开连接", CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"WebSocket 主动断开异常：{0}", e);
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
            _activeClose = true;   // 主动释放也按主动断开处理

            var client = _client;
            _client = null;
            _closedTcs?.TrySetResult(true);

            client?.Dispose();
        }
        
        // Received 按帧触发，分片消息（Cont）需累积到 FIN 才拼出完整消息
        private void AppendFrame(WSDataFrame frame)
        {
            if (frame == null)
            {
                return;
            }

            var payload = frame.PayloadData;
            if (payload.Length == 0)
            {
                return;
            }
            
            byte[] complete = null;
            var completeKind = WsFrameKind.Binary;
            lock (_receiveLock)
            {
                var isMessageStart = frame.Opcode == WSDataType.Binary || frame.Opcode == WSDataType.Text;
                if (isMessageStart)
                {
                    _pending.Clear();
                    _currentKind = frame.Opcode == WSDataType.Text ? WsFrameKind.Text : WsFrameKind.Binary;
                }

                if (_pending.Count + payload.Length > MaxMessageBytes)
                {
                    Console.WriteLine(@"WebSocket 消息超过最大长度 {0} 字节，已丢弃", MaxMessageBytes);
                    _pending.Clear();
                    return;
                }

                _pending.AddRange(payload.ToArray());

                if (frame.FIN)
                {
                    complete = _pending.ToArray();
                    completeKind = _currentKind;
                    _pending.Clear();
                }
            }
            
            if (complete != null)
            {
                DataReceived?.Invoke(complete);
                FrameReceived?.Invoke(complete, completeKind);
            }
        }
    }
}
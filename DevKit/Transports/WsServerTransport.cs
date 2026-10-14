using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Models;
using DevKit.Transports.Enums;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;

namespace DevKit.Transports
{
    public class WsServerTransport : IServerTransport
    {
        /// <summary>
        /// 单个会话的中继帧缓存。
        /// </summary>
        private sealed class FrameBuffer
        {
            public readonly object SyncRoot = new object();
            public readonly List<byte> Buffer = new List<byte>();
            public WsFrameKind Kind = WsFrameKind.Binary;
        }

        private const int MaxMessageBytes = 4 * 1024 * 1024;

        private readonly ConcurrentDictionary<string, IWebSocket> _clients =
            new ConcurrentDictionary<string, IWebSocket>();

        // Received 插件按“帧”触发，中继帧(Cont)要拼到 FIN 才是完整消息，所以每个会话各留一份缓存
        private readonly ConcurrentDictionary<string, FrameBuffer> _buffers =
            new ConcurrentDictionary<string, FrameBuffer>();

        private HttpService _service;
        private bool _disposed;

        public event Action<ServerClient> ClientConnected;
        public event Action<ServerClient> ClientClosed;
        public event Action<ServerClient, byte[]> DataReceived;

        /// <summary>
        /// 与 WsClientTransport.FrameReceived 对应：额外带出帧类型（文本/二进制）。
        /// </summary>
        public event Action<ServerClient, byte[], WsFrameKind> FrameReceived;

        public bool IsRunning => _service != null && _service.ServerState == ServerState.Running;

        public Task StartAsync(string address, int port, bool reuseAddress, CancellationToken token)
        {
            return StartAsync(address, port, "/ws", token);
        }

        public async Task StartAsync(string address, int port, string path, CancellationToken token)
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
                throw new InvalidOperationException("WebSocket 服务已经启动");
            }

            var service = new HttpService();
            _service = service;

            try
            {
                var host = new IPHost($"{address.Trim()}:{port}");
                var config = new TouchSocketConfig()
                    .SetListenIPHosts(host)
                    .SetReuseAddress(true)
                    .ConfigurePlugins(a =>
                    {
                        a.UseWebSocket(options =>
                        {
                            options.SetUrl(path);
                            options.AutoPong = true;
                        });

                        a.AddWebSocketConnectedPlugin(OnWebSocketConnected);
                        a.AddWebSocketReceivedPlugin(OnWebSocketReceived);
                        a.AddWebSocketClosedPlugin(OnWebSocketClosed);
                    });

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
                _buffers.Clear();
                service.Dispose();
                throw;
            }
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
            _buffers.Clear();

            try
            {
                await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"WebSocket 服务停止失败：{0}", e);
            }
            finally
            {
                service.Dispose();
            }
        }

        #region WebSocket 插件回调（服务端插件可能并发执行，必须线程安全）

        private Task OnWebSocketConnected(IWebSocket webSocket, HttpContextEventArgs e)
        {
            var session = webSocket.Client;
            var key = GetClientKey(session);

            _clients[key] = webSocket;
            ClientConnected?.Invoke(new ServerClient(key, session.IP, session.Port, GetRequestPath(e)));
            return EasyTask.CompletedTask;
        }

        private Task OnWebSocketReceived(IWebSocket webSocket, WSDataFrameEventArgs e)
        {
            CombineFrame(GetClientKey(webSocket.Client), e.DataFrame);
            return EasyTask.CompletedTask;
        }

        private Task OnWebSocketClosed(IWebSocket webSocket, ClosedEventArgs e)
        {
            var session = webSocket.Client;
            var key = GetClientKey(session);
            _buffers.TryRemove(key, out _);

            // 只有真正完成过握手的会话才通知上层，避免未升级的 HTTP 连接也冒出来
            if (_clients.TryGetValue(key, out var current) && ReferenceEquals(current, webSocket))
            {
                _clients.TryRemove(key, out _);
                ClientClosed?.Invoke(new ServerClient(key, session.IP, session.Port));
            }

            return EasyTask.CompletedTask;
        }

        /// <summary>
        /// 会话 Key。HttpSessionClient（IHttpSessionClient）没有 Id 属性，这里用 “IP:Port” 代替。
        /// </summary>
        private static string GetClientKey(IHttpSession session)
        {
            return $"{session.IP}:{session.Port}";
        }
        
        private static string GetRequestPath(HttpContextEventArgs e)
        {
            return e?.Context?.Request?.RelativeURL;
        }

        /// <summary>
        /// 把中继（Cont）帧拼成一条完整消息，逻辑与 WsClientTransport.AppendFrame 保持一致。
        /// </summary>
        private void CombineFrame(string clientId, WSDataFrame frame)
        {
            if (frame == null)
            {
                return;
            }

            // Ping / Pong / Close 由框架处理（已开 AutoPong），这里只关心数据帧
            if (!frame.IsText && !frame.IsBinary && frame.Opcode != WSDataType.Cont)
            {
                return;
            }

            var payload = frame.PayloadData;
            if (payload.Length == 0)
            {
                return;
            }

            var buffer = _buffers.GetOrAdd(clientId, id => new FrameBuffer());
            byte[] complete;
            WsFrameKind kind;

            lock (buffer.SyncRoot)
            {
                if (frame.IsText || frame.IsBinary)
                {
                    buffer.Buffer.Clear();
                    buffer.Kind = frame.IsText ? WsFrameKind.Text : WsFrameKind.Binary;
                }

                if (buffer.Buffer.Count + payload.Length > MaxMessageBytes)
                {
                    Console.WriteLine(@"WebSocket 消息超过最大长度 {0} 字节，已丢弃", MaxMessageBytes);
                    buffer.Buffer.Clear();
                    return;
                }

                buffer.Buffer.AddRange(payload.ToArray());

                if (!frame.FIN)
                {
                    return;
                }

                complete = buffer.Buffer.ToArray();
                kind = buffer.Kind;
                buffer.Buffer.Clear();
            }

            if (!_clients.TryGetValue(clientId, out var webSocket))
            {
                return;
            }

            var session = webSocket.Client;
            var client = new ServerClient(clientId, session.IP, session.Port);

            DataReceived?.Invoke(client, complete);
            FrameReceived?.Invoke(client, complete, kind);
        }

        #endregion WebSocket 插件回调

        public Task SendAsync(string clientId, byte[] payload)
        {
            return SendAsync(clientId, payload, WsFrameKind.Binary);
        }

        /// <summary>
        /// 按指定帧类型发送：HEX 走二进制帧，ASCII 走文本帧。
        /// </summary>
        public async Task SendAsync(string clientId, byte[] payload, WsFrameKind kind)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (!_clients.TryGetValue(clientId, out var webSocket))
            {
                throw new InvalidOperationException($"客户端 {clientId} 不存在或已断开");
            }

            // WSDataFrame 是一次性对象，发送结束后要及时释放
            var frame = new WSDataFrame(payload)
            {
                Opcode = kind == WsFrameKind.Text ? WSDataType.Text : WSDataType.Binary
            };
            await webSocket.SendAsync(frame, true, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task DisconnectAll()
        {
            foreach (var pair in _clients)
            {
                await CloseClient(pair.Value).ConfigureAwait(false);
            }
        }

        public async Task DisconnectClient(string clientId)
        {
            if (_clients.TryGetValue(clientId, out var webSocket))
            {
                await CloseClient(webSocket).ConfigureAwait(false);
            }
        }

        private static async Task CloseClient(IWebSocket webSocket)
        {
            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "服务端主动断开", CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"WebSocket 断开客户端异常：{0}", e);
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
            _buffers.Clear();

            service?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WsServerTransport));
            }
        }
    }
}
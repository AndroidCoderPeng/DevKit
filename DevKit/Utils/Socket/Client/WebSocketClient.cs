using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using HandyControl.Controls;

namespace DevKit.Utils.Socket.Client
{
    public class WebSocketClient
    {
        private readonly Bootstrap _bootStrap = new Bootstrap();
        private readonly MultithreadEventLoopGroup _loopGroup = new MultithreadEventLoopGroup();
        private string _url = string.Empty;
        private IChannel _channel;
        private bool _isRunning;

        public WebSocketClientDelegateAggregator.ConnectedEventHandler OnConnected { get; set; }
        public WebSocketClientDelegateAggregator.DisconnectedEventHandler OnDisconnected { get; set; }
        public WebSocketClientDelegateAggregator.ConnectFailedEventHandler OnConnectFailed { get; set; }
        public WebSocketClientDelegateAggregator.DataReceivedEventHandler OnDataReceived { get; set; }

        public WebSocketClient()
        {
            _bootStrap.Group(_loopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true) //无阻塞
                .Option(ChannelOption.SoKeepalive, true) //长连接
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(10240, 51200, 102400))
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    var clientHandShaker = WebSocketClientHandshakerFactory.NewHandshaker(
                        new Uri(_url), WebSocketVersion.V13, null, true, new DefaultHttpHeaders()
                    );
                    channel.Pipeline
                        .AddLast(new HttpClientCodec())
                        .AddLast(new HttpObjectAggregator(8192))
                        .AddLast(new WebSocketClientProtocolHandler(clientHandShaker))
                        .AddLast(new IdleStateHandler(0, 0, 60))
                        .AddLast(new WebSocketChannelInboundHandler(this));
                }));
        }

        private class WebSocketChannelInboundHandler : SimpleChannelInboundHandler<object>
        {
            private readonly WebSocketClient _webSocketClient;

            public WebSocketChannelInboundHandler(WebSocketClient webSocketClient)
            {
                _webSocketClient = webSocketClient;
            }

            public override void ChannelActive(IChannelHandlerContext context)
            {
                var address = context.Channel.RemoteAddress;
                if (address is IPEndPoint endPoint)
                {
                    Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已连接");
                }

                _webSocketClient.OnConnected(this, context);
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                var address = context.Channel.RemoteAddress;
                if (address is IPEndPoint endPoint)
                {
                    Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已断开");
                }

                _webSocketClient.OnDisconnected(this, context);
            }

            protected override void ChannelRead0(IChannelHandlerContext context, object msg)
            {
                if (msg is TextWebSocketFrame textFrame)
                {
                    _webSocketClient.OnDataReceived(this, textFrame.Text());
                }
                else if (msg is CloseWebSocketFrame)
                {
                    _webSocketClient.OnDisconnected(this, context);
                    context.CloseAsync();
                }
            }

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                _webSocketClient.OnConnectFailed(this, exception);
                context.CloseAsync();
            }
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public void Start(string url)
        {
            _url = url;
            if (_isRunning)
            {
                return;
            }

            Connect();
        }

        public void Close()
        {
            _channel.CloseAsync();
            _isRunning = false;
        }

        private void Connect()
        {
            if (_channel != null && _channel.Active)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var task = _bootStrap.ConnectAsync();
                    if (task.Result.Active)
                    {
                        _isRunning = true;
                        _channel = task.Result;
                    }
                }
                catch (Exception)
                {
                    Application.Current.Dispatcher.Invoke(() => { Growl.Error("连接服务端失败，请检查网络"); });
                }
            });
        }

        public void SendAsync(object message)
        {
            _channel.WriteAndFlushAsync(message);
        }
    }
}
using System;
using System.Net;
using System.Threading.Tasks;
using DevKit.Utils.SocketBase;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DevKit.Utils
{
    public class TcpClient : ITcpClient
    {
        private readonly Bootstrap _bootStrap = new Bootstrap();
        private readonly MultithreadEventLoopGroup _loopGroup = new MultithreadEventLoopGroup();
        private string _host;
        private int _port;
        private IChannel _channel;
        private bool _isRunning;

        public ConnectedEventHandler OnConnected { get; set; }
        public DisconnectedEventHandler OnDisconnected { get; set; }
        public ConnectFailedEventHandler OnConnectFailed { get; set; }
        public DataReceivedEventHandler OnDataReceived { get; set; }

        public TcpClient()
        {
            _bootStrap.Group(_loopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true) //无阻塞
                .Option(ChannelOption.SoKeepalive, true) //长连接
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(1024, 5120, 10240))
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Handler(new SimpleChannelInitializer<ISocketChannel>(this));
        }

        /// <summary>
        /// 通道初始化
        /// </summary>
        private class SimpleChannelInitializer<T> : ChannelInitializer<T> where T : ISocketChannel
        {
            private readonly TcpClient _tcpClient;

            public SimpleChannelInitializer(TcpClient tcpClient)
            {
                _tcpClient = tcpClient;
            }

            protected override void InitChannel(T channel)
            {
                channel.Pipeline
                    .AddLast(new ByteArrayDecoder())
                    .AddLast(new ByteArrayEncoder())
                    .AddLast(new IdleStateHandler(0, 0, 60))
                    .AddLast(new TcpChannelInboundHandler(_tcpClient));
            }

            /// <summary>
            /// 消息适配器
            /// </summary>
            private class TcpChannelInboundHandler : SimpleChannelInboundHandler<byte[]>
            {
                private readonly TcpClient _tcpClient;

                public TcpChannelInboundHandler(TcpClient tcpClient)
                {
                    _tcpClient = tcpClient;
                }

                public override void ChannelActive(IChannelHandlerContext context)
                {
                    var address = context.Channel.RemoteAddress;
                    if (address is IPEndPoint endPoint)
                    {
                        Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已连接");
                    }

                    _tcpClient.OnConnected(this, context);
                }

                public override void ChannelInactive(IChannelHandlerContext context)
                {
                    var address = context.Channel.RemoteAddress;
                    if (address is IPEndPoint endPoint)
                    {
                        Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已断开");
                    }

                    _tcpClient.OnDisconnected(this, context);
                }

                protected override void ChannelRead0(IChannelHandlerContext ctx, byte[] msg)
                {
                    _tcpClient.OnDataReceived(this, msg);
                }

                public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
                {
                    _tcpClient.OnConnectFailed(this, exception);
                    context.CloseAsync();
                }
            }
        }

        /// <summary>
        /// TcpClient 是否正在运行
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            return _isRunning;
        }

        public void Start(string host, int port)
        {
            _host = host;
            _port = port;
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
                    var task = _bootStrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(_host), _port));
                    if (task.Result.Active)
                    {
                        _isRunning = true;
                        _channel = task.Result;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
        }

        public void SendAsync(object message)
        {
            _channel.WriteAndFlushAsync(message);
        }
    }
}
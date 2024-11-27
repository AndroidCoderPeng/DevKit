using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Utils.Socket.Base;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using HandyControl.Controls;

namespace DevKit.Utils.Socket.Server
{
    public class TcpServer
    {
        private readonly MultithreadEventLoopGroup _bossGroup = new MultithreadEventLoopGroup();
        private readonly MultithreadEventLoopGroup _workerGroup = new MultithreadEventLoopGroup();
        private readonly ServerBootstrap _serverBootstrap = new ServerBootstrap();
        private string _host;
        private int _port;
        private ListenStateDelegate _stateDelegate;
        private IChannel _channel;
        private bool _isRunning;

        public delegate void ListenStateDelegate(int state);

        public TcpServerDelegateAggregator.ConnectedEventHandler OnConnected { get; set; }
        public TcpServerDelegateAggregator.DisconnectedEventHandler OnDisconnected { get; set; }
        public TcpServerDelegateAggregator.ConnectFailedEventHandler OnConnectFailed { get; set; }
        public TcpServerDelegateAggregator.DataReceivedEventHandler OnDataReceived { get; set; }

        public TcpServer()
        {
            _serverBootstrap.Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 1024)
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                .ChildHandler(new SimpleChannelInitializer<TcpServerSocketChannel>(this));
        }

        private class SimpleChannelInitializer<T> : ChannelInitializer<T> where T : TcpServerSocketChannel
        {
            private readonly TcpServer _tcpServer;

            public SimpleChannelInitializer(TcpServer tcpServer)
            {
                _tcpServer = tcpServer;
            }

            protected override void InitChannel(T channel)
            {
                channel.Pipeline
                    .AddLast(new ByteArrayDecoder())
                    .AddLast(new ByteArrayEncoder())
                    .AddLast(new TcpChannelInboundHandler(_tcpServer));
            }

            private class TcpChannelInboundHandler : SimpleChannelInboundHandler<byte[]>
            {
                private readonly TcpServer _tcpServer;

                public TcpChannelInboundHandler(TcpServer tcpServer)
                {
                    _tcpServer = tcpServer;
                }

                public override void ChannelActive(IChannelHandlerContext context)
                {
                    var address = context.Channel.RemoteAddress;
                    if (address is IPEndPoint endPoint)
                    {
                        Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已连接");
                    }

                    _tcpServer.OnConnected(this, context);
                }

                public override void ChannelInactive(IChannelHandlerContext context)
                {
                    var address = context.Channel.RemoteAddress;
                    if (address is IPEndPoint endPoint)
                    {
                        Console.WriteLine($@"{endPoint.Address.MapToIPv4()} 已断开");
                    }

                    _tcpServer.OnDisconnected(this, context);
                }

                protected override void ChannelRead0(IChannelHandlerContext ctx, byte[] msg)
                {
                    _tcpServer.OnDataReceived(this, msg);
                }

                public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
                {
                    _tcpServer.OnConnectFailed(this, exception);
                    context.CloseAsync();
                }
            }
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public void StartListen(string host, int port, ListenStateDelegate stateDelegate)
        {
            _host = host;
            _port = port;
            _stateDelegate = stateDelegate;
            if (_isRunning)
            {
                return;
            }

            ListenLocalPort();
        }

        private void ListenLocalPort()
        {
            if (_channel != null && _channel.Active)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var task = _serverBootstrap.BindAsync(new IPEndPoint(IPAddress.Parse(_host), _port));
                    if (task.Result.Active)
                    {
                        _stateDelegate(1);
                        _isRunning = true;
                        _channel = task.Result;
                        _channel.CloseAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Application.Current.Dispatcher.Invoke(() => { Growl.Error("本地端口存在冲突，启动TCP服务失败"); });
                    _stateDelegate(0);
                }
            });
        }

        public void StopListen()
        {
            _channel.CloseAsync();
            _channel = null;
            _isRunning = false;
            _stateDelegate(0);
        }

        public void SendAsync(object message)
        {
            _channel.WriteAndFlushAsync(message);
        }
    }
}
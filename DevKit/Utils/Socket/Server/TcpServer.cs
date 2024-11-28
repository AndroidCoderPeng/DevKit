using System;
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
        private readonly MultithreadEventLoopGroup _bossGroup = new MultithreadEventLoopGroup(1);
        private readonly MultithreadEventLoopGroup _workerGroup = new MultithreadEventLoopGroup();
        private readonly ServerBootstrap _serverBootstrap = new ServerBootstrap();
        private int _port;
        private ListenStateDelegate _stateDelegate;
        private IChannel _channel;
        private bool _isRunning;

        public delegate void ListenStateDelegate(int state);

        public TcpServerDelegateAggregator.ConnectedEventHandler OnConnected { get; set; }
        public TcpServerDelegateAggregator.DisconnectedEventHandler OnDisconnected { get; set; }
        public TcpServerDelegateAggregator.DataReceivedEventHandler OnDataReceived { get; set; }

        public TcpServer()
        {
            _serverBootstrap.Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 1024)
                .Option(ChannelOption.SoKeepalive, true)
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline
                        .AddLast(new ByteArrayDecoder())
                        .AddLast(new ByteArrayEncoder())
                        .AddLast(new TcpChannelInboundHandler(this));
                }));
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
                _tcpServer.OnConnected(this, context);
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                _tcpServer.OnDisconnected(this, context);
            }

            protected override void ChannelRead0(IChannelHandlerContext ctx, byte[] msg)
            {
                _tcpServer.OnDataReceived(this, ctx, msg);
            }

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                context.CloseAsync();
            }
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public void StartListen(int port, ListenStateDelegate stateDelegate)
        {
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

            try
            {
                Task.Run(() =>
                {
                    var bindTask = _serverBootstrap.BindAsync(_port);
                    bindTask.Wait();
                    _channel = bindTask.Result;
                    _stateDelegate(1);
                    _isRunning = true;
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Application.Current.Dispatcher.Invoke(() => { Growl.Error("本地端口存在冲突，启动TCP服务失败"); });
                _stateDelegate(0);
            }
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
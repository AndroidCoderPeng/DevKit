using System;
using System.Net;
using DevKit.Utils.Socket.Base;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DevKit.Utils.Socket.Server
{
    public class TcpServer
    {
        private readonly MultithreadEventLoopGroup _bossGroup = new MultithreadEventLoopGroup();
        private readonly MultithreadEventLoopGroup _workerGroup = new MultithreadEventLoopGroup();
        private readonly ServerBootstrap _serverBootstrap = new ServerBootstrap();

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
                .Handler(new SimpleChannelInitializer<TcpServerSocketChannel>(this));
            var channel = _serverBootstrap.BindAsync().Result;
            channel.CloseAsync();
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
    }
}
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Utils.Socket.Base;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using HandyControl.Controls;

namespace DevKit.Utils.Socket.Client
{
    public class TcpClient
    {
        private readonly Bootstrap _bootStrap = new Bootstrap();
        private readonly MultithreadEventLoopGroup _loopGroup = new MultithreadEventLoopGroup();
        private string _host;
        private int _port;
        private IChannel _channel;
        private bool _isRunning;

        public TcpClientDelegateAggregator.ConnectedEventHandler OnConnected { get; set; }
        public TcpClientDelegateAggregator.DisconnectedEventHandler OnDisconnected { get; set; }
        public TcpClientDelegateAggregator.ConnectFailedEventHandler OnConnectFailed { get; set; }
        public TcpClientDelegateAggregator.DataReceivedEventHandler OnDataReceived { get; set; }

        public TcpClient()
        {
            _bootStrap.Group(_loopGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true) //无阻塞
                .Option(ChannelOption.SoKeepalive, true) //长连接
                .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(10240, 51200, 102400))
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(5))
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline
                        .AddLast(new ByteArrayDecoder())
                        .AddLast(new ByteArrayEncoder())
                        .AddLast(new IdleStateHandler(0, 0, 60))
                        .AddLast(new TcpChannelInboundHandler(this));
                }));
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
                _tcpClient.OnConnected(this, context);
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
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
            if (_channel == null)
            {
                return;
            }

            _channel.CloseAsync();
            _channel = null;
            _isRunning = false;
        }

        private void Connect()
        {
            if (_channel != null && _channel.Active)
            {
                return;
            }

            try
            {
                Task.Run(async () =>
                {
                    _channel = await _bootStrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(_host), _port));
                    _isRunning = true;
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Application.Current.Dispatcher.Invoke(() => { Growl.Error("连接服务端失败，请检查网络"); });
            }
        }

        public void SendAsync(byte[] bytes)
        {
            _channel.WriteAndFlushAsync(bytes);
        }

        public void SendAsync(string msg)
        {
            _channel.WriteAndFlushAsync(msg);
        }
    }
}
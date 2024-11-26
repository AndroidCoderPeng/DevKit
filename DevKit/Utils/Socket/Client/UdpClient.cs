using System.Net;
using System.Threading.Tasks;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DevKit.Utils.Socket.Client
{
    public class UdpClient
    {
        private readonly Bootstrap _bootStrap = new Bootstrap();
        private readonly MultithreadEventLoopGroup _loopGroup = new MultithreadEventLoopGroup();
        private IPEndPoint _endPoint;
        private IChannel _channel;

        public UdpClientDelegate.DataReceivedEventHandler OnDataReceived { get; set; }

        public UdpClient()
        {
            _bootStrap.Group(_loopGroup)
                .Channel<SocketDatagramChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoRcvbuf, 51200)
                .Option(ChannelOption.SoSndbuf, 51200)
                .Handler(new SimpleChannelInitializer<SocketDatagramChannel>(this));
        }

        private class SimpleChannelInitializer<T> : ChannelInitializer<T> where T : SocketDatagramChannel
        {
            private readonly UdpClient _udpClient;

            public SimpleChannelInitializer(UdpClient udpClient)
            {
                _udpClient = udpClient;
            }

            protected override void InitChannel(T channel)
            {
                channel.Pipeline
                    .AddLast(new IdleStateHandler(0, 0, 60))
                    .AddLast(new UdpChannelInboundHandler(_udpClient));
            }

            private class UdpChannelInboundHandler : SimpleChannelInboundHandler<DatagramPacket>
            {
                private readonly UdpClient _udpClient;

                public UdpChannelInboundHandler(UdpClient udpClient)
                {
                    _udpClient = udpClient;
                }

                protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket msg)
                {
                    var byteBuffer = msg.Content;
                    var bytes = new byte[byteBuffer.ReadableBytes];
                    byteBuffer.ReadBytes(bytes);
                    _udpClient.OnDataReceived(this, bytes);
                }
            }
        }

        public void BindRemote(string host, int port)
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            if (_channel != null && _channel.Active)
            {
                return;
            }

            Task.Run(() =>
            {
                var task = _bootStrap.ConnectAsync(_endPoint);
                if (task.Result.Active)
                {
                    _channel = task.Result;
                }
            });
        }
        
        public void SendAsync(object message)
        {
            _channel.WriteAndFlushAsync(message);
        }
    }
}
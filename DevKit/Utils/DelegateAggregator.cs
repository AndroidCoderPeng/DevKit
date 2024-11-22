using System;
using DotNetty.Transport.Channels;

namespace DevKit.Utils
{
    public class TcpClientDelegateAggregator
    {
        public delegate void ConnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DisconnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);

        public delegate void ConnectFailedEventHandler(object sender, Exception exception);
    }

    public class TcpServerDelegateAggregator
    {
        public delegate void ConnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DisconnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);

        public delegate void ConnectFailedEventHandler(object sender, Exception exception);
    }

    class UdpClientDelegate
    {
        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);
    }
    
    class UdpServerDelegate
    {
        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);
    }
    
    public class WebSocketClientDelegateAggregator
    {
        public delegate void ConnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DisconnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);

        public delegate void ConnectFailedEventHandler(object sender, Exception exception);
    }

    public class WebSocketServerDelegateAggregator
    {
        public delegate void ConnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DisconnectedEventHandler(object sender, IChannelHandlerContext context);

        public delegate void DataReceivedEventHandler(object sender, byte[] bytes);

        public delegate void ConnectFailedEventHandler(object sender, Exception exception);
    }
}
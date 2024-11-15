using System;
using DotNetty.Transport.Channels;

namespace DevKit.Utils
{
    public delegate void ConnectedEventHandler(object sender, IChannelHandlerContext context);

    public delegate void DisconnectedEventHandler(object sender, IChannelHandlerContext context);

    public delegate void DataReceivedEventHandler(object sender, byte[] message);
    
    public delegate void ConnectFailedEventHandler(object sender, Exception exception);
}
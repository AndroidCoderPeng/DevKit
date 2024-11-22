using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace DevKit.Utils.Socket.Base
{
    /// <summary>
    /// 字节码解码器
    /// </summary>
    public class ByteArrayDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            var array = new byte[input.ReadableBytes];
            input.ReadBytes(array);
            output.Add(array);
        }
    }
}
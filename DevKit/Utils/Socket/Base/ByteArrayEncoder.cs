using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace DevKit.Utils.Socket.Base
{
    /// <summary>
    /// 字符串编码器
    /// </summary>
    public class ByteArrayEncoder : MessageToByteEncoder<string>
    {
        protected override void Encode(IChannelHandlerContext context, string message, IByteBuffer output)
        {
            var contentBytes = Encoding.UTF8.GetBytes(message);
            output.WriteInt(contentBytes.Length);
            output.WriteBytes(contentBytes);
        }
    }
}
using SQLite;

namespace DevKit.Cache
{
    [Table("ClientMessageCache")]
    public class ClientMessageCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string ClientId { get; set; }

        public string ClientIp { get; set; }

        public int ClientPort { get; set; }

        /// <summary>
        /// 消息文本内容
        /// </summary>
        public string MessageContent { get; set; }

        /// <summary>
        /// 消息原始内容Byte[]的文本形式
        /// </summary>
        public string ByteArrayContent { get; set; }

        public string Time { get; set; }

        public int IsSend { get; set; }
    }
}
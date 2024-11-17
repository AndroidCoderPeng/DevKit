using SQLite;

namespace DevKit.Cache
{
    [Table("TcpClientConfigCache")]
    public class TcpClientConfigCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }
        
        public string RemoteAddress { get; set; }

        public int RemotePort { get; set; }

        public int SendHex { get; set; }

        public int ShowHex { get; set; }
    }
}
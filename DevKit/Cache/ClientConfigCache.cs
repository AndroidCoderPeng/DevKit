using SQLite;

namespace DevKit.Cache
{
    [Table("ClientConfigCache")]
    public class ClientConfigCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string ClientType { get; set; }

        public string RemoteAddress { get; set; }

        public int RemotePort { get; set; }

        public string WebSocketPath { get; set; }
    }
}
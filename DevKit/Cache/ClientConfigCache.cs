using SQLite;

namespace DevKit.Cache
{
    [Table("ClientConfigCache")]
    public class ClientConfigCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public int Type { get; set; }

        public string RemoteAddress { get; set; }

        public int RemotePort { get; set; }

        public int SendHex { get; set; }

        public int ShowHex { get; set; }
    }
}
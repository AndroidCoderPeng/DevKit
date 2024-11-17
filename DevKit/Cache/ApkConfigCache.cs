using SQLite;

namespace DevKit.Cache
{
    [Table("ApkConfigCache")]
    public class ApkConfigCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string KeyPath { get; set; }

        public string Alias { get; set; }

        public string Password { get; set; }

        public string ApkRootFolder { get; set; }
    }
}
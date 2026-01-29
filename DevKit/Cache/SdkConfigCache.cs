using SQLite;

namespace DevKit.Cache
{
    [Table("SdkConfigCache")]
    public class SdkConfigCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string NdkPath { get; set; }
        public string SdkPath { get; set; }
    }
}
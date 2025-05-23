using SQLite;

namespace DevKit.Cache
{
    [Table("ExCommandCache")]
    public class ExCommandCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string ClientType { get; set; }
        
        public string CommandValue { get; set; }

        public string Annotation { get; set; }
    }
}
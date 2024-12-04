using SQLite;

namespace DevKit.Cache
{
    [Table("ExCommandCache")]
    public class ExCommandCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// TCP Client/Server、UDP Client/Server、WebSocket Client/Server、串口
        /// </summary>
        public int ParentType { get; set; }

        public string CommandValue { get; set; }

        public int IsHex { get; set; }

        public string Annotation { get; set; }
    }
}
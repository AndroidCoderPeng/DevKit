using SQLite;

namespace DevKit.Cache
{
    [Table("CommandExtensionCache")]
    public class CommandExtensionCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// 父类ID，对应不同类型父类的主键
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// TCP Client/Server、UDP Client/Server、WebSocket Client/Server、串口
        /// </summary>
        public int ParentType { get; set; }

        public string Command { get; set; }

        public int IsHex { get; set; }

        public string Annotation { get; set; }
    }
}
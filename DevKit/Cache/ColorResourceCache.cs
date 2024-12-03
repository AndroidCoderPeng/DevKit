using SQLite;

namespace DevKit.Cache
{
    [Table("ColorResourceCache")]
    public class ColorResourceCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// 色系
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// 颜色名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 颜色值
        /// </summary>
        public string Hex { get; set; }
    }
}
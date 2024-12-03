using SQLite;

namespace DevKit.Cache
{
    [Table("GradientColorResCache")]
    public class GradientColorResCache
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string FirstColor { get; set; }
        public string SecondColor { get; set; }
        public string ThirdColor { get; set; }
        public string FourthColor { get; set; }
    }
}
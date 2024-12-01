using System.Windows.Media.Imaging;

namespace DevKit.Models
{
    public class PlatformImageTypeModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string AndroidSizeTag { get; set; }
        public BitmapImage ResultImage { get; set; }
    }
}
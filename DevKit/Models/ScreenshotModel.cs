using System;
using System.Windows.Media;
using Prism.Mvvm;

namespace DevKit.Models
{
    public class ScreenshotModel : BindableBase
    {
        public string FilePath { get; set; }

        private DateTime _time;

        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                TimeText = value == DateTime.MinValue ? "时间未知" : value.ToString("yyyy-MM-dd HH:mm:ss");
                RaisePropertyChanged();
            }
        }

        private string _timeText;

        public string TimeText
        {
            get => _timeText;
            set => SetProperty(ref _timeText, value);
        }

        private string _sizeText = "";

        public string SizeText
        {
            get => _sizeText;
            set => SetProperty(ref _sizeText, value);
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private ImageSource _thumbnail;

        public ImageSource Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        /// <summary>缩略图在电脑上的缓存路径（也是原图，供预览）</summary>
        public string CachePath { get; set; }
    }
}
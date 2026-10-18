using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.Models
{
    public partial class ScreenshotFile : ObservableObject
    {
        public string FilePath { get; set; }

        [ObservableProperty] private DateTime _time;
        [ObservableProperty] private string _timeText;
        [ObservableProperty] private string _sizeText = "";
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private ImageSource _thumbnail;

        /// <summary>缩略图在电脑上的缓存路径（也是原图，供预览）</summary>
        public string CachePath { get; set; }
    }
}
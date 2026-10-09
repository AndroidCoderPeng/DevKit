using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DevKit.Cache;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Color = System.Windows.Media.Color;

namespace DevKit.ViewModels
{
    public class ColorResourceViewModel : BindableBase, IDialogAware
    {
        public string Title => "颜色处理";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.RecentlyColor, new RecentlyColorConfig
            {
                // 只保存最近的9个颜色
                Colors = _recentlyColors.Take(9).ToList()
            });
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private enum ChannelType
        {
            Alpha,
            Red,
            Green,
            Blue
        }

        // 单一状态源
        private Color _currentColor = Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6);

        #region VM

        private bool _isRgbToHexSelected = true;

        public bool IsRgbToHexSelected
        {
            get => _isRgbToHexSelected;
            set
            {
                _isRgbToHexSelected = value;
                RaisePropertyChanged();
            }
        }

        public bool IsHexToRgbSelected
        {
            get => !IsRgbToHexSelected;
            set
            {
                if (value == IsHexToRgbSelected) return;
                IsRgbToHexSelected = !value;
            }
        }

        private bool _isAlphaBoxChecked = true;

        public bool IsAlphaBoxChecked
        {
            get => _isAlphaBoxChecked;
            set
            {
                _isAlphaBoxChecked = value;
                RaisePropertyChanged();
            }
        }

        // 纯黑不透明
        private SolidColorBrush _colorViewBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x0E, 0x0C, 0x06));

        public SolidColorBrush ColorViewBrush
        {
            get => _colorViewBrush;
            set
            {
                _colorViewBrush = value;
                RaisePropertyChanged();
            }
        }

        private string _colorHexValue;

        public string ColorHexValue
        {
            get => _colorHexValue;
            set
            {
                _colorHexValue = value;
                RaisePropertyChanged();
            }
        }

        private int _alphaValue = 255;

        public int AlphaValue
        {
            get => _alphaValue;
            set
            {
                _alphaValue = value;
                RaisePropertyChanged();
            }
        }

        public int AlphaRatioValue => (int)Math.Round(AlphaValue * 100.0 / 255);

        private double _redColorValue;

        public double RedColorValue
        {
            get => _redColorValue;
            set
            {
                _redColorValue = value;
                RaisePropertyChanged();
            }
        }

        private double _greenColorValue;

        public double GreenColorValue
        {
            get => _greenColorValue;
            set
            {
                _greenColorValue = value;
                RaisePropertyChanged();
            }
        }

        private double _blueColorValue;

        public double BlueColorValue
        {
            get => _blueColorValue;
            set
            {
                _blueColorValue = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _recentlyColors = new ObservableCollection<string>();

        public ObservableCollection<string> RecentlyColors
        {
            get => _recentlyColors;
            set
            {
                _recentlyColors = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ColorResourceCache> _colorResources =
            new ObservableCollection<ColorResourceCache>();

        public ObservableCollection<ColorResourceCache> ColorResources
        {
            get => _colorResources;
            set
            {
                _colorResources = value;
                RaisePropertyChanged();
            }
        }

        private string _keyword = string.Empty;

        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand RandomColorCommand { set; get; }
        public DelegateCommand ResetCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<string> ColorHexTextChangedCommand { set; get; }
        public DelegateCommand<string> AlphaColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RedColorTextChangedCommand { set; get; }
        public DelegateCommand<string> GreenColorTextChangedCommand { set; get; }
        public DelegateCommand<string> BlueColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RecentlyColorSelectedCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private bool _isUpdatingColor;

        public ColorResourceViewModel()
        {
            // 根据默认颜色设置 Slider 的值
            if (_isAlphaBoxChecked)
            {
                AlphaValue = _colorViewBrush.Color.A;
            }

            RedColorValue = _colorViewBrush.Color.R;
            GreenColorValue = _colorViewBrush.Color.G;
            BlueColorValue = _colorViewBrush.Color.B;

            // 加载最近使用的颜色
            var config = SettingsStore.Load<RecentlyColorConfig>(ConfigSections.FileName, ConfigSections.RecentlyColor);
            RecentlyColors = new ObservableCollection<string>(config.Colors);

            // 加载颜色资源缓存
            _ = LoadColorResourcesAsync();
        }

        private async Task LoadColorResourcesAsync()
        {
            try
            {
                var colorResCaches = await Task.Run(() =>
                {
                    using (var dataBase = new DataBaseConnection())
                    {
                        return dataBase.Table<ColorResourceCache>().ToList();
                    }
                });

                const int batchSize = 32;
                for (var index = 0; index < colorResCaches.Count; index += batchSize)
                {
                    var batch = colorResCaches.Skip(index).Take(batchSize).ToList();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var color in batch)
                        {
                            ColorResources.Add(color);
                        }
                    });

                    // 让 ListBox 有机会逐批渲染
                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        
        private void RefreshColor()
        {
            if (_isUpdatingColor)
            {
                return;
            }

            _isUpdatingColor = true;

            try
            {
                var alpha = IsAlphaBoxChecked ? (byte)AlphaValue : byte.MaxValue;
                var red = (byte)Clamp((int)RedColorValue);
                var green = (byte)Clamp((int)GreenColorValue);
                var blue = (byte)Clamp((int)BlueColorValue);

                ColorViewBrush = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));

                var rgbHex = $"{red:X2}{green:X2}{blue:X2}";
                ColorHexValue = IsAlphaBoxChecked ? $"{(byte)AlphaValue:X2}{rgbHex}" : rgbHex;
            }
            finally
            {
                _isUpdatingColor = false;
            }
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private static string NormalizeHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Trim().TrimStart('#').Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        private void UpdateChannel(string value, ChannelType channel)
        {
            if (_isUpdatingColor || !TryParseChannel(value, out var result))
            {
                return;
            }

            switch (channel)
            {
                case ChannelType.Alpha:
                    AlphaValue = result;
                    break;

                case ChannelType.Red:
                    RedColorValue = result;
                    break;

                case ChannelType.Green:
                    GreenColorValue = result;
                    break;

                case ChannelType.Blue:
                    BlueColorValue = result;
                    break;
            }
        }

        private static bool TryParseChannel(string value, out int result)
        {
            if (!int.TryParse(value, out result))
            {
                result = 0;
                return false;
            }

            result = Clamp(result);
            return true;
        }

        private void ApplyColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var hex = NormalizeHex(value);

            if (hex.Length == 8)
            {
                AlphaValue = Convert.ToInt32(hex.Substring(0, 2), 16);
                hex = hex.Substring(2);
            }

            if (hex.Length != 6)
            {
                return;
            }

            RedColorValue = Convert.ToInt32(hex.Substring(0, 2), 16);
            GreenColorValue = Convert.ToInt32(hex.Substring(2, 2), 16);
            BlueColorValue = Convert.ToInt32(hex.Substring(4, 2), 16);

            RefreshColor();
            AddRecentlyColor(_colorHexValue);
        }

        private void AddRecentlyColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var color = value.ToUpperInvariant();
            var oldColor =
                RecentlyColors.FirstOrDefault(item => string.Equals(item, color, StringComparison.OrdinalIgnoreCase));

            if (oldColor != null)
            {
                RecentlyColors.Remove(oldColor);
            }

            RecentlyColors.Insert(0, color);
            while (RecentlyColors.Count > 9)
            {
                RecentlyColors.RemoveAt(RecentlyColors.Count - 1);
            }
        }
    }
}
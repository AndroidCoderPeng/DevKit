using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
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
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.History, new RecentlyColorConfig
            {
                // 只保存最近的9个颜色
                Colors = _recentlyColors.Take(9).ToList()
            });
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        // 单一状态源
        private Color _currentColor = Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6);

        private Color DisplayColor => IsAlphaBoxChecked
            ? _currentColor
            : Color.FromArgb(255, _currentColor.R, _currentColor.G, _currentColor.B);

        #region VM

        private bool _isRgbToHexSelected = true;

        public bool IsRgbToHexSelected
        {
            get => _isRgbToHexSelected;
            set
            {
                if (_isRgbToHexSelected == value)
                {
                    return;
                }

                _isRgbToHexSelected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsHexToRgbSelected));
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
                if (_isAlphaBoxChecked == value)
                {
                    return;
                }

                _isAlphaBoxChecked = value;
                RaisePropertyChanged();

                // 透明通道开关需要通知依赖它的属性
                RaisePropertyChanged(nameof(ColorViewBrush));
                var hexValue = FormatHex(_currentColor, IsAlphaBoxChecked);
                if (_colorHexValue != hexValue)
                {
                    _colorHexValue = hexValue;
                    RaisePropertyChanged(nameof(ColorHexValue));
                }
                RaisePropertyChanged(nameof(CurrentColorHex));
            }
        }

        public SolidColorBrush ColorViewBrush => new SolidColorBrush(DisplayColor);

        private string _colorHexValue = "FF2E7CF6";

        public string ColorHexValue
        {
            get => _colorHexValue;
            set
            {
                if (_colorHexValue == value)
                {
                    return;
                }

                _colorHexValue = value;
                ApplyHex(value);
            }
        }

        public string CurrentColorHex => $"#{FormatHex(_currentColor, IsAlphaBoxChecked)}";

        public int AlphaValue
        {
            get => _currentColor.A;
            set => UpdateColor(color => Color.FromArgb((byte)Clamp(value), color.R, color.G, color.B));
        }

        public int AlphaRatioValue => (100 - (int)Math.Round(AlphaValue * 100.0 / 255));

        public double RedColorValue
        {
            get => _currentColor.R;
            set => UpdateColor(color => Color.FromArgb(color.A, (byte)Clamp((int)value), color.G, color.B));
        }

        public double GreenColorValue
        {
            get => _currentColor.G;
            set => UpdateColor(color => Color.FromArgb(color.A, color.R, (byte)Clamp((int)value), color.B));
        }

        public double BlueColorValue
        {
            get => _currentColor.B;
            set => UpdateColor(color => Color.FromArgb(color.A, color.R, color.G, (byte)Clamp((int)value)));
        }

        private string _toastMessage;

        public string ToastMessage
        {
            get => _toastMessage;
            set
            {
                _toastMessage = value;
                RaisePropertyChanged();
            }
        }

        private bool _isToastVisible;

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set
            {
                _isToastVisible = value;
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
                ApplyColorResourceFilter();
            }
        }

        private int _displayColors;

        public int DisplayColors
        {
            get => _displayColors;
            private set
            {
                if (_displayColors == value)
                {
                    return;
                }

                _displayColors = value;
                RaisePropertyChanged();
            }
        }

        private string _keyword = string.Empty;

        public string Keyword
        {
            get => _keyword;
            set
            {
                if (_keyword == value)
                {
                    return;
                }

                _keyword = value;
                RaisePropertyChanged();
                ApplyColorResourceFilter();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand RandomColorCommand { set; get; }
        public DelegateCommand ResetCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<string> RecentlyColorSelectedCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private DispatcherTimer _toastTimer;
        private readonly Random _random = new Random();

        public ColorResourceViewModel()
        {
            // 加载最近使用的颜色
            var config = SettingsStore.Load<RecentlyColorConfig>(ConfigSections.FileName, ConfigSections.History);
            RecentlyColors = new ObservableCollection<string>(config.Colors);

            // 加载颜色资源缓存
            _ = LoadColorResourcesAsync();

            // 实现 Keyword 颜色筛选
            ApplyColorResourceFilter();

            RandomColorCommand = new DelegateCommand(() =>
            {
                _currentColor = Color.FromArgb(
                    (byte)_random.Next(0, 256),
                    (byte)_random.Next(0, 256),
                    (byte)_random.Next(0, 256),
                    (byte)_random.Next(0, 256));
                NotifyColorChanged();
            });

            ResetCommand = new DelegateCommand(() =>
            {
                _currentColor = Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6);
                NotifyColorChanged();
            });

            CopyColorHexValueCommand = new DelegateCommand(() =>
            {
                Clipboard.SetText(CurrentColorHex);
                ShowToast($"{CurrentColorHex} 已复制到剪贴板");
            });

            RecentlyColorSelectedCommand = new DelegateCommand<string>(value =>
            {
                if (ApplyHex(value))
                {
                    AddRecentlyColor(value);
                }
            });

            ColorItemClickedCommand = new DelegateCommand<ColorResourceCache>(item =>
            {
                if (item == null)
                {
                    return;
                }

                if (ApplyHex(item.Hex))
                {
                    Clipboard.SetText(CurrentColorHex);
                    AddRecentlyColor(CurrentColorHex);
                    ShowToast($"{CurrentColorHex} 已复制到剪贴板");
                }
            });
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

                        DisplayColors = ColorResources.Count;
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

        private void ApplyColorResourceFilter()
        {
            var view = CollectionViewSource.GetDefaultView(ColorResources);
            var keyword = (_keyword ?? string.Empty).Trim();

            if (keyword.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                DisplayColors = view.Cast<object>().Count();
                return;
            }

            view.Filter = item =>
            {
                if (!(item is ColorResourceCache color))
                {
                    return false;
                }

                return (color.Name?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                       || (color.Hex?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
            };

            view.Refresh();
            DisplayColors = view.Cast<object>().Count();
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void UpdateColor(Func<Color, Color> update)
        {
            var newColor = update(_currentColor);
            if (newColor == _currentColor)
            {
                return;
            }

            _currentColor = newColor;
            NotifyColorChanged();
        }

        private void NotifyColorChanged()
        {
            RaisePropertyChanged(nameof(ColorViewBrush));
            var hexValue = FormatHex(_currentColor, IsAlphaBoxChecked);
            if (_colorHexValue != hexValue)
            {
                _colorHexValue = hexValue;
                RaisePropertyChanged(nameof(ColorHexValue));
            }
            RaisePropertyChanged(nameof(CurrentColorHex));
            RaisePropertyChanged(nameof(AlphaValue));
            RaisePropertyChanged(nameof(AlphaRatioValue));
            RaisePropertyChanged(nameof(RedColorValue));
            RaisePropertyChanged(nameof(GreenColorValue));
            RaisePropertyChanged(nameof(BlueColorValue));
        }

        private static string FormatHex(Color color, bool includeAlpha)
        {
            var rgb = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            return includeAlpha ? $"{color.A:X2}{rgb}" : rgb;
        }

        private bool ApplyHex(string value)
        {
            if (!TryParseHex(value, out var newColor))
            {
                return false;
            }

            _currentColor = newColor;
            NotifyColorChanged();
            return true;
        }

        private static bool TryParseHex(string value, out Color color)
        {
            color = default(Color);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var hex = value.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
            {
                hex = hex.Substring(1);
            }

            if ((hex.Length != 6 && hex.Length != 8) || !hex.All(Uri.IsHexDigit))
            {
                return false;
            }

            var offset = 0;
            var alpha = byte.MaxValue;
            if (hex.Length == 8)
            {
                if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out alpha))
                {
                    return false;
                }

                offset = 2;
            }

            if (!byte.TryParse(hex.Substring(offset, 2), NumberStyles.HexNumber, null, out var red)
                || !byte.TryParse(hex.Substring(offset + 2, 2), NumberStyles.HexNumber, null, out var green)
                || !byte.TryParse(hex.Substring(offset + 4, 2), NumberStyles.HexNumber, null, out var blue))
            {
                return false;
            }

            color = Color.FromArgb(alpha, red, green, blue);
            return true;
        }

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
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
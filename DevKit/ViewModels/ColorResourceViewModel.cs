using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Newtonsoft.Json;
using Color = System.Windows.Media.Color;

namespace DevKit.ViewModels
{
    public partial class ColorResourceViewModel : ObservableObject
    {
        #region VM

        // 单一状态源
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ColorViewBrush))]
        [NotifyPropertyChangedFor(nameof(CurrentColorHex))]
        [NotifyPropertyChangedFor(nameof(AlphaValue))]
        [NotifyPropertyChangedFor(nameof(AlphaRatioValue))]
        [NotifyPropertyChangedFor(nameof(RedColorValue))]
        [NotifyPropertyChangedFor(nameof(GreenColorValue))]
        [NotifyPropertyChangedFor(nameof(BlueColorValue))]
        private Color _currentColor = Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6);

        private Color DisplayColor => IsAlphaBoxChecked
            ? CurrentColor
            : Color.FromArgb(255, CurrentColor.R, CurrentColor.G, CurrentColor.B);

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsHexToRgbSelected))]
        private bool _isRgbToHexSelected = true;

        public bool IsHexToRgbSelected
        {
            get => !IsRgbToHexSelected;
            set
            {
                if (value != IsHexToRgbSelected)
                {
                    IsRgbToHexSelected = !value;
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ColorViewBrush))]
        [NotifyPropertyChangedFor(nameof(CurrentColorHex))]
        private bool _isAlphaBoxChecked = true;

        public SolidColorBrush ColorViewBrush => new(DisplayColor);

        private string _colorHexValue = "FF2E7CF6";

        public string ColorHexValue
        {
            get => _colorHexValue;
            set
            {
                if (!SetProperty(ref _colorHexValue, value))
                {
                    return;
                }

                ApplyHex(value);
            }
        }

        public string CurrentColorHex => $"#{FormatHex(CurrentColor, IsAlphaBoxChecked)}";

        public int AlphaValue
        {
            get => CurrentColor.A;
            set => UpdateColor(color => Color.FromArgb((byte)Clamp(value), color.R, color.G, color.B));
        }

        public int AlphaRatioValue => 100 - (int)Math.Round(AlphaValue * 100.0 / 255);

        public double RedColorValue
        {
            get => CurrentColor.R;
            set => UpdateColor(color => Color.FromArgb(color.A, (byte)Clamp((int)value), color.G, color.B));
        }

        public double GreenColorValue
        {
            get => CurrentColor.G;
            set => UpdateColor(color => Color.FromArgb(color.A, color.R, (byte)Clamp((int)value), color.B));
        }

        public double BlueColorValue
        {
            get => CurrentColor.B;
            set => UpdateColor(color => Color.FromArgb(color.A, color.R, color.G, (byte)Clamp((int)value)));
        }

        [ObservableProperty] private string _toastMessage;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private ObservableCollection<string> _recentlyColors = [];
        [ObservableProperty] private ObservableCollection<ColorResource> _colorResources = [];
        [ObservableProperty] private string _keyword = string.Empty;

        partial void OnKeywordChanged(string value)
        {
            ApplyColorResourceFilter(value);
        }

        [ObservableProperty] private int _displayColors;

        #endregion

        private DispatcherTimer _toastTimer;
        private readonly Random _random = new();

        public ColorResourceViewModel()
        {
            // 加载最近使用的颜色
            var config = SettingsStore.Load<RecentlyColorConfig>(ConfigSections.FileName, ConfigSections.History);
            RecentlyColors = new ObservableCollection<string>(config.Colors);

            // 加载颜色资源缓存
            _ = LoadColorResourcesAsync();

            // 实现 Keyword 颜色筛选
            ApplyColorResourceFilter(Keyword);
        }

        private int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void UpdateColor(Func<Color, Color> update)
        {
            CurrentColor = update(CurrentColor);
        }
        
        partial void OnIsAlphaBoxCheckedChanged(bool value)
        {
            _ = value;
            SynchronizeColorHexValue();
        }

        partial void OnCurrentColorChanged(Color value)
        {
            _ = value;
            SynchronizeColorHexValue();
        }

        private void SynchronizeColorHexValue()
        {
            SetProperty(ref _colorHexValue, FormatHex(CurrentColor, IsAlphaBoxChecked),
                nameof(ColorHexValue));
        }
        
        private static string FormatHex(Color color, bool includeAlpha)
        {
            var rgb = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            return includeAlpha ? $"{color.A:X2}{rgb}" : rgb;
        }
        
        private async Task LoadColorResourcesAsync()
        {
            try
            {
                var colorResCaches = await Task.Run(() =>
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Colors.json");
                    if (!File.Exists(path))
                    {
                        return [];
                    }

                    var json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<List<ColorResource>>(json) ?? new List<ColorResource>();
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

        private void ApplyColorResourceFilter(string keyword)
        {
            var view = CollectionViewSource.GetDefaultView(ColorResources);
            if (keyword.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                DisplayColors = view.Cast<object>().Count();
                return;
            }

            view.Filter = item =>
            {
                if (item is not ColorResource color)
                {
                    return false;
                }

                return (color.Name?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                       || (color.Hex?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
            };

            view.Refresh();
            DisplayColors = view.Cast<object>().Count();
        }

        [RelayCommand]
        private void RandomColor()
        {
            CurrentColor = Color.FromArgb(
                (byte)_random.Next(0, 256),
                (byte)_random.Next(0, 256),
                (byte)_random.Next(0, 256),
                (byte)_random.Next(0, 256));
        }

        [RelayCommand]
        private void Reset()
        {
            CurrentColor = Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6);
        }

        [RelayCommand]
        private void CopyColorHexValue()
        {
            Clipboard.SetText(CurrentColorHex);
            ShowToast($"{CurrentColorHex} 已复制到剪贴板");
        }

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (_, _) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }

        [RelayCommand]
        private void RecentlyColorSelected(string value)
        {
            var color = value.ToUpperInvariant();
            if (ApplyHex(color))
            {
                AddRecentlyColor(color);
            }
        }

        private bool ApplyHex(string value)
        {
            if (!TryParseHex(value, out var newColor))
            {
                return false;
            }

            CurrentColor = newColor;
            return true;
        }

        private bool TryParseHex(string value, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var hex = value.Trim();
            if (hex.StartsWith('#'))
            {
                hex = hex[1..];
            }

            if ((hex.Length != 6 && hex.Length != 8) || !hex.All(Uri.IsHexDigit))
            {
                return false;
            }

            var offset = 0;
            var alpha = byte.MaxValue;
            if (hex.Length == 8)
            {
                if (!byte.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, null, out alpha))
                {
                    return false;
                }

                offset = 2;
            }

            if (!byte.TryParse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, null, out var red)
                || !byte.TryParse(hex.AsSpan(offset + 2, 2), NumberStyles.HexNumber, null, out var green)
                || !byte.TryParse(hex.AsSpan(offset + 4, 2), NumberStyles.HexNumber, null, out var blue))
            {
                return false;
            }

            color = Color.FromArgb(alpha, red, green, blue);
            return true;
        }

        [RelayCommand]
        private void ColorItemClicked(ColorResource item)
        {
            if (item == null)
            {
                return;
            }

            if (!ApplyHex(item.Hex)) return;

            Clipboard.SetText(CurrentColorHex);
            AddRecentlyColor(CurrentColorHex);
            ShowToast($"{CurrentColorHex} 已复制到剪贴板");
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

        public void SaveConfiguration()
        {
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.History, new RecentlyColorConfig
            {
                Colors = [.. RecentlyColors.Take(9)]
            });
        }
    }
}
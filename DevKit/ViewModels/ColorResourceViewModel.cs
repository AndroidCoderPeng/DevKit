using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DevKit.Cache;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class ColorResourceViewModel : BindableBase, IDialogAware
    {
        public string Title => "颜色值转换";

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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _userInputColorHexValue = string.Empty;

        public string UserInputColorHexValue
        {
            set
            {
                _userInputColorHexValue = value;
                RaisePropertyChanged();
            }
            get => _userInputColorHexValue;
        }

        private byte _redColor;

        public byte RedColor
        {
            set
            {
                _redColor = value;
                RaisePropertyChanged();
            }
            get => _redColor;
        }

        private byte _greenColor;

        public byte GreenColor
        {
            set
            {
                _greenColor = value;
                RaisePropertyChanged();
            }
            get => _greenColor;
        }

        private byte _blueColor;

        public byte BlueColor
        {
            set
            {
                _blueColor = value;
                RaisePropertyChanged();
            }
            get => _blueColor;
        }

        private SolidColorBrush _colorViewBrush;

        public SolidColorBrush ColorViewBrush
        {
            set
            {
                _colorViewBrush = value;
                RaisePropertyChanged();
            }
            get => _colorViewBrush;
        }

        private byte _sliderValue = 255;

        public byte SliderValue
        {
            set
            {
                _sliderValue = value;
                RaisePropertyChanged();
            }
            get => _sliderValue;
        }

        private string _colorHexValue = string.Empty;

        public string ColorHexValue
        {
            set
            {
                _colorHexValue = value;
                RaisePropertyChanged();
            }
            get => _colorHexValue;
        }

        private bool _isAlphaBoxChecked;

        public bool IsAlphaBoxChecked
        {
            set
            {
                _isAlphaBoxChecked = value;
                RaisePropertyChanged();
            }
            get => _isAlphaBoxChecked;
        }

        private ObservableCollection<ColorResourceCache> _colorResources =
            new ObservableCollection<ColorResourceCache>();

        public ObservableCollection<ColorResourceCache> ColorResources
        {
            set
            {
                _colorResources = value;
                RaisePropertyChanged();
            }
            get => _colorResources;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ColorHexToRgbCommand { set; get; }
        public DelegateCommand AlphaValueChangedCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand CheckBoxCheckedCommand { set; get; }
        public DelegateCommand CheckBoxUncheckedCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        public ColorResourceViewModel()
        {
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            ColorViewBrush = new SolidColorBrush(color);

            ColorHexToRgbCommand = new DelegateCommand(ColorHexToRgb);
            AlphaValueChangedCommand = new DelegateCommand(AlphaValueChanged);
            CopyColorHexValueCommand = new DelegateCommand(CopyColorHexValue);
            CheckBoxCheckedCommand = new DelegateCommand(OnAlphaChecked);
            CheckBoxUncheckedCommand = new DelegateCommand(OnAlphaUnChecked);
            ColorItemClickedCommand = new DelegateCommand<ColorResourceCache>(ColorItemClicked);
        }

        private async Task LoadColorResourcesAsync()
        {
            try
            {
                using (var dataBase = new DataBaseConnection())
                {
                    var colorResCaches = await Task.Run(() => dataBase.Table<ColorResourceCache>().ToList());
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ColorResources = colorResCaches.ToObservableCollection();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ColorHexToRgb()
        {
            var colorHex = _userInputColorHexValue;
            if (string.IsNullOrEmpty(colorHex))
            {
                MessageBox.Show("输入不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 移除前缀 #
            if (colorHex.StartsWith("#"))
            {
                colorHex = colorHex.Substring(1);
            }

            if (!colorHex.IsHex())
            {
                MessageBox.Show("不是有效颜色值，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 检查长度是否为 6（RGB）或 8（RGBA）
            if (colorHex.Length != 6 && colorHex.Length != 8)
            {
                MessageBox.Show("颜色值长度不正确，应为6位或8位", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 手动转换为 System.Windows.Media.Color
            if (_isAlphaBoxChecked)
            {
                if (colorHex.Length != 8)
                {
                    MessageBox.Show("启用透明度时，颜色值必须为8位", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var alpha = byte.Parse(colorHex.Substring(0, 2), NumberStyles.HexNumber);
                RedColor = byte.Parse(colorHex.Substring(2, 2), NumberStyles.HexNumber);
                GreenColor = byte.Parse(colorHex.Substring(4, 2), NumberStyles.HexNumber);
                BlueColor = byte.Parse(colorHex.Substring(6, 2), NumberStyles.HexNumber);

                SliderValue = alpha;

                var mediaColor = Color.FromArgb(_sliderValue, _redColor, _greenColor, _blueColor);
                SetColorBrushAndHex(mediaColor, true);
            }
            else
            {
                if (colorHex.Length != 6)
                {
                    MessageBox.Show("禁用透明度时，颜色值必须为6位", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                RedColor = byte.Parse(colorHex.Substring(0, 2), NumberStyles.HexNumber);
                GreenColor = byte.Parse(colorHex.Substring(2, 2), NumberStyles.HexNumber);
                BlueColor = byte.Parse(colorHex.Substring(4, 2), NumberStyles.HexNumber);

                SliderValue = 255;

                var mediaColor = Color.FromRgb(_redColor, _greenColor, _blueColor);
                SetColorBrushAndHex(mediaColor, false);
            }
        }

        private void SetColorBrushAndHex(Color mediaColor, bool includeAlpha)
        {
            ColorViewBrush = new SolidColorBrush(mediaColor);
            if (includeAlpha)
            {
                ColorHexValue = mediaColor.ToString();
            }
            else
            {
                ColorHexValue = string.Concat("#", mediaColor.R.ToString("X2"), mediaColor.G.ToString("X2"),
                    mediaColor.B.ToString("X2"));
            }
        }

        private void AlphaValueChanged()
        {
            if (_isAlphaBoxChecked)
            {
                var color = Color.FromArgb(_sliderValue, _redColor, _greenColor, _blueColor);
                ColorViewBrush = new SolidColorBrush(color);
                ColorHexValue = color.ToString();
            }
            else
            {
                var color = Color.FromRgb(_redColor, _greenColor, _blueColor);
                ColorViewBrush = new SolidColorBrush(color);
                ColorHexValue = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            }
        }

        private void OnAlphaChecked()
        {
            var color = Color.FromArgb(_sliderValue, _redColor, _greenColor, _blueColor);
            ColorHexValue = color.ToString();
        }

        private void OnAlphaUnChecked()
        {
            var color = Color.FromRgb(_redColor, _greenColor, _blueColor);
            ColorHexValue = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void CopyColorHexValue()
        {
            if (string.IsNullOrEmpty(_colorHexValue))
            {
                MessageBox.Show("请先输入颜色值", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Clipboard.SetText(_colorHexValue);
            Growl.Success("颜色值已复制");
        }

        private void ColorItemClicked(ColorResourceCache cache)
        {
            Clipboard.SetText(cache.Hex);
            Growl.Success("颜色值已复制");
        }
    }
}
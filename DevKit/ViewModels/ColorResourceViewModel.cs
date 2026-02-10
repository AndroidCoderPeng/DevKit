using System;
using System.Collections.ObjectModel;
using System.Drawing;
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

        private string _redColor = "0";

        public string RedColor
        {
            set
            {
                _redColor = value;
                RaisePropertyChanged();
            }
            get => _redColor;
        }

        private string _greenColor = "0";

        public string GreenColor
        {
            set
            {
                _greenColor = value;
                RaisePropertyChanged();
            }
            get => _greenColor;
        }

        private string _blueColor = "0";

        public string BlueColor
        {
            set
            {
                _blueColor = value;
                RaisePropertyChanged();
            }
            get => _blueColor;
        }

        private string _alphaValue = "255";

        public string AlphaValue
        {
            set
            {
                _alphaValue = value;
                RaisePropertyChanged();
            }
            get => _alphaValue;
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

        private string _colorHexValue = "#FF000000";

        public string ColorHexValue
        {
            set
            {
                _colorHexValue = value;
                RaisePropertyChanged();
            }
            get => _colorHexValue;
        }

        private bool _isHexInputEnabled;

        public bool IsHexInputEnabled
        {
            set
            {
                _isHexInputEnabled = value;
                RaisePropertyChanged();
            }
            get => _isHexInputEnabled;
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

        public DelegateCommand<string> AlphaColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RedColorTextChangedCommand { set; get; }
        public DelegateCommand<string> GreenColorTextChangedCommand { set; get; }
        public DelegateCommand<string> BlueColorTextChangedCommand { set; get; }
        public DelegateCommand<string> ColorHexTextChangedCommand { set; get; }

        public DelegateCommand HexCheckBoxCheckedCommand { set; get; }
        public DelegateCommand HexCheckBoxUncheckedCommand { set; get; }

        public DelegateCommand AlphaCheckBoxCheckedCommand { set; get; }
        public DelegateCommand AlphaCheckBoxUncheckedCommand { set; get; }

        public DelegateCommand AlphaValueChangedCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private byte _alpha = 255;
        private byte _red;
        private byte _green;
        private byte _blue;

        public ColorResourceViewModel()
        {
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            ColorViewBrush = new SolidColorBrush(color);

            AlphaColorTextChangedCommand = new DelegateCommand<string>(AlphaColorTextChanged);
            RedColorTextChangedCommand = new DelegateCommand<string>(RedColorTextChanged);
            GreenColorTextChangedCommand = new DelegateCommand<string>(GreenColorTextChanged);
            BlueColorTextChangedCommand = new DelegateCommand<string>(BlueColorTextChanged);
            ColorHexTextChangedCommand = new DelegateCommand<string>(ColorHexTextChanged);

            HexCheckBoxCheckedCommand = new DelegateCommand(HexCheckBoxChecked);
            HexCheckBoxUncheckedCommand = new DelegateCommand(HexCheckBoxUnchecked);

            AlphaCheckBoxCheckedCommand = new DelegateCommand(AlphaCheckBoxChecked);
            AlphaCheckBoxUncheckedCommand = new DelegateCommand(AlphaCheckBoxUnchecked);

            AlphaValueChangedCommand = new DelegateCommand(AlphaValueChanged);
            CopyColorHexValueCommand = new DelegateCommand(CopyColorHexValue);
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

        private void AlphaColorTextChanged(string colorValue)
        {
            if (_isHexInputEnabled)
            {
                Console.WriteLine(@"HEX转RGB模式，不响应此函数");
                return;
            }

            if (string.IsNullOrWhiteSpace(colorValue) || !colorValue.IsNumber())
            {
                colorValue = "0";
            }

            if (!int.TryParse(colorValue, out var intValue)) intValue = 0;
            intValue = Math.Max(0, Math.Min(255, intValue));
            _alpha = (byte)intValue;
            ArgbToHex();
        }

        private void RedColorTextChanged(string colorValue)
        {
            if (_isHexInputEnabled)
            {
                Console.WriteLine(@"HEX转RGB模式，不响应此函数");
                return;
            }

            if (string.IsNullOrWhiteSpace(colorValue) || !colorValue.IsNumber())
            {
                colorValue = "0";
            }

            if (!int.TryParse(colorValue, out var intValue)) intValue = 0;
            intValue = Math.Max(0, Math.Min(255, intValue));
            _red = (byte)intValue;
            ArgbToHex();
        }

        private void GreenColorTextChanged(string colorValue)
        {
            if (_isHexInputEnabled)
            {
                Console.WriteLine(@"HEX转RGB模式，不响应此函数");
                return;
            }

            if (string.IsNullOrWhiteSpace(colorValue) || !colorValue.IsNumber())
            {
                colorValue = "0";
            }

            if (!int.TryParse(colorValue, out var intValue)) intValue = 0;
            intValue = Math.Max(0, Math.Min(255, intValue));
            _green = (byte)intValue;
            ArgbToHex();
        }

        private void BlueColorTextChanged(string colorValue)
        {
            if (_isHexInputEnabled)
            {
                Console.WriteLine(@"HEX转RGB模式，不响应此函数");
                return;
            }

            if (string.IsNullOrWhiteSpace(colorValue) || !colorValue.IsNumber())
            {
                colorValue = "0";
            }

            if (!int.TryParse(colorValue, out var intValue)) intValue = 0;
            intValue = Math.Max(0, Math.Min(255, intValue));
            _blue = (byte)intValue;
            ArgbToHex();
        }

        private void ArgbToHex()
        {
            var color = Color.FromArgb(_alpha, _red, _green, _blue);
            SliderValue = color.A;
            ColorHexValue = color.ToString();
            ColorViewBrush = new SolidColorBrush(color);
        }

        private void ColorHexTextChanged(string colorValue)
        {
            if (!_isHexInputEnabled)
            {
                Console.WriteLine(@"RGB转HEX模式，不响应此函数");
                return;
            }

            if (string.IsNullOrWhiteSpace(colorValue))
            {
                HexToArgb("#FF000000");
                return;
            }

            if (!colorValue.StartsWith("#"))
            {
                colorValue = colorValue.Insert(0, "#");
            }

            HexToArgb(colorValue);
        }

        private void HexToArgb(string colorHex)
        {
            if (!colorHex.IsColorHexString()) return;

            if (colorHex.Length != 4 && colorHex.Length != 7 && colorHex.Length != 9) return;

            // 四个字符（#000）或者七个字符（#000000）或者九个字符（#FF000000）
            var drawingColor = ColorTranslator.FromHtml(colorHex);
            AlphaValue = drawingColor.A.ToString();
            RedColor = drawingColor.R.ToString();
            GreenColor = drawingColor.G.ToString();
            BlueColor = drawingColor.B.ToString();
            var mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            ColorViewBrush = new SolidColorBrush(mediaColor);
        }

        private void HexCheckBoxChecked()
        {
            IsHexInputEnabled = false;
        }

        private void HexCheckBoxUnchecked()
        {
            IsHexInputEnabled = true;
        }

        private void AlphaCheckBoxChecked()
        {
            var color = Color.FromArgb(
                Convert.ToByte(_alphaValue),
                Convert.ToByte(_redColor), Convert.ToByte(_greenColor), Convert.ToByte(_blueColor)
            );
            // 添加透明度
            ColorHexValue = color.ToString();
        }

        private void AlphaCheckBoxUnchecked()
        {
            var color = Color.FromRgb(
                Convert.ToByte(_redColor), Convert.ToByte(_greenColor), Convert.ToByte(_blueColor)
            );
            // 去掉透明度
            ColorHexValue = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void AlphaValueChanged()
        {
            var color = Color.FromArgb(
                _sliderValue, Convert.ToByte(_redColor), Convert.ToByte(_greenColor), Convert.ToByte(_blueColor)
            );
            AlphaValue = _sliderValue.ToString();
            ColorViewBrush = new SolidColorBrush(color);
            ColorHexValue = color.ToString();
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
            var drawingColor = ColorTranslator.FromHtml(cache.Hex);
            var mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            RedColor = drawingColor.R.ToString();
            GreenColor = drawingColor.G.ToString();
            BlueColor = drawingColor.B.ToString();
            ColorViewBrush = new SolidColorBrush(mediaColor);
            Growl.Success("颜色值已复制");
        }
    }
}
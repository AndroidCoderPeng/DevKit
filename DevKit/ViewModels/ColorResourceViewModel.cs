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

        private bool _isHexBoxChecked = true;

        public bool IsHexBoxChecked
        {
            set
            {
                _isHexBoxChecked = value;
                RaisePropertyChanged();
            }
            get => _isHexBoxChecked;
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

        public DelegateCommand ColorConvertCommand { set; get; }
        public DelegateCommand AlphaValueChangedCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        public ColorResourceViewModel()
        {
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            ColorViewBrush = new SolidColorBrush(color);

            ColorConvertCommand = new DelegateCommand(ColorConvert);
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

        private void ColorConvert()
        {
            if (_isHexBoxChecked)
            {
                if (!_redColor.IsByte() || !_greenColor.IsByte() || !_blueColor.IsByte())
                {
                    MessageBox.Show("请填写正确的RGB值，各分量取值范围【0~255】", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Color mediaColor;
                if (_isAlphaBoxChecked)
                {
                    if (!_alphaValue.IsByte())
                    {
                        MessageBox.Show("请填写正确的透明度值，取值范围【0~255】", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SliderValue = Convert.ToByte(_alphaValue);
                    mediaColor = Color.FromArgb(
                        Convert.ToByte(_alphaValue), Convert.ToByte(_redColor), Convert.ToByte(_greenColor),
                        Convert.ToByte(_blueColor)
                    );
                    ColorHexValue = mediaColor.ToString(); // 默认会带上透明度
                }
                else
                {
                    mediaColor = Color.FromRgb(
                        Convert.ToByte(_redColor), Convert.ToByte(_greenColor), Convert.ToByte(_blueColor)
                    );
                    ColorHexValue = $"#{mediaColor.R:X2}{mediaColor.G:X2}{mediaColor.B:X2}";
                }

                ColorViewBrush = new SolidColorBrush(mediaColor);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_colorHexValue))
                {
                    MessageBox.Show("请填写正确的HEX值", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 判断是否是合理的Hex颜色值
                try
                {
                    var hex = _colorHexValue.Trim();
                    if (hex.StartsWith("#"))
                    {
                        hex = hex.Substring(1);
                    }

                    // 验证长度是否符合HEX颜色值规范(3位、6位或8位)
                    if (hex.Length != 3 && hex.Length != 6 && hex.Length != 8)
                    {
                        throw new FormatException();
                    }

                    // 验证每个字符是否都是有效的十六进制字符
                    var isValidHex = true;
                    foreach (var c in hex)
                    {
                        if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                        {
                            isValidHex = false;
                            break;
                        }
                    }

                    if (!isValidHex)
                    {
                        throw new FormatException();
                    }
                    
                    // 如果验证通过，继续进行HEX到RGB的转换
                    var drawingColor = ColorTranslator.FromHtml(_colorHexValue);
                    RedColor = drawingColor.R.ToString();
                    GreenColor = drawingColor.G.ToString();
                    BlueColor = drawingColor.B.ToString();
                    
                    if (hex.Length == 8)
                    {
                        AlphaValue = drawingColor.A.ToString();
                        SliderValue = drawingColor.A;
                        IsAlphaBoxChecked = true;
                    }
                    else
                    {
                        AlphaValue = "255";
                        SliderValue = 255;
                        IsAlphaBoxChecked = false;
                    }
                    
                    var mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                    ColorViewBrush = new SolidColorBrush(mediaColor);
                    ColorHexValue = _colorHexValue;
                }
                catch (Exception e)
                {
                    MessageBox.Show("请输入有效的HEX颜色值，例如:#FF0000、#00FF00等", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

            if (_isAlphaBoxChecked)
            {
                Clipboard.SetText(_colorHexValue);
            }
            else
            {
                var color = _colorViewBrush.Color;
                Clipboard.SetText($"#{color.R:X2}{color.G:X2}{color.B:X2}");
            }

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
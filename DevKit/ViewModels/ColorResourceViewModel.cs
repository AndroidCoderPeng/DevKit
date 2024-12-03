using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevKit.DataService;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class ColorResourceViewModel : BindableBase
    {
        #region DelegateCommand

        public DelegateCommand<NumericUpDown> ColorRedValueChangedCommand { set; get; }
        public DelegateCommand<NumericUpDown> ColorGreenValueChangedCommand { set; get; }
        public DelegateCommand<NumericUpDown> ColorBlueValueChangedCommand { set; get; }
        public DelegateCommand<Slider> AlphaValueChangedCommand { set; get; }
        public DelegateCommand<string> CopyColorHexValueCommand { set; get; }
        public DelegateCommand<string> ColorHexToRgbCommand { set; get; }

        #endregion

        #region VM

        private SolidColorBrush _colorBrush;

        public SolidColorBrush ColorBrush
        {
            set
            {
                _colorBrush = value;
                RaisePropertyChanged();
            }
            get => _colorBrush;
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

        private byte _red;

        public byte Red
        {
            set
            {
                _red = value;
                RaisePropertyChanged();
            }
            get => _red;
        }

        private byte _green;

        public byte Green
        {
            set
            {
                _green = value;
                RaisePropertyChanged();
            }
            get => _green;
        }

        private byte _blue;

        public byte Blue
        {
            set
            {
                _blue = value;
                RaisePropertyChanged();
            }
            get => _blue;
        }

        public List<string> ColorSchemes { get; }

        #endregion

        private readonly IAppDataService _dataService;
        private byte _alpha = 255;

        public ColorResourceViewModel(IAppDataService dataService)
        {
            _dataService = dataService;
            ColorSchemes = _dataService.GetColorSchemes();

            var color = Color.FromRgb(_red, _green, _blue);
            ColorBrush = new SolidColorBrush(color);

            ColorRedValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorRedValueChanged);
            ColorGreenValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorGreenValueChanged);
            ColorBlueValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorBlueValueChanged);
            AlphaValueChangedCommand = new DelegateCommand<Slider>(AlphaValueChanged);
            CopyColorHexValueCommand = new DelegateCommand<string>(CopyColorHexValue);
            ColorHexToRgbCommand = new DelegateCommand<string>(ColorHexToRgb);
        }

        private void ColorRedValueChanged(NumericUpDown numeric)
        {
            var value = numeric.Value;
            if (value > 255)
            {
                value = 255;
            }

            _red = (byte)value;
            GenerateColor();
        }

        private void ColorGreenValueChanged(NumericUpDown numeric)
        {
            var value = numeric.Value;
            if (value > 255)
            {
                value = 255;
            }

            _green = (byte)value;
            GenerateColor();
        }

        private void ColorBlueValueChanged(NumericUpDown numeric)
        {
            var value = numeric.Value;
            if (value > 255)
            {
                value = 255;
            }

            _blue = (byte)value;
            GenerateColor();
        }

        private void AlphaValueChanged(Slider slider)
        {
            _alpha = (byte)(slider.Value * byte.MaxValue);
            GenerateColor();
        }

        private void GenerateColor()
        {
            var color = Color.FromArgb(_alpha, _red, _green, _blue);
            ColorBrush = new SolidColorBrush(color);
            ColorHexValue = color.ToString();
        }

        private void CopyColorHexValue(string colorVale)
        {
            Clipboard.SetText(colorVale);
            Growl.Success("颜色值已复制");
        }

        private void ColorHexToRgb(string colorValue)
        {
            if (!colorValue.StartsWith("#"))
            {
                MessageBox.Show("不是有效颜色值，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!colorValue.Substring(1, 6).IsHex())
            {
                MessageBox.Show("不是有效颜色值，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //转换RGB时，透明度默认为最大值255
            _alpha = 255;

            //#b1d85c
            var color = ColorTranslator.FromHtml(colorValue);
            Red = color.R;
            Green = color.G;
            Blue = color.B;
        }
    }
}
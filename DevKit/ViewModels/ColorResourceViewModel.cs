using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;

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

        #endregion

        private byte _red;
        private byte _green;
        private byte _blue;
        private byte _alpha = 255;

        public ColorResourceViewModel()
        {
            var color = Color.FromRgb(_red, _green, _blue);
            ColorBrush = new SolidColorBrush(color);

            ColorRedValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorRedValueChanged);
            ColorGreenValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorGreenValueChanged);
            ColorBlueValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorBlueValueChanged);
            AlphaValueChangedCommand = new DelegateCommand<Slider>(AlphaValueChanged);
            CopyColorHexValueCommand = new DelegateCommand<string>(CopyColorHexValue);
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
    }
}
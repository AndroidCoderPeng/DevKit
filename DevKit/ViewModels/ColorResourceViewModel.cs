using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
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
        public DelegateCommand<ComboBox> ItemSelectedCommand { set; get; }
        public DelegateCommand<string> TraditionColorListBoxItemButtonClickCommand { set; get; }
        public DelegateCommand<string> DimColorListBoxItemButtonClickCommand { set; get; }

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

        private int _colorCount;

        public int ColorCount
        {
            set
            {
                _colorCount = value;
                RaisePropertyChanged();
            }
            get => _colorCount;
        }

        private string _isTraditionColorListBoxVisible = "Visible";

        public string IsTraditionColorListBoxVisible
        {
            set
            {
                _isTraditionColorListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isTraditionColorListBoxVisible;
        }

        private string _isDimColorListBoxVisible = "Collapsed";

        public string IsDimColorListBoxVisible
        {
            set
            {
                _isDimColorListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isDimColorListBoxVisible;
        }

        private string _isGradientColorListBoxVisible = "Collapsed";

        public string IsGradientColorListBoxVisible
        {
            set
            {
                _isGradientColorListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isGradientColorListBoxVisible;
        }

        private ObservableCollection<ColorResourceCache> _colorResources = new ObservableCollection<ColorResourceCache>();

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

        private readonly IAppDataService _dataService;
        private byte _alpha = 255;

        public ColorResourceViewModel(IAppDataService dataService)
        {
            _dataService = dataService;
            ColorSchemes = _dataService.GetColorSchemes();
            Task.Run(async () =>
            {
                var models = await _dataService.GetColorsByScheme("中国传统色系");
                ColorResources = models.ToObservableCollection();
            });
            ColorCount = ColorResources.Count;

            var color = Color.FromRgb(_red, _green, _blue);
            ColorBrush = new SolidColorBrush(color);

            ColorRedValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorRedValueChanged);
            ColorGreenValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorGreenValueChanged);
            ColorBlueValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorBlueValueChanged);
            AlphaValueChangedCommand = new DelegateCommand<Slider>(AlphaValueChanged);
            CopyColorHexValueCommand = new DelegateCommand<string>(CopyColorHexValue);
            ColorHexToRgbCommand = new DelegateCommand<string>(ColorHexToRgb);
            ItemSelectedCommand = new DelegateCommand<ComboBox>(ItemSelected);
            TraditionColorListBoxItemButtonClickCommand =
                new DelegateCommand<string>(TraditionColorListBoxItemButtonClick);
            DimColorListBoxItemButtonClickCommand = new DelegateCommand<string>(DimColorListBoxItemButtonClick);
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

        private void ItemSelected(ComboBox comboBox)
        {
            var text = comboBox.Text;
            switch (text)
            {
                case "中国传统色系":
                    IsTraditionColorListBoxVisible = "Visible";
                    IsDimColorListBoxVisible = "Collapsed";
                    IsGradientColorListBoxVisible = "Collapsed";

                    Task.Run(async () =>
                    {
                        var models = await _dataService.GetColorsByScheme("中国传统色系");
                        ColorResources = models.ToObservableCollection();
                    });
                    ColorCount = ColorResources.Count;
                    break;
                case "低调色系":
                    IsTraditionColorListBoxVisible = "Collapsed";
                    IsDimColorListBoxVisible = "Visible";
                    IsGradientColorListBoxVisible = "Collapsed";

                    Task.Run(async () =>
                    {
                        var models = await _dataService.GetColorsByScheme("低调色系");
                        ColorResources = models.ToObservableCollection();
                    });
                    ColorCount = ColorResources.Count;
                    break;
            }
        }

        private void TraditionColorListBoxItemButtonClick(string colorVale)
        {
            Clipboard.SetText(colorVale);
            Growl.Success("颜色值已复制");
        }

        private void DimColorListBoxItemButtonClick(string colorVale)
        {
            Clipboard.SetText(colorVale);
            Growl.Success("颜色值已复制");
        }
    }
}
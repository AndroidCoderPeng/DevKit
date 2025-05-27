using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevKit.Cache;
using DevKit.DataService;
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

        private string _colorRgbValue = "(0,0,0,1)";

        public string ColorRgbValue
        {
            set
            {
                _colorRgbValue = value;
                RaisePropertyChanged();
            }
            get => _colorRgbValue;
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

        public DelegateCommand<Slider> AlphaValueChangedCommand { set; get; }

        // public DelegateCommand<NumericUpDown> ColorRedValueChangedCommand { set; get; }
        // public DelegateCommand<NumericUpDown> ColorGreenValueChangedCommand { set; get; }
        // public DelegateCommand<NumericUpDown> ColorBlueValueChangedCommand { set; get; }
        public DelegateCommand<string> CopyColorHexValueCommand { set; get; }
        public DelegateCommand<string> ColorHexToRgbCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private byte _alpha = 255;

        public ColorResourceViewModel(IAppDataService dataService)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var colorResCaches = dataBase.Table<ColorResourceCache>()
                    .Where(x => x.Scheme.Equals("中国传统色系"))
                    .ToList();
                ColorResources = colorResCaches.ToObservableCollection();
            }

            var color = Color.FromRgb(_red, _green, _blue);
            ColorViewBrush = new SolidColorBrush(color);

            AlphaValueChangedCommand = new DelegateCommand<Slider>(AlphaValueChanged);
            // ColorRedValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorRedValueChanged);
            // ColorGreenValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorGreenValueChanged);
            // ColorBlueValueChangedCommand = new DelegateCommand<NumericUpDown>(ColorBlueValueChanged);
            CopyColorHexValueCommand = new DelegateCommand<string>(CopyColorHexValue);
            ColorHexToRgbCommand = new DelegateCommand<string>(ColorHexToRgb);
            ColorItemClickedCommand = new DelegateCommand<ColorResourceCache>(ColorItemClicked);
        }

        // private void ColorRedValueChanged(NumericUpDown numeric)
        // {
        //     var value = numeric.Value;
        //     if (value > 255)
        //     {
        //         value = 255;
        //     }
        //
        //     _red = (byte)value;
        //     GenerateColor();
        // }

        // private void ColorGreenValueChanged(NumericUpDown numeric)
        // {
        //     var value = numeric.Value;
        //     if (value > 255)
        //     {
        //         value = 255;
        //     }
        //
        //     _green = (byte)value;
        //     GenerateColor();
        // }

        // private void ColorBlueValueChanged(NumericUpDown numeric)
        // {
        //     var value = numeric.Value;
        //     if (value > 255)
        //     {
        //         value = 255;
        //     }
        //
        //     _blue = (byte)value;
        //     GenerateColor();
        // }

        private void AlphaValueChanged(Slider slider)
        {
            _alpha = (byte)slider.Value;
            GenerateColor();
        }

        private void GenerateColor()
        {
            var color = Color.FromArgb(_alpha, _red, _green, _blue);
            ColorViewBrush = new SolidColorBrush(color);
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

        private void ColorItemClicked(ColorResourceCache cache)
        {
            Clipboard.SetText(cache.Hex);
            Growl.Success("颜色值已复制");
        }
    }
}
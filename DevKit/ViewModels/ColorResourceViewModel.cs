using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public DelegateCommand<Slider> AlphaValueChangedCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand CheckBoxCheckedCommand { set; get; }
        public DelegateCommand CheckBoxUncheckedCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private byte _alpha = 255;

        public ColorResourceViewModel()
        {
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            ColorViewBrush = new SolidColorBrush(color);

            ColorHexToRgbCommand = new DelegateCommand(ColorHexToRgb);
            AlphaValueChangedCommand = new DelegateCommand<Slider>(AlphaValueChanged);
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
            if (_userInputColorHexValue.StartsWith("#") && _userInputColorHexValue.Length == 7)
            {
                _userInputColorHexValue = _userInputColorHexValue.Substring(1);
            }

            if (!_userInputColorHexValue.IsHex())
            {
                MessageBox.Show("不是有效颜色值，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //#b1d85c
            var drawingColor = ColorTranslator.FromHtml($"#{_userInputColorHexValue}");
            RedColor = drawingColor.R;
            GreenColor = drawingColor.G;
            BlueColor = drawingColor.B;

            // 手动转换为 System.Windows.Media.Color
            if (_isAlphaBoxChecked)
            {
                var mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                ColorViewBrush = new SolidColorBrush(mediaColor);
                ColorHexValue = mediaColor.ToString();
            }
            else
            {
                var mediaColor = Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                ColorViewBrush = new SolidColorBrush(mediaColor);
                ColorHexValue = "#" + mediaColor.R.ToString("X2") + mediaColor.G.ToString("X2") +
                                mediaColor.B.ToString("X2");
            }
        }

        private void AlphaValueChanged(Slider slider)
        {
            if (_isAlphaBoxChecked)
            {
                _alpha = (byte)slider.Value;
                var color = Color.FromArgb(_alpha, _redColor, _greenColor, _blueColor);
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
            var color = Color.FromArgb(_alpha, _redColor, _greenColor, _blueColor);
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
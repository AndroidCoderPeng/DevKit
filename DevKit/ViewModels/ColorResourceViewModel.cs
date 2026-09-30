using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.RecentlyColor, new RecentlyColorConfig
            {
                // 只保存最近的9个颜色
                Colors = _recentlyColors.Take(9).ToList()
            });
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private bool _isToHexSelected = true;

        public bool IsToHexSelected
        {
            get => _isToHexSelected;
            set
            {
                _isToHexSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAlphaBoxChecked = true;

        public bool IsAlphaBoxChecked
        {
            get => _isAlphaBoxChecked;
            set
            {
                _isAlphaBoxChecked = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush _colorViewBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x2E, 0x7C, 0xF6));

        public SolidColorBrush ColorViewBrush
        {
            get => _colorViewBrush;
            set
            {
                _colorViewBrush = value;
                RaisePropertyChanged();
            }
        }

        private int _alphaValue = 255;

        public int AlphaValue
        {
            get => _alphaValue;
            set
            {
                _alphaValue = value;
                RaisePropertyChanged();
            }
        }

        private string _colorHexValue = string.Empty;

        public string ColorHexValue
        {
            get => _colorHexValue;
            set
            {
                _colorHexValue = value;
                RaisePropertyChanged();
            }
        }

        private double _redColor;

        public double RedColor
        {
            get => _redColor;
            set
            {
                _redColor = value;
                RaisePropertyChanged();
            }
        }

        private double _greenColor;

        public double GreenColor
        {
            get => _greenColor;
            set
            {
                _greenColor = value;
                RaisePropertyChanged();
            }
        }

        private double _blueColor;

        public double BlueColor
        {
            get => _blueColor;
            set
            {
                _blueColor = value;
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

        private string _colorMode = string.Empty;

        public string ColorMode
        {
            get => _colorMode;
            set
            {
                _colorMode = value;
                RaisePropertyChanged();
            }
        }
        
        private string _currentColorHex = string.Empty;

        public string CurrentColorHex
        {
            get => _currentColorHex;
            set
            {
                _currentColorHex = value;
                RaisePropertyChanged();
            }
        }
        
        #endregion

        #region DelegateCommand

        public DelegateCommand RandomColorCommand { set; get; }
        public DelegateCommand ResetCommand { set; get; }
        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<string> ColorHexTextChangedCommand { set; get; }
        public DelegateCommand<string> AlphaColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RedColorTextChangedCommand { set; get; }
        public DelegateCommand<string> GreenColorTextChangedCommand { set; get; }
        public DelegateCommand<string> BlueColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RecentlyColorSelectedCommand { set; get; }
        //////////////////////////////////////
        // public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private byte _alpha = 255;
        private byte _red;
        private byte _green;
        private byte _blue;

        public ColorResourceViewModel()
        {
            var config = SettingsStore.Load<RecentlyColorConfig>(ConfigSections.FileName, ConfigSections.RecentlyColor);
            RecentlyColors = new ObservableCollection<string>(config.Colors);
            
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            // ColorViewBrush = new SolidColorBrush(color);
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
                        // ColorResources = colorResCaches.ToObservableCollection();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class GenerateIconViewModel : BindableBase
    {
        #region VM

        public List<string> PlatformTypes { get; }

        private string _isWindowsIconListBoxVisible = "Visible";

        public string IsWindowsIconListBoxVisible
        {
            set
            {
                _isWindowsIconListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isWindowsIconListBoxVisible;
        }

        private string _isAndroidDrawableListBoxVisible = "Collapsed";

        public string IsAndroidDrawableListBoxVisible
        {
            set
            {
                _isAndroidDrawableListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isAndroidDrawableListBoxVisible;
        }

        private string _isIPhoneImageListBoxVisible = "Collapsed";

        public string IsIPhoneImageListBoxVisible
        {
            set
            {
                _isIPhoneImageListBoxVisible = value;
                RaisePropertyChanged();
            }
            get => _isIPhoneImageListBoxVisible;
        }

        private bool _isIcoRadioButtonEnabled = true;

        public bool IsIcoRadioButtonEnabled
        {
            set
            {
                _isIcoRadioButtonEnabled = value;
                RaisePropertyChanged();
            }
            get => _isIcoRadioButtonEnabled;
        }

        private bool _isIcoRadioButtonChecked = true;

        public bool IsIcoRadioButtonChecked
        {
            set
            {
                _isIcoRadioButtonChecked = value;
                RaisePropertyChanged();
            }
            get => _isIcoRadioButtonChecked;
        }

        private bool _isPngRadioButtonChecked;

        public bool IsPngRadioButtonChecked
        {
            set
            {
                _isPngRadioButtonChecked = value;
                RaisePropertyChanged();
            }
            get => _isPngRadioButtonChecked;
        }

        private ObservableCollection<PlatformImageTypeModel> _imageTypeCollection =
            new ObservableCollection<PlatformImageTypeModel>();

        public ObservableCollection<PlatformImageTypeModel> ImageTypeCollection
        {
            set
            {
                _imageTypeCollection = value;
                RaisePropertyChanged();
            }
            get => _imageTypeCollection;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<ComboBox> ItemSelectedCommand { set; get; }

        #endregion

        public GenerateIconViewModel(IAppDataService dataService)
        {
            PlatformTypes = dataService.GetPlatformTypes();

            ItemSelectedCommand = new DelegateCommand<ComboBox>(ItemSelected);
        }

        private void ItemSelected(ComboBox comboBox)
        {
            var text = comboBox.Text;
            switch (text)
            {
                case "Windows":
                    IsWindowsIconListBoxVisible = "Visible";
                    IsAndroidDrawableListBoxVisible = "Collapsed";
                    IsIPhoneImageListBoxVisible = "Collapsed";

                    IsIcoRadioButtonEnabled = true;
                    IsIcoRadioButtonChecked = true;
                    break;
                case "Android":
                    IsWindowsIconListBoxVisible = "Collapsed";
                    IsAndroidDrawableListBoxVisible = "Visible";
                    IsIPhoneImageListBoxVisible = "Collapsed";

                    IsIcoRadioButtonEnabled = false;
                    IsPngRadioButtonChecked = true;
                    break;
                default:
                    IsWindowsIconListBoxVisible = "Collapsed";
                    IsAndroidDrawableListBoxVisible = "Collapsed";
                    IsIPhoneImageListBoxVisible = "Visible";

                    IsIcoRadioButtonEnabled = false;
                    IsPngRadioButtonChecked = true;
                    break;
            }
        }
    }
}
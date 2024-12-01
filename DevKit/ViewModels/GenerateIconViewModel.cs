using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
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

        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }
        public DelegateCommand<ComboBox> ItemSelectedCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private Uri _uri;

        public GenerateIconViewModel(IAppDataService dataService)
        {
            _dataService = dataService;
            PlatformTypes = _dataService.GetPlatformTypes();

            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
            ItemSelectedCommand = new DelegateCommand<ComboBox>(ItemSelected);
        }

        private void ImageSelected(Uri uri)
        {
            _uri = uri;
            ImageTypeCollection = _dataService.GetImageTypesByPlatform("Windows", _uri).ToObservableCollection();
            IsIcoRadioButtonChecked = true;
        }

        private void ImageUnselected()
        {
            ImageTypeCollection.Clear();
            _uri = null;
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

                    ImageTypeCollection = _dataService.GetImageTypesByPlatform("Windows", _uri)
                        .ToObservableCollection();
                    break;
                case "Android":
                    IsWindowsIconListBoxVisible = "Collapsed";
                    IsAndroidDrawableListBoxVisible = "Visible";
                    IsIPhoneImageListBoxVisible = "Collapsed";

                    IsIcoRadioButtonEnabled = false;
                    IsPngRadioButtonChecked = true;

                    ImageTypeCollection = _dataService.GetImageTypesByPlatform("Android", _uri)
                        .ToObservableCollection();
                    break;
            }
        }
    }
}
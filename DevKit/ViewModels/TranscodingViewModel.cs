using System;
using System.Drawing;
using System.IO;
using DevKit.DataService;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class TranscodingViewModel : BindableBase
    {
        #region VM

        private string _hexValue = "00";

        public string HexValue
        {
            set
            {
                _hexValue = value;
                RaisePropertyChanged();
            }
            get => _hexValue;
        }

        private string _decimalValue = "0";

        public string DecimalValue
        {
            set
            {
                _decimalValue = value;
                RaisePropertyChanged();
            }
            get => _decimalValue;
        }

        private string _stringValue = "NUL 空";

        public string StringValue
        {
            set
            {
                _stringValue = value;
                RaisePropertyChanged();
            }
            get => _stringValue;
        }

        private string _uniCodeValue = "\\u00";

        public string UniCodeValue
        {
            set
            {
                _uniCodeValue = value;
                RaisePropertyChanged();
            }
            get => _uniCodeValue;
        }

        private string _base64Result = string.Empty;

        public string Base64Result
        {
            set
            {
                _base64Result = value;
                RaisePropertyChanged();
            }
            get => _base64Result;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> SearchStartedCommand { set; get; }
        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;

        public TranscodingViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            SearchStartedCommand = new DelegateCommand<string>(SearchStarted);
            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
        }

        private void SearchStarted(string hexValue)
        {
            var asciiCode = _dataService.QueryAsciiCodeByHex(hexValue);
            DecimalValue = asciiCode.DecimalValue.ToString();
            StringValue = asciiCode.StringValue;
            UniCodeValue = asciiCode.UniCodeValue;
        }

        private void ImageSelected(Uri uri)
        {
            using (var ms = new MemoryStream())
            {
                using (var image = Image.FromFile(uri.LocalPath))
                {
                    // 将图片保存到MemoryStream中
                    image.Save(ms, image.RawFormat);
                }

                var imageBytes = ms.ToArray();
                Base64Result = Convert.ToBase64String(imageBytes);
            }
        }

        private void ImageUnselected()
        {
            Base64Result = string.Empty;
        }
    }
}
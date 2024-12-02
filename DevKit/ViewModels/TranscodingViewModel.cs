using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using DevKit.DataService;
using DevKit.Utils;
using HandyControl.Controls;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class TranscodingViewModel : BindableBase
    {
        #region VM

        private string _asciiCodeValue;

        public string AsciiCodeValue
        {
            set
            {
                _asciiCodeValue = value;
                RaisePropertyChanged();
            }
            get => _asciiCodeValue;
        }

        private string _hexCodeValue;

        public string HexCodeValue
        {
            set
            {
                _hexCodeValue = value;
                RaisePropertyChanged();
            }
            get => _hexCodeValue;
        }

        private string _unsignedByteArray;

        public string UnsignedByteArray
        {
            set
            {
                _unsignedByteArray = value;
                RaisePropertyChanged();
            }
            get => _unsignedByteArray;
        }

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

        public DelegateCommand<string> ByteArrayToHexCommand { set; get; }
        public DelegateCommand<string> SignedToUnsignedCommand { set; get; }
        public DelegateCommand<string> ByteArrayToAsciiCommand { set; get; }
        public DelegateCommand<TextBox> TextChangedCommand { set; get; }
        public DelegateCommand<string> SearchStartedCommand { set; get; }
        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;

        public TranscodingViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            ByteArrayToHexCommand = new DelegateCommand<string>(ByteArrayToHex);
            SignedToUnsignedCommand = new DelegateCommand<string>(SignedToUnsigned);
            ByteArrayToAsciiCommand = new DelegateCommand<string>(ByteArrayToAscii);
            TextChangedCommand = new DelegateCommand<TextBox>(TextChanged);
            SearchStartedCommand = new DelegateCommand<string>(SearchStarted);
            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
        }

        private void ByteArrayToHex(string byteArray)
        {
            if (byteArray.StartsWith("[") && byteArray.EndsWith("]"))
            {
                var dataContent = byteArray
                    .Substring(1, byteArray.Length - 2)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder();
                foreach (var data in dataContent)
                {
                    var hex = (int.Parse(data) & 0xFF).ToString("X");
                    builder.Append(hex).Append(" ");
                }

                HexCodeValue = builder.ToString().TrimEnd();
            }
            else
            {
                MessageBox.Show("不是有效的数据，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SignedToUnsigned(string byteArray)
        {
            if (byteArray.StartsWith("[") && byteArray.EndsWith("]"))
            {
                var dataContent = byteArray
                    .Substring(1, byteArray.Length - 2)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var list = dataContent.Select(data => int.Parse(data) & 0xFF).ToList();
                UnsignedByteArray = JsonConvert.SerializeObject(list);
            }
            else
            {
                MessageBox.Show("不是有效的数据，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ByteArrayToAscii(string byteArray)
        {
            if (byteArray.StartsWith("[") && byteArray.EndsWith("]"))
            {
                var dataContent = byteArray
                    .Substring(1, byteArray.Length - 2)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var bytes = new byte[dataContent.Length];
                for (var i = 0; i < dataContent.Length; i++)
                {
                    var data = int.Parse(dataContent[i]) & 0xFF;
                    if (data > 127)
                    {
                        MessageBox.Show("不是有效的数据，无法转换为ASCII", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    bytes[i] = Convert.ToByte(dataContent[i]);
                }

                AsciiCodeValue = Encoding.ASCII.GetString(bytes).Replace("\n", "").Replace("\r", "");
            }
            else
            {
                MessageBox.Show("不是有效的数据，无法转换", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextChanged(TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                HexCodeValue = string.Empty;
                AsciiCodeValue = string.Empty;
            }
        }

        private void SearchStarted(string hexValue)
        {
            if (!hexValue.IsHex())
            {
                MessageBox.Show("不是有效的16进制", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (int.Parse(hexValue, NumberStyles.HexNumber) > 127)
            {
                MessageBox.Show("超出ASCII码范围", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
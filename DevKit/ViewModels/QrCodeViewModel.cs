using System;
using System.Windows;
using System.Windows.Media.Imaging;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using ZXing;
using ZXing.Common;

namespace DevKit.ViewModels
{
    public class QrCodeViewModel : BindableBase
    {
        #region DelegateCommand

        public DelegateCommand UploadLogoCommand { set; get; }
        public DelegateCommand<string> GenerateQrCodeCommand { set; get; }
        public DelegateCommand SaveQrCodeCommand { set; get; }
        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }

        #endregion

        #region VM

        private BitmapImage _qrCodeBitmapImage;

        public BitmapImage QrCodeBitmapImage
        {
            set
            {
                _qrCodeBitmapImage = value;
                RaisePropertyChanged();
            }
            get => _qrCodeBitmapImage;
        }

        private string _qrCodeContent;

        public string QrCodeContent
        {
            set
            {
                _qrCodeContent = value;
                RaisePropertyChanged();
            }
            get => _qrCodeContent;
        }

        #endregion

        public QrCodeViewModel()
        {
            UploadLogoCommand = new DelegateCommand(UploadLogo);
            GenerateQrCodeCommand = new DelegateCommand<string>(GenerateQrCode);
            SaveQrCodeCommand = new DelegateCommand(SaveQrCode);
            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
        }

        private void UploadLogo()
        {
        }

        private void GenerateQrCode(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("请输入需要编码的内容", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (content.Length > 512)
            {
                MessageBox.Show("内容过长，最大支持512个字符", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = 1024,
                    Height = 1024
                }
            };
            var qrCodeBitmap = writer.Write(content);
            //需要重新规定尺寸
            QrCodeBitmapImage = qrCodeBitmap.ToBitmapImage();
        }

        private void SaveQrCode()
        {
            // qrCodeImage.Save("qrcode.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ImageSelected(Uri uri)
        {
        }

        private void ImageUnselected()
        {
        }
    }
}
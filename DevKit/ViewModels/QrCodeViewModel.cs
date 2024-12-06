using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using QRCoder;

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

        private bool _isOptionButtonEnabled;

        public bool IsOptionButtonEnabled
        {
            set
            {
                _isOptionButtonEnabled = value;
                RaisePropertyChanged();
            }
            get => _isOptionButtonEnabled;
        }

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

        private readonly QRCodeGenerator _qrGenerator = new QRCodeGenerator();

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

            // 生成QRCodeData对象，设置纠错级别为中等
            var qrCodeData = _qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M, true);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeBitmap = qrCode.GetGraphic(15, Color.Black, Color.White, false);
            QrCodeBitmapImage = qrCodeBitmap.ToBitmapImage();
            IsOptionButtonEnabled = true;
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
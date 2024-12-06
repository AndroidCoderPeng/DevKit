using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using QRCoder;
using ZXing;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class QrCodeViewModel : BindableBase
    {
        #region DelegateCommand

        public DelegateCommand<string> UploadLogoCommand { set; get; }
        public DelegateCommand<string> GenerateQrCodeCommand { set; get; }
        public DelegateCommand SaveQrCodeCommand { set; get; }
        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }

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
            UploadLogoCommand = new DelegateCommand<string>(UploadLogo);
            GenerateQrCodeCommand = new DelegateCommand<string>(GenerateQrCode);
            SaveQrCodeCommand = new DelegateCommand(SaveQrCode);
            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
        }

        private void UploadLogo(string content)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = @"请选择Logo文件",
                Filter = @"Logo(*.png;*.jpg)|*.png;*.jpg"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var qrCodeData = _qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M, true);
                var qrCode = new QRCode(qrCodeData);
                var icon = new Bitmap(dialog.FileName);
                var qrCodeBitmap = qrCode.GetGraphic(15, Color.Black, Color.White, icon, 20, 5, false);
                QrCodeBitmapImage = qrCodeBitmap.ToBitmapImage();
            }
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
            var dialog = new SaveFileDialog
            {
                Filter = @"二维码图片(*.png)|*.png",
                DefaultExt = ".png",
                FileName = "QrCode"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var stream = _qrCodeBitmapImage.StreamSource;
                var image = Image.FromStream(stream);
                image.Save(dialog.FileName, ImageFormat.Png);
                Growl.Success("二维码保存成功");
            }
        }

        private void ImageSelected(Uri uri)
        {
            using (var bitmap = new Bitmap(uri.LocalPath))
            {
                var codeReader = new BarcodeReader();
                var result = codeReader.Decode(bitmap);
                if (result != null)
                {
                    QrCodeContent = result.Text;
                }
                else
                {
                    MessageBox.Show("解析二维码失败，请确认二维码是否正确的", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
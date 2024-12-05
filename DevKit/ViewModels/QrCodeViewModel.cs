using System;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;

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
        
        private void UploadLogo(){}
        
        private void GenerateQrCode(string content){}

        private void SaveQrCode()
        {
            
        }
        
        private void ImageSelected(Uri uri)
        {
            
        }

        private void ImageUnselected()
        {
            
        }
    }
}
using System;
using System.Drawing;
using System.IO;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class TranscodingViewModel : BindableBase
    {
        #region VM

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

        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }

        #endregion

        public TranscodingViewModel()
        {
            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
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

                // 将MemoryStream转换为byte数组
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
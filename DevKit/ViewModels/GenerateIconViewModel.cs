using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using ComboBox = System.Windows.Controls.ComboBox;
using Size = System.Drawing.Size;

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
        public DelegateCommand OutputIconCommand { set; get; }

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
            OutputIconCommand = new DelegateCommand(OutputIcon);
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

                    IsIcoRadioButtonEnabled = true;
                    IsIcoRadioButtonChecked = true;

                    ImageTypeCollection = _dataService.GetImageTypesByPlatform("Windows", _uri)
                        .ToObservableCollection();
                    break;
                case "Android":
                    IsWindowsIconListBoxVisible = "Collapsed";
                    IsAndroidDrawableListBoxVisible = "Visible";

                    IsIcoRadioButtonEnabled = false;
                    IsPngRadioButtonChecked = true;

                    ImageTypeCollection = _dataService.GetImageTypesByPlatform("Android", _uri)
                        .ToObservableCollection();
                    break;
            }
        }

        /// <summary>
        /// 根据不同平台到处不同尺寸不同类型的Icon
        /// </summary>
        private void OutputIcon()
        {
            //获取源文件名
            var file = new FileInfo(_uri.LocalPath);
            var fileName = Path.GetFileNameWithoutExtension(file.FullName);
            var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var rootPath = folderDialog.SelectedPath;
                if (_isWindowsIconListBoxVisible == "Visible")
                {
                    if (_isIcoRadioButtonChecked)
                    {
                        // //生成一系列不同尺寸的ico格式图片
                        foreach (var type in _imageTypeCollection)
                        {
                            var size = new Size(type.Width, type.Height);
                            var destination = $@"{rootPath}\launcher_{type.Width}.ico";
                            using (var originalImage = Image.FromFile(_uri.LocalPath))
                            {
                                using (var bitmap = new Bitmap(originalImage, size))
                                {
                                    bitmap.Save(destination, ImageFormat.Icon);
                                }
                            }
                        }

                        Growl.Success("图标生成成功");
                    }
                    else if (_isPngRadioButtonChecked)
                    {
                        //生成一系列不同尺寸的png格式图片
                        foreach (var type in _imageTypeCollection)
                        {
                            var size = new Size(type.Width, type.Height);
                            var destination = $@"{rootPath}\{fileName}_{type.Width}.png";
                            using (var originalImage = Image.FromFile(_uri.LocalPath))
                            {
                                using (var bitmap = new Bitmap(originalImage, size))
                                {
                                    bitmap.Save(destination, ImageFormat.Png);
                                }
                            }
                        }

                        Growl.Success("图标生成成功");
                    }
                    else
                    {
                        //生成一系列不同尺寸的jpg格式图片
                        foreach (var type in _imageTypeCollection)
                        {
                            var size = new Size(type.Width, type.Height);
                            var destination = $@"{rootPath}\{fileName}_{type.Width}.jpg";
                            using (var originalImage = Image.FromFile(_uri.LocalPath))
                            {
                                using (var bitmap = new Bitmap(originalImage, size))
                                {
                                    bitmap.Save(destination, ImageFormat.Jpeg);
                                }
                            }
                        }

                        Growl.Success("图标生成成功");
                    }
                }

                if (_isAndroidDrawableListBoxVisible == "Visible")
                {
                    GenerateAndroidIcon(
                        rootPath, fileName, _isPngRadioButtonChecked ? ImageFormat.Png : ImageFormat.Jpeg
                    );
                }
            }
        }

        /// <summary>
        /// 生成Android平台图标
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="fileName"></param>
        /// <param name="imageFormat"></param>
        private void GenerateAndroidIcon(string rootPath, string fileName, ImageFormat imageFormat)
        {
            foreach (var type in _imageTypeCollection)
            {
                var size = new Size(type.Width, type.Height);
                var folderPath = $@"{rootPath}\{type.AndroidSizeTag}";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var destination = Equals(imageFormat, ImageFormat.Png)
                    ? $@"{folderPath}\{fileName}.png"
                    : $@"{folderPath}\{fileName}.jpg";
                using (var originalImage = Image.FromFile(_uri.LocalPath))
                {
                    using (var bitmap = new Bitmap(originalImage, size))
                    {
                        bitmap.Save(destination, imageFormat);
                    }
                }
            }

            Growl.Success("图标生成成功");
        }
    }
}
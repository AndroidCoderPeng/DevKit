﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using ComboBox = System.Windows.Controls.ComboBox;

namespace DevKit.ViewModels
{
    public class GenerateIconViewModel : BindableBase
    {
        #region VM

        private BitmapImage _roundCornerImage;

        public BitmapImage RoundCornerImage
        {
            set
            {
                _roundCornerImage = value;
                RaisePropertyChanged();
            }
            get => _roundCornerImage;
        }

        private int _cornerRadius = 25;

        public int CornerRadius
        {
            set
            {
                _cornerRadius = value;
                RaisePropertyChanged();
            }
            get => _cornerRadius;
        }

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

        private List<PlatformImageTypeModel> _imageTypeItems = new List<PlatformImageTypeModel>();

        public List<PlatformImageTypeModel> ImageTypeItems
        {
            set
            {
                _imageTypeItems = value;
                RaisePropertyChanged();
            }
            get => _imageTypeItems;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<Uri> ImageSelectedCommand { set; get; }
        public DelegateCommand ImageUnselectedCommand { set; get; }
        public DelegateCommand CreateRoundCornerIconCommand { set; get; }
        public DelegateCommand SaveRoundCornerIconCommand { set; get; }
        public DelegateCommand<ComboBox> ItemSelectedCommand { set; get; }
        public DelegateCommand OutputIconCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private Uri _uri;
        private Bitmap _roundImage;

        public GenerateIconViewModel(IAppDataService dataService)
        {
            _dataService = dataService;
            PlatformTypes = _dataService.GetPlatformTypes();

            ImageSelectedCommand = new DelegateCommand<Uri>(ImageSelected);
            ImageUnselectedCommand = new DelegateCommand(ImageUnselected);
            CreateRoundCornerIconCommand = new DelegateCommand(CreateRoundCornerIcon);
            SaveRoundCornerIconCommand = new DelegateCommand(SaveRoundCornerIcon);
            ItemSelectedCommand = new DelegateCommand<ComboBox>(ItemSelected);
            OutputIconCommand = new DelegateCommand(OutputIcon);
        }

        private void ImageSelected(Uri uri)
        {
            _uri = uri;
            RoundCornerImage = new BitmapImage(_uri);
            ImageTypeItems = _dataService.GetImageTypesByPlatform("Windows", _uri);
            IsIcoRadioButtonChecked = true;
        }

        private void ImageUnselected()
        {
            ImageTypeItems.Clear();
            _uri = null;
            RoundCornerImage = null;
        }

        private void CreateRoundCornerIcon()
        {
            var originalImage = Image.FromFile(_uri.LocalPath);
            _roundImage = new Bitmap(originalImage.Width, originalImage.Height);
            using (var g = Graphics.FromImage(_roundImage))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 抗锯齿
                g.InterpolationMode = InterpolationMode.HighQualityBicubic; // 高质量双三次插值
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.FillRectangle(Brushes.Transparent, new Rectangle(0, 0, originalImage.Width, originalImage.Height));
                using (var path = CreateRoundedRectPath(originalImage.Width, originalImage.Height, _cornerRadius))
                {
                    g.SetClip(path);
                    g.DrawImage(originalImage, new Point(0, 0));
                }
            }

            originalImage.Dispose();
            RoundCornerImage = _roundImage.ToBitmapImage();
        }

        private void SaveRoundCornerIcon()
        {
            var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var file = new FileInfo(_uri.LocalPath);
                var fileName = Path.GetFileNameWithoutExtension(file.FullName);
                _roundImage.Save($@"{folderDialog.SelectedPath}\{fileName}.png", ImageFormat.Png);
                _roundImage.Dispose();
                Growl.Success("图标生成成功");
            }
        }

        private GraphicsPath CreateRoundedRectPath(int width, int height, int cornerRadius)
        {
            var path = new GraphicsPath();
            // 左上角
            path.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
            // 右上角
            path.AddArc(width - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
            // 右下角
            path.AddArc(width - cornerRadius, height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            // 左下角
            path.AddArc(0, height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseFigure();
            return path;
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

                    ImageTypeItems = _dataService.GetImageTypesByPlatform("Windows", _uri);
                    break;
                case "Android":
                    IsWindowsIconListBoxVisible = "Collapsed";
                    IsAndroidDrawableListBoxVisible = "Visible";

                    IsIcoRadioButtonEnabled = false;
                    IsPngRadioButtonChecked = true;

                    ImageTypeItems = _dataService.GetImageTypesByPlatform("Android", _uri);
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
                        foreach (var type in _imageTypeItems)
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
                        GenerateWindowsIcon(rootPath, fileName, ImageFormat.Png);
                    }
                    else
                    {
                        GenerateWindowsIcon(rootPath, fileName, ImageFormat.Jpeg);
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
        /// 生成Windows平台图标
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="fileName"></param>
        /// <param name="imageFormat"></param>
        private void GenerateWindowsIcon(string rootPath, string fileName, ImageFormat imageFormat)
        {
            foreach (var type in _imageTypeItems)
            {
                var size = new Size(type.Width, type.Height);
                var destination = Equals(imageFormat, ImageFormat.Png)
                    ? $@"{rootPath}\{fileName}_{type.Width}.png"
                    : $@"{rootPath}\{fileName}_{type.Width}.jpg";
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

        /// <summary>
        /// 生成Android平台图标
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="fileName"></param>
        /// <param name="imageFormat"></param>
        private void GenerateAndroidIcon(string rootPath, string fileName, ImageFormat imageFormat)
        {
            foreach (var type in _imageTypeItems)
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
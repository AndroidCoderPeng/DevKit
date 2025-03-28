using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class ScreenShotListDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "截屏导出";

        public event Action<IDialogResult> RequestClose;

        private ObservableCollection<ScreenshotModel> _screenshots = new ObservableCollection<ScreenshotModel>();

        public ObservableCollection<ScreenshotModel> Screenshots
        {
            set
            {
                _screenshots = value;
                RaisePropertyChanged();
            }
            get => _screenshots;
        }

        #region DelegateCommand

        public DelegateCommand SortScreenshotsCommand { set; get; }
        public DelegateCommand<ScreenshotModel> OutputScreenshotCommand { set; get; }

        #endregion

        private bool _isAscending;

        public ScreenShotListDialogViewModel()
        {
            SortScreenshotsCommand = new DelegateCommand(SortScreenshots);
            OutputScreenshotCommand = new DelegateCommand<ScreenshotModel>(OutputScreenshot);
        }

        private void SortScreenshots()
        {
            var list = _screenshots.ToList();
            if (_isAscending)
            {
                list = list.OrderBy(s => s.Time).ToList();
                _isAscending = false;
            }
            else
            {
                list = list.OrderByDescending(s => s.Time).ToList();
                _isAscending = true;
            }

            Screenshots = list.ToObservableCollection();
        }

        private void OutputScreenshot(ScreenshotModel screenshot)
        {
            if (screenshot == null)
            {
                MessageBox.Show("请选择要导出的图片");
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "selectedImage", screenshot.FilePath }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var list = parameters.GetValue<List<string>>("screenshots");
            if (_screenshots.Any())
            {
                Screenshots.Clear();
            }

            //整理截屏集合
            foreach (var path in list)
            {
                // 获取文件名，去掉扩展名
                var fileName = Path.GetFileNameWithoutExtension(path);
                var result = DateTime.TryParseExact(
                    fileName, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime
                );
                if (result)
                {
                    var model = new ScreenshotModel
                    {
                        FilePath = path,
                        Time = dateTime
                    };
                    Screenshots.Add(model);
                }
                else
                {
                    Console.WriteLine($@"File: {path}, Time: 无法解析时间");
                }
            }
        }
    }
}
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
        public DelegateCommand<ScreenshotModel> DeleteScreenshotCommand { set; get; }
        public DelegateCommand<ScreenshotModel> OutputScreenshotCommand { set; get; }

        #endregion

        private string _selectedDevice;
        private bool _isAscending;

        public ScreenShotListDialogViewModel()
        {
            SortScreenshotsCommand = new DelegateCommand(SortScreenshots);
            DeleteScreenshotCommand = new DelegateCommand<ScreenshotModel>(DeleteScreenshot);
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

        private void DeleteScreenshot(ScreenshotModel screenshot)
        {
            if (screenshot == null)
            {
                MessageBox.Show("请选择要删除的截屏");
                return;
            }

            var result = MessageBox.Show(
                "是否删除此截屏？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                var argument = new ArgumentCreator();
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("rm").Append(screenshot.FilePath);
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.Execute("adb");
                LoadScreenshots();
            }
        }

        private void OutputScreenshot(ScreenshotModel screenshot)
        {
            if (screenshot == null)
            {
                MessageBox.Show("请选择要导出的截屏");
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
            _selectedDevice = parameters.GetValue<string>("device");
            LoadScreenshots();
        }

        private void LoadScreenshots()
        {
            if (_screenshots.Any())
            {
                Screenshots.Clear();
            }

            var list = new List<string>();

            var argument = new ArgumentCreator();
            argument.Append("-s").Append(_selectedDevice).Append("shell").Append("ls").Append("/sdcard/*.png");
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += delegate(string value) { list.Add(value); };
            executor.Execute("adb");

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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Application = System.Windows.Application;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class ScreenshotExportDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "截屏导出";

        public event Action<IDialogResult> RequestClose;

        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevKit", "Cache", "Screenshots");

        private static readonly string[] ScanDirectories =
        {
            "/sdcard/DCIM/Screenshots",
            "/sdcard/Pictures/Screenshots",
            "/sdcard/ScreenShots",
            "/sdcard"
        };

        #region DelegateCommand

        public DelegateCommand RefreshCommand { set; get; }
        public DelegateCommand SortCommand { set; get; }
        public DelegateCommand SelectAllCommand { set; get; }
        public DelegateCommand DeleteSelectedCommand { set; get; }
        public DelegateCommand CaptureCommand { set; get; }
        public DelegateCommand<ScreenshotModel> ItemClickedCommand { set; get; }
        public DelegateCommand<ScreenshotModel> ItemDoubleClickedCommand { set; get; }
        public DelegateCommand<ScreenshotModel> DeleteSingleCommand { set; get; }
        public DelegateCommand BrowseCommand { set; get; }
        public DelegateCommand ExportCommand { set; get; }

        #endregion

        #region VM

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EmptyVisibility));
            }
        }

        private ObservableCollection<ScreenshotModel> _screenshots = new ObservableCollection<ScreenshotModel>();

        public ObservableCollection<ScreenshotModel> Screenshots
        {
            get => _screenshots;
            set
            {
                _screenshots = value;
                RaisePropertyChanged();
            }
        }

        public Visibility ExportProgressVisibility => IsExporting ? Visibility.Visible : Visibility.Collapsed;

        private bool _isExporting;

        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                _isExporting = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ExportProgressVisibility));
            }
        }

        private double _exportProgress;

        public double ExportProgress
        {
            get => _exportProgress;
            set
            {
                _exportProgress = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedCount;

        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                _selectedCount = value;
                RaisePropertyChanged();
            }
        }

        public Visibility EmptyVisibility =>
            _screenshots.Count == 0 && !_isLoading ? Visibility.Visible : Visibility.Collapsed;

        private string _saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public string SaveDirectory
        {
            get => _saveDirectory;
            set
            {
                _saveDirectory = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private string _device;
        private ListCollectionView _view;
        private bool _ascending;

        public ScreenshotExportDialogViewModel()
        {
            RefreshCommand = new DelegateCommand(LoadScreenshots);

            SortCommand = new DelegateCommand(() =>
            {
                _ascending = !_ascending;
                _view.SortDescriptions.Clear();
                _view.SortDescriptions.Add(new SortDescription("Time",
                    _ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));
            });

            SelectAllCommand = new DelegateCommand(() =>
            {
                var allSelected = _screenshots.Count > 0 && _screenshots.All(s => s.IsSelected);
                foreach (var s in _screenshots) s.IsSelected = !allSelected;
                RecalcSelectedCount();
            });

            DeleteSelectedCommand = new DelegateCommand(() =>
            {
                var selected = _screenshots.Where(s => s.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("请先选择要删除的截屏", "删除截屏",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"确定从设备上删除这 {selected.Count} 张截屏吗？此操作不可撤销。", "删除截屏",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.OK) return;

                foreach (var item in selected)
                {
                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(_device).Append("shell")
                        .Append("rm").Append(item.FilePath).ToCommandLine();
                    new CommandExecutor(cmdStr).Execute("adb");
                }

                foreach (var item in selected) _screenshots.Remove(item);
                RecalcSelectedCount();
                RaisePropertyChanged(nameof(EmptyVisibility));
            });

            CaptureCommand = new DelegateCommand(() =>
            {
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
                var argument = new ArgumentCreator();
                var cmdStr = argument.Append("-s").Append(_device).Append("shell")
                    .Append("screencap").Append("-p").Append($"/sdcard/{fileName}").ToCommandLine();
                new CommandExecutor(cmdStr).Execute("adb");
                LoadScreenshots();
            });

            ItemClickedCommand = new DelegateCommand<ScreenshotModel>(_ => RecalcSelectedCount());

            ItemDoubleClickedCommand = new DelegateCommand<ScreenshotModel>(item =>
            {
                if (item == null) return;
                Task.Run(() =>
                {
                    var cache = EnsureCached(item);
                    if (File.Exists(cache))
                    {
                        Process.Start(cache);
                    }
                });
            });

            DeleteSingleCommand = new DelegateCommand<ScreenshotModel>(item =>
            {
                var result = MessageBox.Show($"确定从设备上删除这张截屏吗？此操作不可撤销。", "删除截屏",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.OK) return;

                var argument = new ArgumentCreator();
                var cmdStr = argument.Append("-s").Append(_device).Append("shell")
                    .Append("rm").Append(item.FilePath).ToCommandLine();
                new CommandExecutor(cmdStr).Execute("adb");

                _screenshots.Remove(item);
                RecalcSelectedCount();
                RaisePropertyChanged(nameof(EmptyVisibility));
            });

            BrowseCommand = new DelegateCommand(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.SelectedPath = SaveDirectory;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveDirectory = dialog.SelectedPath;
                    }
                }
            });

            ExportCommand = new DelegateCommand(() =>
            {
                var selected = _screenshots.Where(s => s.IsSelected).Select(s => s.FilePath).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("请先选择要导出的截屏", "截屏导出", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SaveDirectory))
                {
                    MessageBox.Show("请选择保存目录", "截屏导出", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _ = ExportAsync(selected, SaveDirectory);
            });
        }

        public bool CanCloseDialog() => !IsExporting;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            _device = parameters.GetValue<string>("device");
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_screenshots);

            // 排序只设置一次，之后集合增删由 ListCollectionView 自动重排
            _view.SortDescriptions.Clear();
            _view.SortDescriptions.Add(new SortDescription("Time", ListSortDirection.Descending));
            LoadScreenshots();
        }

        private void LoadScreenshots()
        {
            if (string.IsNullOrEmpty(_device)) return;

            IsLoading = true;

            Task.Run(() =>
            {
                Directory.CreateDirectory(CacheDirectory);

                // 后台扫描
                var remote = ScanRemoteScreenshots();
                var added = new List<ScreenshotModel>();

                // UI 线程做 diff：新增的加进来，已消失的移除
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existing = new HashSet<string>(
                        _screenshots.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);
                    var remoteSet = new HashSet<string>(
                        remote.Select(m => m.FilePath), StringComparer.OrdinalIgnoreCase);

                    var removed = _screenshots.Where(s => !remoteSet.Contains(s.FilePath)).ToList();
                    added = remote.Where(m => !existing.Contains(m.FilePath)).ToList();

                    foreach (var m in removed) _screenshots.Remove(m);
                    foreach (var m in added) _screenshots.Add(m);

                    RecalcSelectedCount();
                    IsLoading = false;
                    RaisePropertyChanged(nameof(EmptyVisibility));
                });

                // 只为新增项加载缩略图
                LoadThumbnails(added);
            });
        }

        private List<ScreenshotModel> ScanRemoteScreenshots()
        {
            var result = new List<ScreenshotModel>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dir in ScanDirectories)
            {
                var argument = new ArgumentCreator();
                var cmdStr = argument.Append("-s").Append(_device).Append("shell")
                    .Append("ls").Append($"{dir}/*.png").ToCommandLine();

                var executor = new CommandExecutor(cmdStr);
                executor.OnStandardOutput += line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    line = line.Trim();
                    if (line.StartsWith("ls:")) return; // 目录不存在 / 无权限
                    if (!line.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
                    if (!seen.Add(line)) return; // 目录间去重

                    var model = ParseScreenshot(line);
                    if (model != null) result.Add(model);
                };
                executor.Execute("adb");
            }

            return result;
        }

        /// <summary>
        /// 重新统计当前被选中的截图数量，并同步到界面
        /// </summary>
        private void RecalcSelectedCount() => SelectedCount = _screenshots.Count(s => s.IsSelected);

        private static ScreenshotModel ParseScreenshot(string path)
        {
            return new ScreenshotModel
            {
                FilePath = path,
                Time = ParseTimeFromName(path)
            };
        }

        private static DateTime ParseTimeFromName(string path)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var match = Regex.Match(stem,
                @"(\d{4})[-_]?(\d{2})[-_]?(\d{2})[-_]?(\d{2})[-_]?(\d{2})[-_]?(\d{2})");
            if (match.Success)
            {
                var text = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value
                           + match.Groups[4].Value + match.Groups[5].Value + match.Groups[6].Value;
                if (DateTime.TryParseExact(text, "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }

            return DateTime.MinValue;
        }

        private void LoadThumbnails(List<ScreenshotModel> models)
        {
            foreach (var item in models)
            {
                try
                {
                    var cache = EnsureCached(item);
                    if (!File.Exists(cache)) continue;

                    var length = new FileInfo(cache).Length;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(cache, UriKind.Absolute);
                    bitmap.DecodePixelWidth = 180;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        item.Thumbnail = bitmap;
                        item.SizeText = length.ToFileSize();
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private string EnsureCached(ScreenshotModel item)
        {
            if (!string.IsNullOrEmpty(item.CachePath) && File.Exists(item.CachePath))
            {
                return item.CachePath;
            }

            var local = Path.Combine(CacheDirectory, Path.GetFileName(item.FilePath));
            item.CachePath = local;
            if (File.Exists(local)) return local;

            var argument = new ArgumentCreator();
            var cmdStr = argument.Append("-s").Append(_device)
                .Append("pull").Append(item.FilePath).Append(local).ToCommandLine();
            new CommandExecutor(cmdStr).Execute("adb");
            return local;
        }

        private async Task ExportAsync(List<string> remotePaths, string saveDirectory)
        {
            IsExporting = true;
            ExportProgress = 0;

            var total = remotePaths.Count;
            var done = 0;
            var failed = 0;

            try
            {
                Directory.CreateDirectory(saveDirectory);

                foreach (var remotePath in remotePaths)
                {
                    var fileName = Path.GetFileName(remotePath);
                    if (string.IsNullOrWhiteSpace(fileName)) continue;

                    var localPath = GetUniqueFilePath(Path.Combine(saveDirectory, fileName));
                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(_device)
                        .Append("pull").Append(remotePath).Append(localPath).ToCommandLine();

                    var exitCode = await Task.Run(() => new CommandExecutor(cmdStr).Execute("adb"));
                    if (exitCode != 0 || !File.Exists(localPath)) failed++;

                    done++;
                    ExportProgress = done * 100.0 / total; // 每导完一张更新一次
                }
            }
            finally
            {
                IsExporting = false; // 进度条自动隐藏
            }

            var success = done - failed;
            MessageBox.Show(failed == 0
                    ? $"已导出 {success} 张截屏到 {saveDirectory}"
                    : $"导出完成：成功 {success} 张，失败 {failed} 张",
                "截屏导出", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path)) return path;

            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);

            for (var i = 1; i < 1000; i++)
            {
                var candidate = Path.Combine(directory, $"{name} ({i}){extension}");
                if (!File.Exists(candidate)) return candidate;
            }

            return Path.Combine(directory, $"{name} ({DateTime.Now:HHmmss}){extension}");
        }
    }
}
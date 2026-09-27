using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DevKit.Cache;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Application = System.Windows.Application;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public class ApplicationPackageViewModel : BindableBase, IDialogAware
    {
        public string Title => "APK";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
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
        }

        #region VM

        private string _keyFilePath = string.Empty;

        public string KeyFilePath
        {
            get => _keyFilePath;
            set
            {
                _keyFilePath = value;
                RaisePropertyChanged();
            }
        }

        private string _keyAlias = string.Empty;

        public string KeyAlias
        {
            get => _keyAlias;
            set
            {
                _keyAlias = value;
                RaisePropertyChanged();
            }
        }

        private string _keyPassword = string.Empty;

        public string KeyPassword
        {
            get => _keyPassword;
            set
            {
                _keyPassword = value;
                RaisePropertyChanged();
            }
        }

        private string _jdkPath = string.Empty;

        public string JdkPath
        {
            get => _jdkPath;
            set
            {
                _jdkPath = value;
                RaisePropertyChanged();
            }
        }

        private string _outputResult = "请手动查看";

        public string OutputResult
        {
            get => _outputResult;
            set
            {
                _outputResult = value;
                RaisePropertyChanged();
            }
        }

        private string _apkRootFolderPath = string.Empty;

        public string ApkRootFolderPath
        {
            get => _apkRootFolderPath;
            set
            {
                _apkRootFolderPath = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ApkFileModel> _apkFileCollection;

        public ObservableCollection<ApkFileModel> ApkFileCollection
        {
            get => _apkFileCollection;
            set
            {
                _apkFileCollection = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand SelectKeyCommand { set; get; }
        public DelegateCommand SelectJdkCommand { set; get; }
        public DelegateCommand ShowSha1Command { set; get; }
        public DelegateCommand SelectApkRootFolderCommand { set; get; }
        public DelegateCommand RefreshApkFilesCommand { set; get; }
        public DelegateCommand<string> OpenFileFolderCommand { set; get; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        public ApplicationPackageViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            var config = SettingsStore.Load<ApkConfigCache>(ApkConfigCache.FileName);
            JdkPath = config.JdkPath;
            KeyFilePath = config.KeyPath;
            KeyAlias = config.Alias;
            KeyPassword = config.Password;
            ApkRootFolderPath = config.ApkRootFolder;

            SelectKeyCommand = new DelegateCommand(() =>
            {
                var fileDialog = new OpenFileDialog
                {
                    DefaultExt = ".jks",
                    Filter = "秘钥文件(*.jks)|*.jks"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    KeyFilePath = fileDialog.FileName;
                    UpdateConfigCache();
                }
            });

            SelectJdkCommand = new DelegateCommand(() =>
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        //C:\Program Files\Java\jdk1.8.0_311
                        //C:\Program Files\Java\jdk1.8.0_311\bin
                        var selectedPath = folderDialog.SelectedPath;
                        if (!selectedPath.EndsWith("bin", StringComparison.OrdinalIgnoreCase))
                        {
                            selectedPath = Path.Combine(selectedPath, "bin");
                        }

                        JdkPath = selectedPath;
                        UpdateConfigCache();
                    }
                }
            });

            ShowSha1Command = new DelegateCommand(() => _ = ShowSha1Async());

            SelectApkRootFolderCommand = new DelegateCommand(SelectApkRootFolder);
            RefreshApkFilesCommand = new DelegateCommand(RefreshApkFiles);
            OpenFileFolderCommand = new DelegateCommand<string>(OpenFileFolder);
        }

        private async Task ShowSha1Async()
        {
            if (string.IsNullOrWhiteSpace(_keyFilePath) || string.IsNullOrWhiteSpace(_keyAlias) ||
                string.IsNullOrWhiteSpace(_keyPassword))
            {
                MessageBox.Show("请完善签名Key配置", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // keytool 路径检查放在 UI 线程，避免后台线程弹窗
            var keytoolPath = Path.Combine(_jdkPath, "keytool.exe");
            if (!File.Exists(keytoolPath))
            {
                MessageBox.Show("keytool 未找到，请检查 JDK 路径是否正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 清空输出结果
            if (!string.IsNullOrEmpty(_outputResult)) OutputResult = string.Empty;

            try
            {
                var output = await Task.Run(() => RunKeytool(keytoolPath, _keyAlias, _keyFilePath, _keyPassword));

                // keytool 的错误信息固定以 "keytool error" 开头（走 stdout）
                if (output.StartsWith("keytool error", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(output, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OutputResult = output;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectApkRootFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = @"请选择apk安装包归档的根目录";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    ApkRootFolderPath = folderDialog.SelectedPath;
                    UpdateConfigCache();

                    ApkFileCollection?.Clear();

                    //异步遍历文件夹下面的apk文件
                    var dialogParameters = new DialogParameters
                    {
                        { "LoadingMessage", "文件检索中，请稍后......" }
                    };
                    _dialogService.Show("LoadingDialog", dialogParameters, delegate { });
                    Task.Run(async () =>
                    {
                        var totalFiles = await GetApkFilesAsync();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
                            if (!totalFiles.Any())
                            {
                                Growl.Info("该文件夹下面不包含Android安装包");
                            }

                            ApkFileCollection = totalFiles.ToObservableCollection();
                        });
                    });
                }
            }
        }

        private async Task<List<ApkFileModel>> GetApkFilesAsync()
        {
            var list = new List<ApkFileModel>();
            await Task.Run(() => TraverseFolder(_apkRootFolderPath, list));
            return list;
        }

        /// <summary>
        /// 遍历文件夹并生成相应的数据类型集合
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="apkFiles"></param>
        private void TraverseFolder(string folderPath, List<ApkFileModel> apkFiles)
        {
            var files = new DirectoryInfo(folderPath)
                .GetFiles("*.apk", SearchOption.AllDirectories)
                .OrderBy(file => file.LastWriteTime)
                .Reverse();
            foreach (var file in files)
            {
                var fullName = file.FullName;
                if (fullName.Contains("debug") || file.Name.StartsWith(".")) continue;

                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
                var index = nameWithoutExtension.IndexOf("20", StringComparison.Ordinal);
                var fileName = index < 0 ? nameWithoutExtension : nameWithoutExtension.Substring(0, index - 1);

                var apk = new ApkFileModel
                {
                    FileName = fileName,
                    FullName = fullName,
                    FileSize = file.Length.ToFileSize(),
                    ModifyTime = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 匹配日期和版本号的正则表达式模式，支持 YYYYMMDD_版本号 或 _XX_YYYYMMDD_版本号 或 YYYYMMDD_版本号_附加信息
                const string pattern = @"^(.+?)(?:_[A-Za-z0-9]+)?_(\d{8})_((?:\d+\.)*\d+)(?:_(.+))?$";
                var match = Regex.Match(nameWithoutExtension, pattern);
                if (match.Success)
                {
                    apk.BuildTime = match.Groups[2].Value; // 提取日期 20260101
                    apk.Version = match.Groups[3].Value; // 提取版本号 1.0.1.0
                    apk.ExtraInfo = match.Groups[4].Success ? match.Groups[4].Value : string.Empty; // 提取附加信息
                }

                apkFiles.Add(apk);
            }
        }

        private void RefreshApkFiles()
        {
            if (string.IsNullOrWhiteSpace(_apkRootFolderPath))
            {
                MessageBox.Show("Android安装包根目录路径为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ApkFileCollection?.Clear();
            //异步遍历文件夹下面的apk文件
            var dialogParameters = new DialogParameters
            {
                { "LoadingMessage", "文件检索中，请稍后......" }
            };
            _dialogService.Show("LoadingDialog", dialogParameters, delegate { });
            Task.Run(async () =>
            {
                var totalFiles = await GetApkFilesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
                    if (!totalFiles.Any())
                    {
                        Growl.Info("该文件夹下面不包含Android安装包");
                    }

                    ApkFileCollection = totalFiles.ToObservableCollection();
                });
            });
        }

        private void OpenFileFolder(string path)
        {
            if (path == null)
            {
                return;
            }

            var directoryPath = Path.GetDirectoryName(path);
            try
            {
                Debug.Assert(directoryPath != null, nameof(directoryPath) + " != null");
                Process.Start(new ProcessStartInfo()
                {
                    FileName = directoryPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---- 私有辅助函数 -----

        private void UpdateConfigCache()
        {
            SettingsStore.Save(ApkConfigCache.FileName, new ApkConfigCache
            {
                JdkPath = _jdkPath,
                KeyPath = _keyFilePath,
                Alias = _keyAlias,
                Password = KeyPassword,
                ApkRootFolder = _apkRootFolderPath
            });
        }

        private string RunKeytool(string keytoolPath, string alias, string keyFile, string password)
        {
            var psi = new ProcessStartInfo
            {
                FileName = keytoolPath,
                Arguments = new ArgumentCreator()
                    .Append("-v")
                    .Append("-list")
                    .Append("-alias").Append(alias)
                    .Append("-keystore").Append(keyFile)
                    .Append("-storepass").Append(password)
                    .ToCommandLine(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 keytool 进程");
            }

            using (process)
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Task.WaitAll(output, error); // 等异步读取结束，确保拿全输出
                return string.IsNullOrWhiteSpace(output.Result) ? error.Result?.Trim() : output.Result?.Trim();
            }
        }
    }
}
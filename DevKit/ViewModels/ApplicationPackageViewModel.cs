using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
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
        public string Title => "安装包归档";

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

        private string _outputResult = "配置完成后，点击“查看 SHA1”读取证书信息。";

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

        private string _keyword = string.Empty;

        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                RaisePropertyChanged();
                ApplyFilter();
            }
        }

        private Visibility _scanVisibility = Visibility.Hidden;

        public Visibility ScanVisibility
        {
            get => _scanVisibility;
            set
            {
                _scanVisibility = value;
                RaisePropertyChanged();
            }
        }

        private double _scanProgress;

        public double ScanProgress
        {
            get => _scanProgress;
            set
            {
                _scanProgress = value;
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

        public ApplicationPackageViewModel()
        {
            var config = SettingsStore.Load<AppConfigCache>(AppConfigCache.FileName);
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

            SelectApkRootFolderCommand = new DelegateCommand(() =>
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = @"请选择apk安装包归档的根目录";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        ApkRootFolderPath = folderDialog.SelectedPath;
                        UpdateConfigCache();
                        StartScan();
                    }
                }
            });

            RefreshApkFilesCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(_apkRootFolderPath))
                {
                    MessageBox.Show("Android安装包根目录路径为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StartScan();
            });

            OpenFileFolderCommand = new DelegateCommand<string>(path =>
            {
                var directoryPath = Path.GetDirectoryName(path);
                if (directoryPath == null) return;
                Process.Start(new ProcessStartInfo()
                {
                    FileName = directoryPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            });
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

        /// <summary>
        /// 统一扫描入口
        /// </summary>
        private void StartScan()
        {
            ApkFileCollection?.Clear();

            ScanProgress = 0;
            ScanVisibility = Visibility.Visible;

            Task.Run(async () =>
            {
                try
                {
                    var totalFiles = await GetApkFilesAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ApkFileCollection = totalFiles.ToObservableCollection();
                        ApplyFilter();
                    });
                }
                catch (Exception e)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"扫描失败：{e.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    // 无论成败，进度条一定收起
                    Application.Current.Dispatcher.Invoke(() => { ScanVisibility = Visibility.Collapsed; });
                }
            });
        }

        private Task<List<ApkFileModel>> GetApkFilesAsync()
        {
            var list = new List<ApkFileModel>();
            return Task.Run(() =>
            {
                TraverseFolder(_apkRootFolderPath, list, (processed, total) =>
                {
                    var progress = total == 0 ? 100 : processed * 100.0 / total;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ScanProgress = Math.Round(progress);
                    }));
                });
                return list;
            });
        }

        /// <summary>
        /// 遍历文件夹并生成相应的数据类型集合
        /// </summary>
        private void TraverseFolder(string folderPath, List<ApkFileModel> apkFiles, Action<int, int> onProgress = null)
        {
            var files = new DirectoryInfo(folderPath)
                .GetFiles("*.apk", SearchOption.AllDirectories)
                .OrderBy(file => file.LastWriteTime)
                .Reverse()
                .ToArray();

            var total = files.Length;
            var processed = 0;

            foreach (var file in files)
            {
                // 进度按「已处理的文件数」上报（含被过滤的 debug 文件），放在 continue 之前
                processed++;
                onProgress?.Invoke(processed, total);

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
                    ModifyTime = file.LastWriteTime.ToString("yyyy-MM-dd")
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

        // ---- 私有辅助函数 -----

        private void UpdateConfigCache()
        {
            SettingsStore.Save(AppConfigCache.FileName, new AppConfigCache
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
        
        /// <summary>
        /// 按名称或版本过滤 APK 列表
        /// </summary>
        private void ApplyFilter()
        {
            if (ApkFileCollection == null) return;

            var view = CollectionViewSource.GetDefaultView(ApkFileCollection);
            var keyword = (Keyword ?? string.Empty).Trim();

            view.Filter = o =>
            {
                if (keyword.Length == 0) return true;

                if (!(o is ApkFileModel apk)) return false;

                var matchName = apk.FileName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                var matchVersion = apk.Version?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                return matchName || matchVersion;
            };
            view.Refresh();
        }
    }
}
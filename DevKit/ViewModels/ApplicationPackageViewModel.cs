using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Application = System.Windows.Application;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    [SupportedOSPlatform("windows6.1")]
    public partial class ApplicationPackageViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private string _keyFilePath = string.Empty;
        [ObservableProperty] private string _keyAlias = string.Empty;
        [ObservableProperty] private string _keyPassword = string.Empty;
        [ObservableProperty] private string _jdkPath = string.Empty;
        [ObservableProperty] private string _outputResult = "配置完成后，点击“查看 SHA1”读取证书信息。";
        [ObservableProperty] private string _apkRootFolderPath = string.Empty;
        [ObservableProperty] private string _keyword = string.Empty;
        [ObservableProperty] private Visibility _scanVisibility = Visibility.Hidden;
        [ObservableProperty] private double _scanProgress;
        [ObservableProperty] private ObservableCollection<ApkFile> _apkFileCollection;

        partial void OnKeywordChanged(string value)
        {
            ApplyFilter(value);
        }

        #endregion

        public ApplicationPackageViewModel()
        {
            var config = SettingsStore.Load<AppPackageConfig>(ConfigSections.FileName, ConfigSections.Apk);
            JdkPath = config.JdkPath;
            KeyFilePath = config.KeyPath;
            KeyAlias = config.Alias;
            KeyPassword = config.Password;
            ApkRootFolderPath = config.ApkRootFolder;
        }

        [RelayCommand]
        private void SelectKey()
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".jks",
                Filter = "秘钥文件(*.jks)|*.jks"
            };

            if (fileDialog.ShowDialog() == true)
            {
                KeyFilePath = fileDialog.FileName;
            }
        }
        
        [RelayCommand]
        private void SelectJdk()
        {
            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = folderDialog.SelectedPath;
                if (!selectedPath.EndsWith("bin", StringComparison.OrdinalIgnoreCase))
                {
                    selectedPath = Path.Combine(selectedPath, "bin");
                }

                JdkPath = selectedPath;
            }
        }

        [RelayCommand]
        private Task ShowSha1()
        {
            return ShowSha1Async();
        }
        
        private async Task ShowSha1Async()
        {
            if (string.IsNullOrWhiteSpace(KeyFilePath) || string.IsNullOrWhiteSpace(KeyAlias) ||
                string.IsNullOrWhiteSpace(KeyPassword))
            {
                MessageBox.Show("请完善签名Key配置", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // keytool 路径检查放在 UI 线程，避免后台线程弹窗
            var keytoolPath = Path.Combine(JdkPath, "keytool.exe");
            if (!File.Exists(keytoolPath))
            {
                MessageBox.Show("keytool 未找到，请检查 JDK 路径是否正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 清空输出结果
            if (!string.IsNullOrEmpty(OutputResult)) OutputResult = string.Empty;

            try
            {
                var output = await Task.Run(() => RunKeytool(keytoolPath, KeyAlias, KeyFilePath, KeyPassword));

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

        [RelayCommand]
        private void SelectApkRootFolder()
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = @"请选择apk安装包归档的根目录";
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                ApkRootFolderPath = folderDialog.SelectedPath;
                StartScan();
            }
        }
        
        /// <summary>
        /// 统一扫描入口
        /// </summary>
        private void StartScan()
        {
            ApkFileCollection = [];

            ScanProgress = 0;
            ScanVisibility = Visibility.Visible;

            _ = Task.Run(() =>
            {
                try
                {
                    TraverseFolder(ApkRootFolderPath, (apk, processed, total) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (apk != null)
                            {
                                ApkFileCollection.Add(apk);
                                ApplyFilter(apk.FileName); // 重新应用过滤器，确保新添加的项符合当前关键字
                            }

                            ScanProgress = total == 0 ? 100 : Math.Round(processed * 100.0 / total);
                        });
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ScanProgress = 100;
                        ScanVisibility = Visibility.Collapsed;
                    });
                }
            });
        }

        /// <summary>
        /// 遍历文件夹并生成相应的数据类型集合
        /// </summary>
        private void TraverseFolder(string folderPath, Action<ApkFile, int, int> onFileScanned)
        {
            var files = new DirectoryInfo(folderPath)
                .GetFiles("*.apk", SearchOption.AllDirectories)
                .OrderByDescending(file => file.LastWriteTime)
                .ToArray();

            var total = files.Length;
            var processed = 0;

            foreach (var file in files)
            {
                processed++;

                ApkFile apk = null;
                var fullName = file.FullName;

                if (fullName.IndexOf("debug", StringComparison.OrdinalIgnoreCase) < 0 && !file.Name.StartsWith("."))
                {
                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
                    var index = nameWithoutExtension.IndexOf("20", StringComparison.Ordinal);
                    var fileName = index < 0 ? nameWithoutExtension : nameWithoutExtension.Substring(0, index - 1);

                    apk = new ApkFile
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
                        apk.BuildTime = match.Groups[2].Value;
                        apk.Version = match.Groups[3].Value;
                        apk.ExtraInfo = match.Groups[4].Success ? match.Groups[4].Value : string.Empty;
                    }
                }

                onFileScanned?.Invoke(apk, processed, total);
            }
        }

        [RelayCommand]
        private void RefreshApkFiles()
        {
            if (string.IsNullOrWhiteSpace(ApkRootFolderPath))
            {
                MessageBox.Show("Android安装包根目录路径为空",
                    "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StartScan();
        }

        [RelayCommand]
        private void OpenFileFolder(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            if (directoryPath == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        
        // ---- 私有辅助函数 -----

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
                return string.IsNullOrWhiteSpace(output.Result) ? error.Result.Trim() : output.Result.Trim();
            }
        }

        /// <summary>
        /// 按名称或版本过滤 APK 列表
        /// </summary>
        private void ApplyFilter(string keyword)
        {
            var view = CollectionViewSource.GetDefaultView(ApkFileCollection);

            // 无关键字：移除过滤，避免空谓词对每项做无谓调用
            if (keyword.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                return;
            }

            view.Filter = o =>
            {
                if (o is not ApkFile apk) return false;

                var matchName = apk.FileName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                var matchVersion = apk.Version?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                return matchName || matchVersion;
            };
            view.Refresh();
        }
        
        public void SaveConfiguration()
        {
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Apk, new AppPackageConfig
            {
                JdkPath = JdkPath,
                KeyPath = KeyFilePath,
                Alias = KeyAlias,
                Password = KeyPassword,
                ApkRootFolder = ApkRootFolderPath
            });
        }
    }
}
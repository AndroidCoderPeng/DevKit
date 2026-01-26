using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private string _outputResult = string.Empty;

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

        public DelegateCommand SelectJdkCommand { set; get; }
        public DelegateCommand SelectKeyCommand { set; get; }
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

            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ApkConfigCache>().OrderByDescending(x => x.Id).FirstOrDefault();
                if (queryResult != null)
                {
                    JdkPath = queryResult.JdkPath;
                    KeyFilePath = queryResult.KeyPath;
                    KeyAlias = queryResult.Alias;
                    KeyPassword = queryResult.Password;
                    ApkRootFolderPath = queryResult.ApkRootFolder;
                }
            }

            SelectJdkCommand = new DelegateCommand(SelectJdk);
            SelectKeyCommand = new DelegateCommand(SelectKey);
            ShowSha1Command = new DelegateCommand(ShowSha1Async);
            SelectApkRootFolderCommand = new DelegateCommand(SelectApkRootFolder);
            RefreshApkFilesCommand = new DelegateCommand(RefreshApkFiles);
            OpenFileFolderCommand = new DelegateCommand<string>(OpenFileFolder);
        }

        private void SelectJdk()
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
        }

        private void SelectKey()
        {
            var fileDialog = new OpenFileDialog
            {
                // 设置默认格式
                DefaultExt = ".jks",
                Filter = "秘钥文件(*.jks)|*.jks"
            };
            var result = fileDialog.ShowDialog();
            if (result == true)
            {
                KeyFilePath = fileDialog.FileName;
                UpdateConfigCache();
            }
        }

        private async void ShowSha1Async()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_keyFilePath) || string.IsNullOrWhiteSpace(_keyAlias) ||
                    string.IsNullOrWhiteSpace(_keyPassword))
                {
                    MessageBox.Show("请完善签名Key配置", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                UpdateConfigCache();

                if (!string.IsNullOrEmpty(_outputResult))
                {
                    OutputResult = string.Empty;
                }

                var list = new List<string>();
                await Task.Run(() => ExecuteCommand(list));
                var builder = new StringBuilder();
                for (var i = 0; i < list.Count; i++)
                {
                    switch (i)
                    {
                        case 1:
                        case 5:
                        case 8:
                            builder.Append(list[i]).Append(Environment.NewLine);
                            break;
                        case 10:
                            var sha1 = list[i].Replace("\t", "").Replace(" ", "").Replace("SHA1:", "SHA1值: ");
                            builder.Append(sha1).Append(Environment.NewLine);
                            break;
                        case 12:
                            builder.Append(list[i]);
                            break;
                    }
                }

                if (builder.ToString().Contains("Exception"))
                {
                    MessageBox.Show(builder.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OutputResult = builder.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCommand(List<string> list)
        {
            var keytoolPath = Path.Combine(_jdkPath, "keytool.exe");
            if (!File.Exists(keytoolPath))
            {
                MessageBox.Show("keytool 未找到，请检查 JDK 路径是否正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var argument = new ArgumentCreator();
            argument.Append("-v")
                .Append("-list")
                .Append("-alias").Append(_keyAlias)
                .Append("-keystore").Append(_keyFilePath)
                .Append("-storepass").Append(_keyPassword);
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += list.Add;
            executor.Execute(keytoolPath);
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

        private void UpdateConfigCache()
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ApkConfigCache>().OrderByDescending(x => x.Id).FirstOrDefault();
                if (queryResult == null)
                {
                    var config = new ApkConfigCache
                    {
                        JdkPath = _jdkPath,
                        KeyPath = _keyFilePath,
                        Alias = _keyAlias,
                        Password = KeyPassword,
                        ApkRootFolder = _apkRootFolderPath
                    };
                    dataBase.Insert(config);
                }
                else
                {
                    queryResult.JdkPath = _jdkPath;
                    queryResult.KeyPath = _keyFilePath;
                    queryResult.Alias = _keyAlias;
                    queryResult.Password = KeyPassword;
                    queryResult.ApkRootFolder = _apkRootFolderPath;
                    dataBase.Update(queryResult);
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
    }
}
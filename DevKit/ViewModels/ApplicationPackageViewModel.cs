using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DevKit.Configs;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public class ApplicationPackageViewModel : BindableBase
    {
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

        public DelegateCommand CreateKeyCommand { set; get; }
        public DelegateCommand SelectKeyCommand { set; get; }
        public DelegateCommand ShowSha1Command { set; get; }
        public DelegateCommand SelectApkRootFolderCommand { set; get; }
        public DelegateCommand RefreshApkFilesCommand { set; get; }
        public DelegateCommand<string> OpenFileFolderCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ConfigCache _configCache = new ConfigCache();
        private readonly Apk _apk = new Apk();

        public ApplicationPackageViewModel(IAppDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            var config = dataService.LoadCacheConfig();
            if (config != null)
            {
                KeyFilePath = config.Apk.KeyPath;
                KeyAlias = config.Apk.Alias;
                KeyPassword = config.Apk.Password;
                ApkRootFolderPath = config.Apk.RootFolder;
            }

            CreateKeyCommand = new DelegateCommand(CreateKey);
            SelectKeyCommand = new DelegateCommand(SelectKey);
            ShowSha1Command = new DelegateCommand(ShowSha1Async);
            SelectApkRootFolderCommand = new DelegateCommand(SelectApkRootFolder);
            RefreshApkFilesCommand = new DelegateCommand(RefreshApkFiles);
            OpenFileFolderCommand = new DelegateCommand<string>(OpenFileFolder);
        }

        private void CreateKey()
        {
            var dialogParameters = new DialogParameters
            {
                { "Title", "新建签名证书" }
            };
            _dialogService.ShowDialog("CreateKeyDialog", dialogParameters, delegate(IDialogResult result)
                {
                    KeyFilePath = result.Parameters.GetValue<string>("KeySavePath");
                    KeyAlias = result.Parameters.GetValue<string>("KeyAlias");
                    KeyPassword = result.Parameters.GetValue<string>("KeyPassword");
                }
            );
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

                //保存配置
                _apk.KeyPath = _keyFilePath;
                _apk.Alias = _keyAlias;
                _apk.Password = _keyPassword;
                _configCache.Apk = _apk;
                _dataService.SaveCacheConfig(_configCache);

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

                OutputResult = builder.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void ExecuteCommand(List<string> list)
        {
            var argument = new ArgumentCreator();
            argument.Append("-v")
                .Append("-list")
                .Append("-alias").Append(_keyAlias)
                .Append("-keystore").Append(_keyFilePath)
                .Append("-storepass").Append(_keyPassword);
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += list.Add;
            executor.Execute("keytool");
        }

        private async void SelectApkRootFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = @"请选择apk安装包归档的根目录";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    ApkRootFolderPath = folderDialog.SelectedPath;

                    _apk.RootFolder = _apkRootFolderPath;
                    _configCache.Apk = _apk;
                    _dataService.SaveCacheConfig(_configCache);

                    //异步遍历文件夹下面的apk文件
                    var dialogParameters = new DialogParameters
                    {
                        { "LoadingMessage", "文件检索中，请稍后......" }
                    };
                    _dialogService.Show("LoadingDialog", dialogParameters, delegate { });
                    var totalFiles = await GetApkFilesAsync();
                    _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
                    ApkFileCollection = totalFiles.ToObservableCollection();
                }
            }
        }

        private async Task<List<ApkFileModel>> GetApkFilesAsync()
        {
            var list = new List<ApkFileModel>();
            await Task.Run(() => TraverseFolder(_apkRootFolderPath, list));
            return list.OrderBy(file => file.CreationTime).Reverse().ToList();
        }

        /// <summary>
        /// 遍历文件夹并生成相应的数据类型集合
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="apkFiles"></param>
        private void TraverseFolder(string folderPath, List<ApkFileModel> apkFiles)
        {
            var files = new DirectoryInfo(folderPath).GetFiles("*.apk", SearchOption.AllDirectories)
                .OrderBy(file => file.CreationTime)
                .Reverse();
            foreach (var file in files)
            {
                if (file.FullName.Contains("debug") || file.Name.StartsWith(".")) continue;
                var apkFile = new ApkFileModel
                {
                    FileName = file.Name,
                    FullPath = file.FullName,
                    FileSize = file.Length.FormatFileSize(),
                    CreationTime = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                };

                apkFiles.Add(apkFile);
            }
        }

        private async void RefreshApkFiles()
        {
            ApkFileCollection?.Clear();
            //异步遍历文件夹下面的apk文件
            var dialogParameters = new DialogParameters
            {
                { "LoadingMessage", "文件检索中，请稍后......" }
            };
            _dialogService.Show("LoadingDialog", dialogParameters, delegate { });
            var totalFiles = await GetApkFilesAsync();
            _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
            ApkFileCollection = totalFiles.ToObservableCollection();
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
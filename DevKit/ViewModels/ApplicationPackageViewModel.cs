using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Models;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

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

        private ObservableCollection<ApkFileModel> _apkFileCollection = new ObservableCollection<ApkFileModel>();

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

        #endregion

        private readonly IDialogService _dialogService;

        public ApplicationPackageViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            CreateKeyCommand = new DelegateCommand(CreateKey);
            SelectKeyCommand = new DelegateCommand(SelectKey);
            ShowSha1Command = new DelegateCommand(ShowSha1Async);
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

                if (!string.IsNullOrEmpty(_outputResult))
                {
                    OutputResult = string.Empty;
                }

                var list = new List<string>();
                await Task.Run(() => ExecuteCommand(list));
                var builder = new StringBuilder();
                for (var i = 0; i < list.Count; i++)
                {
                    Console.WriteLine($@"{i} ===> {list[i]}");
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
    }
}
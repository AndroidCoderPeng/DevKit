using System;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class CreateKeyDialogViewModel : BindableBase, IDialogAware
    {
        public string Title =>  "新建签名证书";
        public event Action<IDialogResult> RequestClose;

        #region VM

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

        private string _keySurname = string.Empty;

        public string KeySurname
        {
            get => _keySurname;
            set
            {
                _keySurname = value;
                RaisePropertyChanged();
            }
        }

        private string _keyCompany = string.Empty;

        public string KeyCompany
        {
            get => _keyCompany;
            set
            {
                _keyCompany = value;
                RaisePropertyChanged();
            }
        }

        private string _keyDistrict = string.Empty;

        public string KeyDistrict
        {
            get => _keyDistrict;
            set
            {
                _keyDistrict = value;
                RaisePropertyChanged();
            }
        }

        private string _keyProvince = string.Empty;

        public string KeyProvince
        {
            get => _keyProvince;
            set
            {
                _keyProvince = value;
                RaisePropertyChanged();
            }
        }

        private string _keySavePath = string.Empty;

        public string KeySavePath
        {
            get => _keySavePath;
            set
            {
                _keySavePath = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand SaveKeyCommand { set; get; }
        public DelegateCommand GenerateKeyCommand { set; get; }

        #endregion

        public CreateKeyDialogViewModel()
        {
            SaveKeyCommand = new DelegateCommand(SaveKey);
            GenerateKeyCommand = new DelegateCommand(GenerateKey);
        }

        private void SaveKey()
        {
            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".jks",
                Filter = "证书文件(*.jks)|*.jks",
                RestoreDirectory = true
            };
            if (fileDialog.ShowDialog() == true)
            {
                KeySavePath = fileDialog.FileName;
            }
        }

        private void GenerateKey()
        {
            if (string.IsNullOrWhiteSpace(_keyAlias) || string.IsNullOrWhiteSpace(_keyPassword) ||
                string.IsNullOrWhiteSpace(_keySurname) || string.IsNullOrWhiteSpace(_keyCompany) ||
                string.IsNullOrWhiteSpace(_keyDistrict) || string.IsNullOrWhiteSpace(_keyProvince) ||
                string.IsNullOrWhiteSpace(_keySavePath))
            {
                MessageBox.Show("请完善证书配置", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //keytool -importkeystore -srckeystore C:\Users\pengx\Desktop\test.jks -destkeystore C:\Users\pengx\Desktop\test.jks -deststoretype pkcs12
                argument.Append("-genkeypair")
                    .Append("-alias").Append(_keyAlias)
                    .Append("-keypass").Append(_keyPassword)
                    .Append("-keystore").Append(_keySavePath)
                    .Append("-storepass").Append(_keyPassword)
                    .Append("-validity").Append("3650")
                    .Append("-dname")
                    .Append($"CN={_keySurname},OU={_keyCompany},L={_keyDistrict},ST={KeyProvince},C=CN");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate
                {
                    var dialogParameters = new DialogParameters
                    {
                        { "KeySavePath", _keySavePath },
                        { "KeyAlias", _keyAlias },
                        { "KeyPassword", _keyPassword }
                    };
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
                    });
                };
                executor.Execute("keytool");
            });
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
    }
}
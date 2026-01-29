using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DevKit.Cache;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public class JNIReverseViewModel : BindableBase, IDialogAware
    {
        public string Title => "JNI逆向";

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

        private string _ndkPath = string.Empty;

        public string NdkPath
        {
            get => _ndkPath;
            set
            {
                _ndkPath = value;
                RaisePropertyChanged();
            }
        }

        private string _sharedFilePath = string.Empty;

        public string SharedFilePath
        {
            get => _sharedFilePath;
            set
            {
                _sharedFilePath = value;
                RaisePropertyChanged();
            }
        }

        private string _stackAddress = string.Empty;

        public string StackAddress
        {
            get => _stackAddress;
            set
            {
                _stackAddress = value;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand SelectNdkCommand { set; get; }
        public DelegateCommand SelectSharedFileCommand { set; get; }
        public DelegateCommand ReverseAddressCommand { set; get; }

        #endregion

        public JNIReverseViewModel()
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<SdkConfigCache>().OrderByDescending(x => x.Id).FirstOrDefault();
                if (queryResult != null)
                {
                    NdkPath = queryResult.NdkPath;
                }
            }

            SelectNdkCommand = new DelegateCommand(SelectNdk);
            SelectSharedFileCommand = new DelegateCommand(SelectSharedFile);
            ReverseAddressCommand = new DelegateCommand(ReverseAddressAsync);
        }

        private void SelectNdk()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    //D:\Dev\Android\Sdk\ndk\21.4.7075529
                    NdkPath = folderDialog.SelectedPath;
                    UpdateConfigCache();
                }
            }
        }

        private void UpdateConfigCache()
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<SdkConfigCache>().OrderByDescending(x => x.Id).FirstOrDefault();
                if (queryResult == null)
                {
                    var config = new SdkConfigCache
                    {
                        NdkPath = _ndkPath
                    };
                    dataBase.Insert(config);
                }
                else
                {
                    queryResult.NdkPath = _ndkPath;
                    dataBase.Update(queryResult);
                }
            }
        }

        private void SelectSharedFile()
        {
            var fileDialog = new OpenFileDialog
            {
                // 设置默认格式
                DefaultExt = ".so",
                Filter = "动态库文件(*.so)|*.so"
            };
            var result = fileDialog.ShowDialog();
            if (result == true)
            {
                SharedFilePath = fileDialog.FileName;
            }
        }

        private async void ReverseAddressAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_ndkPath) || string.IsNullOrWhiteSpace(_sharedFilePath) ||
                    string.IsNullOrWhiteSpace(_stackAddress))
                {
                    MessageBox.Show("请完善缺少的参数", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!string.IsNullOrEmpty(_outputResult))
                {
                    OutputResult = string.Empty;
                }

                var list = new List<string>();
                await Task.Run(() => ExecuteCommand(list));
                var builder = new StringBuilder();
                foreach (var str in list)
                {
                    builder.Append(str).Append(Environment.NewLine);
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
            // D:\Dev\Android\Sdk\ndk\21.4.7075529
            var toolPath = $"{_ndkPath}/toolchains/llvm/prebuilt/windows-x86_64/bin";
            var keytoolPath = Path.Combine(toolPath, "aarch64-linux-android-addr2line.exe");
            if (!File.Exists(keytoolPath))
            {
                MessageBox.Show("请检查 NDK 是否完整。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var argument = new ArgumentCreator();
            argument.Append("-e")
                .Append(_sharedFilePath)
                .Append("-f")
                .Append("-C")
                .Append(_stackAddress);
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += list.Add;
            executor.Execute(keytoolPath);
        }
    }
}
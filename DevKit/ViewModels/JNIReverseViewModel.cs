using System;
using System.Collections.Generic;
using System.IO;
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
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Jni, new JniReverseConfig
            {
                NdkPath = _ndkPath,
                SharedLibPath = _sharedLibPath
            });
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

        private string _ndkState = string.Empty;

        public string NdkState
        {
            get => _ndkState;
            set
            {
                _ndkState = value;
                RaisePropertyChanged();
            }
        }

        private string _sharedLibPath = string.Empty;

        public string SharedLibPath
        {
            get => _sharedLibPath;
            set
            {
                _sharedLibPath = value;
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
        public DelegateCommand SelectSharedLibCommand { set; get; }
        public DelegateCommand ReverseAddressCommand { set; get; }

        #endregion

        public JNIReverseViewModel()
        {
            NdkPath = SettingsStore.Load<JniReverseConfig>(ConfigSections.FileName, ConfigSections.Jni).NdkPath;

            SelectNdkCommand = new DelegateCommand(() =>
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() != DialogResult.OK) return;
                    NdkPath = folderDialog.SelectedPath;
                }
            });

            SelectSharedLibCommand = new DelegateCommand(() =>
            {
                var fileDialog = new OpenFileDialog
                {
                    // 设置默认格式
                    DefaultExt = ".so",
                    Filter = "动态库文件(*.so)|*.so"
                };
                if (fileDialog.ShowDialog() != true) return;
                SharedLibPath = fileDialog.FileName;
            });

            ReverseAddressCommand = new DelegateCommand(() => _ = ReverseAddressAsync());
        }

        private async Task ReverseAddressAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_ndkPath) || string.IsNullOrWhiteSpace(_sharedLibPath) ||
                    string.IsNullOrWhiteSpace(_stackAddress))
                {
                    MessageBox.Show("请完善缺少的参数", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var addr2linePath = Path.Combine(
                    _ndkPath, "toolchains", "llvm", "prebuilt", "windows-x86_64", "bin",
                    "aarch64-linux-android-addr2line.exe");
                if (!File.Exists(addr2linePath))
                {
                    MessageBox.Show("请检查 NDK 是否完整。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (!string.IsNullOrEmpty(_outputResult))
                {
                    OutputResult = string.Empty;
                }

                var argument = new ArgumentCreator();
                argument.Append("-e")
                    .Append(_sharedLibPath)
                    .Append("-f")
                    .Append("-C")
                    .Append(_stackAddress);
                
                var result = await ExecuteAsync(addr2linePath, argument.ToCommandLine());
                if (result.ExitCode != 0)
                {
                    MessageBox.Show(result.Output, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OutputResult = result.Output;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private Task<(int ExitCode, string Output)> ExecuteAsync(string toolPath, string arguments)
        {
            return Task.Run(() =>
            {
                var lines = new List<string>();
                var executor = new CommandExecutor(arguments);
                executor.OnStandardOutput += lines.Add;
                executor.OnStandardError += lines.Add;

                var exitCode = executor.Execute(toolPath);
                return (exitCode, string.Join(Environment.NewLine, lines));
            });
        }
    }
}
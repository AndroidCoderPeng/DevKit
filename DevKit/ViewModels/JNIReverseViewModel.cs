using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

        private string _ndkVersion = string.Empty;

        public string NdkVersion
        {
            get => _ndkVersion;
            set
            {
                _ndkVersion = value;
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
            var config = SettingsStore.Load<JniReverseConfig>(ConfigSections.FileName, ConfigSections.Jni);
            NdkPath = config.NdkPath;
            SharedLibPath = config.SharedLibPath;
            NdkVersion = $"{GetNdkVersion()} · {GetNdkArchitecture()}";

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

                NdkVersion = $"{GetNdkVersion()} · {GetNdkArchitecture()}";
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

                var path = ResolveAddr2LinePath(_ndkPath);
                if (!File.Exists(path))
                {
                    MessageBox.Show("请检查 NDK 是否完整。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!string.IsNullOrEmpty(_outputResult))
                {
                    OutputResult = string.Empty;
                }

                var argument = new ArgumentCreator();
                if (Path.GetFileName(path).Equals("llvm-addr2line.exe", StringComparison.OrdinalIgnoreCase))
                {
                    // llvm-addr2line 默认 LLVM 风格，会把地址回显在结果开头，强制按 GNU addr2line 风格输出
                    argument.Append("--output-style=GNU");
                }

                argument.Append("-e")
                    .Append(_sharedLibPath)
                    .Append("-f")
                    .Append("-C")
                    .Append(_stackAddress);

                var result = await ExecuteAsync(path, argument.ToCommandLine());
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

        // ---- 私有辅助函数 -----

        private string GetNdkVersion()
        {
            // NDK 根目录的 source.properties 记录了版本号，形如 Pkg.Revision = 21.4.7075529
            try
            {
                var sourceProps = Path.Combine(_ndkPath, "source.properties");
                if (File.Exists(sourceProps))
                {
                    foreach (var line in File.ReadAllLines(sourceProps))
                    {
                        if (line.StartsWith("Pkg.Revision", StringComparison.OrdinalIgnoreCase))
                        {
                            var revision = line.Substring(line.IndexOf('=') + 1).Trim();
                            if (!string.IsNullOrEmpty(revision))
                            {
                                // 输出形如 "r21 (21.4.7075529)"
                                var major = revision.Split('.')[0];
                                return $"r{major} ({revision})";
                            }
                        }
                    }
                }

                // 兜底：从安装目录名解析，形如 android-ndk-r21d
                var dirName = new DirectoryInfo(_ndkPath).Name;
                var match = Regex.Match(dirName, @"r\d+\w*");
                return match.Success ? match.Value : "未知版本";
            }
            catch
            {
                return "未知版本";
            }
        }
        
        private string GetNdkArchitecture()
        {
            // 读取 ELF 头里的 e_machine 字段，识别所选 .so 的目标 ABI
            try
            {
                if (string.IsNullOrWhiteSpace(_sharedLibPath) || !File.Exists(_sharedLibPath))
                {
                    return "未知架构";
                }

                using (var fs = new FileStream(_sharedLibPath, FileMode.Open, FileAccess.Read))
                {
                    var header = new byte[20];
                    if (fs.Read(header, 0, header.Length) < 20)
                    {
                        return "未知架构";
                    }

                    // 校验 ELF 魔数 0x7F 'E' 'L' 'F'
                    if (header[0] != 0x7F || header[1] != 'E' || header[2] != 'L' || header[3] != 'F')
                    {
                        return "非 ELF 文件";
                    }

                    // e_machine 位于偏移 18，小端 2 字节（Android 均为小端）
                    var machine = (ushort)(header[18] | (header[19] << 8));
                    switch (machine)
                    {
                        case 3: return "x86";
                        case 40: return "armeabi-v7a";
                        case 62: return "x86_64";
                        case 183: return "arm64-v8a";
                        default: return $"unknown (0x{machine:X})";
                    }
                }
            }
            catch
            {
                return "未知架构";
            }
        }

        private string ResolveAddr2LinePath(string ndkPath)
        {
            var binDir = Path.Combine(ndkPath, "toolchains", "llvm", "prebuilt", "windows-x86_64", "bin");

            // r16~r22：带 triple 前缀的 GNU binutils，输出干净、兼容老用法
            var prefixed = new[]
            {
                "aarch64-linux-android-addr2line.exe",
                "arm-linux-androideabi-addr2line.exe",
                "x86_64-linux-android-addr2line.exe",
                "i686-linux-android-addr2line.exe"
            };
            foreach (var name in prefixed)
            {
                var fullPath = Path.Combine(binDir, name);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // r23+：前缀版被删除，只剩 llvm-addr2line（LLVM 12+，支持 --output-style=GNU）
            var llvmPath = Path.Combine(binDir, "llvm-addr2line.exe");
            if (File.Exists(llvmPath))
            {
                return llvmPath;
            }

            // 旧版 NDK（r16~r18）GCC 工具链在独立目录下
            var legacyTriplets = new[] { "aarch64-linux-android-4.9", "arm-linux-androideabi-4.9" };
            foreach (var triplet in legacyTriplets)
            {
                var legacyBin = Path.Combine(ndkPath, "toolchains", triplet, "prebuilt", "windows-x86_64", "bin");
                foreach (var name in new[]
                             { "aarch64-linux-android-addr2line.exe", "arm-linux-androideabi-addr2line.exe" })
                {
                    var fullPath = Path.Combine(legacyBin, name);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return null;
        }
    }
}
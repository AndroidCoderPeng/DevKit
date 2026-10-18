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
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Utils;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public partial class AndroidDebugBridgeViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private string _currentDevice = "未连接任何设备";
        [ObservableProperty] private string _deviceBrand;
        [ObservableProperty] private string _deviceModel;
        [ObservableProperty] private string _connectionType;
        [ObservableProperty] private string _androidVersion;
        [ObservableProperty] private string _apiCode;
        [ObservableProperty] private string _cpuType;
        [ObservableProperty] private string _batteryCapacity;
        [ObservableProperty] private string _deviceSize;
        [ObservableProperty] private string _deviceDpi;
        [ObservableProperty] private string _deviceAbi;
        [ObservableProperty] private string _androidId;
        [ObservableProperty] private string _deviceIp;
        [ObservableProperty] private string _batteryState;
        [ObservableProperty] private double _batteryProgress;
        [ObservableProperty] private string _batteryTemperature;
        [ObservableProperty] private string _adbVision = "adb";
        [ObservableProperty] private string _keyword = string.Empty;
        [ObservableProperty] private string _toastMessage;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private bool _canInstall = true;
        [ObservableProperty] private bool _isInstalling;
        [ObservableProperty] private Visibility _exportProgressVisibility = Visibility.Hidden;
        [ObservableProperty] private double _exportProgress;
        [ObservableProperty] private ObservableCollection<string> _applicationPackages = [];

        partial void OnKeywordChanged(string value)
        {
            ApplyFilter(value);
        }

        #endregion

        private static readonly Regex InetRegex = new(@"inet\s+(\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.Compiled);
        private static readonly Regex WifiRegex = new(@"^\d{1,3}(?:\.\d{1,3}){3}:\d+$", RegexOptions.Compiled);

        private readonly IDialogService _dialogService;
        private volatile bool _deviceLoaded;
        private DispatcherTimer _toastTimer;
        private long _chargeCounterUah;
        private bool _isAscending;
        private string _selectedPackage = string.Empty;

        public AndroidDebugBridgeViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            // 获取 adb 版本号
            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                var executor = new CommandExecutor(argument.Append("version").ToCommandLine());
                executor.OnStandardOutput += delegate(string value)
                {
                    var match = Regex.Match(value, @"version\s+([\d.]+)");
                    if (match.Success)
                    {
                        AdbVision = $"adb {match.Groups[1].Value}";
                    }
                };
                executor.Execute("adb");
            });

            // 获取已连接的设备
            LoadConnectedDevice();
        }

        /// <summary>
        /// 加载已连接的设备
        /// </summary>
        private void LoadConnectedDevice()
        {
            InitValueBinding();
            _deviceLoaded = false;

            var argument = new ArgumentCreator();
            argument.Append("devices");
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += delegate(string value)
            {
                if (string.IsNullOrEmpty(value) || value.Equals("List of devices attached"))
                {
                    return;
                }

                var newLine = Regex.Replace(value, @"\s", "*");
                var split = newLine.Split(["*"], StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 0) return;

                // 只处理第一台有效设备
                if (_deviceLoaded) return;
                _deviceLoaded = true;

                var serial = split[0];
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentDevice = serial;
                    ConnectionType = GetConnectionType(serial);

                    // 获取设备应用列表
                    GetDeviceApplication();

                    // 获取设备详情
                    _ = LoadDeviceDetails();
                }));
            };
            Task.Run(() => { executor.Execute("adb"); });
        }

        private void InitValueBinding()
        {
            // 清空设备相关绑定，避免切换/刷新设备时残留上一台设备的数据
            CurrentDevice = "未连接任何设备";
            DeviceBrand = string.Empty;
            DeviceModel = string.Empty;
            ConnectionType = string.Empty;
            AndroidVersion = string.Empty;
            ApiCode = string.Empty;
            CpuType = string.Empty;
            BatteryCapacity = string.Empty;
            DeviceSize = string.Empty;
            DeviceDpi = string.Empty;
            DeviceAbi = string.Empty;
            AndroidId = string.Empty;
            DeviceIp = string.Empty;
            BatteryState = string.Empty;
            BatteryProgress = 0;
            BatteryTemperature = string.Empty;

            ApplicationPackages.Clear();
        }

        private void GetDeviceApplication()
        {
            if (ApplicationPackages.Any())
            {
                ApplicationPackages.Clear();
            }

            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //列出第三方的应用
                //adb shell pm list package -3
                var cmdStr = argument.Append("-s").Append(CurrentDevice).Append("shell")
                    .Append("pm")
                    .Append("list")
                    .Append("package")
                    .Append("-3")
                    .ToCommandLine();
                var executor = new CommandExecutor(cmdStr);
                executor.OnStandardOutput += delegate(string value)
                {
                    var package = value.Split([":"], StringSplitOptions.None)[1];
                    if (!ApplicationPackages.Contains(package))
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ApplicationPackages.Add(package);
                        }));
                    }
                };
                executor.Execute("adb");
            });
        }

        private async Task LoadDeviceDetails()
        {
            // 记录本次选中的设备，用于竞态守卫
            var device = CurrentDevice;

            try
            {
                await Task.Run(() =>
                {
                    // 期间已切换设备，丢弃本次
                    if (device != CurrentDevice) return;

                    // 品牌
                    RunCommand(["-s", device, "shell", "getprop", "ro.product.brand"],
                        v => DeviceBrand = v.Trim());

                    // 型号
                    RunCommand(["-s", device, "shell", "getprop", "ro.product.model"],
                        v => DeviceModel = v.Trim());

                    // Android版本
                    RunCommand(["-s", device, "shell", "getprop", "ro.build.version.release"],
                        v => AndroidVersion = v.Trim());

                    // Android API Level
                    RunCommand(["-s", device, "shell", "getprop", "ro.build.version.sdk"],
                        v => ApiCode = v.Trim());

                    // CPU型号
                    RunCommand(["-s", device, "shell", "getprop", "ro.board.platform"], v =>
                    {
                        var platform = v.Trim();
                        if (!string.IsNullOrEmpty(platform))
                        {
                            CpuType = CpuPlatformMap.GetValueOrDefault(platform, platform);
                        }
                    });

                    // 分辨率
                    RunCommand(["-s", device, "shell", "wm", "size"], v =>
                    {
                        var parts = v.Split(':');
                        if (parts.Length > 1) DeviceSize = parts[1].Trim();
                    });

                    // dpi
                    RunCommand(["-s", device, "shell", "wm", "density"], v =>
                    {
                        var parts = v.Split(':');
                        if (parts.Length > 1) DeviceDpi = parts[1].Trim();
                    });

                    // ABI
                    RunCommand(["-s", device, "shell", "getprop", "ro.product.cpu.abilist"],
                        v => DeviceAbi = v.Trim());

                    // Android ID
                    RunCommand(["-s", device, "shell", "settings", "get", "secure", "android_id"],
                        v => AndroidId = v.Trim());

                    // IP
                    RunCommand(["-s", device, "shell", "ip", "addr", "show", "wlan0"], v =>
                    {
                        if (v.Contains("error"))
                        {
                            DeviceIp = "无法获取IP";
                            return;
                        }

                        var match = InetRegex.Match(v);
                        if (match.Success) DeviceIp = match.Groups[1].Value;
                    });

                    // 电池
                    RunCommand(["-s", device, "shell", "dumpsys", "battery"], ParseBatteryLine);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [RelayCommand]
        private void RefreshDevice()
        {
            LoadConnectedDevice();
        }

        [RelayCommand]
        private void AndroidIdLabelClick(string text)
        {
            CopyToClipboard(text);
        }

        private void CopyToClipboard(string text)
        {
            var dataObject = new DataObject(DataFormats.UnicodeText, text);
            Clipboard.SetDataObject(dataObject);

            ShowToast("参数已复制");
        }

        [RelayCommand]
        private void DeviceIpLabelClick(string text)
        {
            CopyToClipboard(text);
        }

        [RelayCommand]
        private void OutputImage()
        {
            if (CurrentDevice == "未连接任何设备")
            {
                ShowToast("请先刷新并连接设备");
                return;
            }

            // TODO: 2024-06-10 17:00:00 暂时屏蔽截图导出功能，后续改
            // var dialogParameters = new DialogParameters
            // {
            //     { "device", CurrentDevice }
            // };
            //
            // _dialogService.ShowDialog("ScreenshotExportDialog", dialogParameters, _ => { });
        }

        [RelayCommand]
        private void Screenshot()
        {
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
            var argument = new ArgumentCreator();
            var cmdStr = argument.Append("-s").Append(CurrentDevice)
                .Append("shell")
                .Append("screencap")
                .Append("-p")
                .Append($"/sdcard/{fileName}")
                .ToCommandLine();

            new CommandExecutor(cmdStr).Execute("adb");
            ShowToast("屏幕截取成功");
        }

        [RelayCommand]
        private void ShowLogcat()
        {
            if (CurrentDevice == "未连接任何设备")
            {
                ShowToast("请先刷新并连接设备");
                return;
            }

            // TODO: 2024-06-10 17:00:00 暂时屏蔽日志查看功能，后续改
            // var dialogParameters = new DialogParameters
            // {
            //     { "device", CurrentDevice }
            // };
            //
            // _dialogService.ShowDialog("AndroidLogcatDialog", dialogParameters, _ => { });
        }

        [RelayCommand]
        private Task Install()
        {
            return InstallApplicationAsync();
        }

        private async Task InstallApplicationAsync()
        {
            if (CurrentDevice == "未连接任何设备")
            {
                ShowToast("请先刷新并连接设备");
                return;
            }

            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".apk",
                Filter = "安装包文件(*.apk)|*.apk"
            };
            if (fileDialog.ShowDialog() != true) return;

            var filePath = fileDialog.FileName;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("安装包路径错误，请重新选择");
                return;
            }

            if (!CanInstall) return;
            CanInstall = false;
            IsInstalling = true;

            try
            {
                await Task.Run(() =>
                {
                    var argument = new ArgumentCreator();
                    // adb -s <设备序列号> install -r <apk路径>
                    var cmdStr = argument.Append("-s").Append(CurrentDevice)
                        .Append("install")
                        .Append("-r")
                        .Append(filePath)
                        .ToCommandLine();

                    var executor = new CommandExecutor(cmdStr);
                    string lastLine = null;
                    executor.OnStandardOutput += line => lastLine = line;
                    executor.OnStandardError += line => lastLine = line;

                    var exitCode = executor.Execute("adb");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsInstalling = false;

                        var success = exitCode == 0 &&
                                      lastLine != null &&
                                      lastLine.Trim().StartsWith("Success", StringComparison.OrdinalIgnoreCase);

                        if (success)
                        {
                            MessageBox.Show("安装成功", "安装应用", MessageBoxButton.OK, MessageBoxImage.Information);
                            GetDeviceApplication();
                        }
                        else
                        {
                            MessageBox.Show(lastLine ?? "安装失败，请检查设备连接或 apk 包", "安装应用", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsInstalling = false;
                    MessageBox.Show($"安装失败：{e.Message}", "安装应用", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                CanInstall = true;
            }
        }

        [RelayCommand]
        private void RebootDevice()
        {
            var result = MessageBox.Show("确定重启该设备？", "重启设备", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
            {
                return;
            }

            var argument = new ArgumentCreator();
            argument.Append("-s").Append(CurrentDevice).Append("reboot");
            new CommandExecutor(argument.ToCommandLine()).Execute("adb");
        }

        [RelayCommand]
        private void ShutdownDevice()
        {
            var result = MessageBox.Show("确定关闭该设备？", "关机", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
            {
                return;
            }

            var argument = new ArgumentCreator();
            var cmdStr = argument.Append("-s").Append(CurrentDevice)
                .Append("shell")
                .Append("reboot")
                .Append("-p")
                .ToCommandLine();

            new CommandExecutor(cmdStr).Execute("adb");
        }

        [RelayCommand]
        private void SortApplication()
        {
            _isAscending = !_isAscending;

            var sorted = _isAscending
                ? ApplicationPackages.OrderBy(x => x, StringComparer.Ordinal)
                : ApplicationPackages.OrderByDescending(x => x, StringComparer.Ordinal);

            ApplicationPackages = [.. sorted];
        }

        [RelayCommand]
        private void PackageSelected(string item)
        {
            _selectedPackage = item;
        }

        [RelayCommand]
        private Task ExportPackage()
        {
            return ExportPackageAsync();
        }

        private async Task ExportPackageAsync()
        {
            if (string.IsNullOrEmpty(_selectedPackage))
            {
                MessageBox.Show("请先选择需要导出的应用", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ResetExportProgress();

            try
            {
                await Task.Run(async () =>
                {
                    // 获取应用安装路径
                    // adb -s <设备序列号> shell pm path <应用包名>
                    var pathOutput = GetRunCommandOutput("-s", CurrentDevice, "shell", "pm", "path", _selectedPackage);
                    var packagePath = ExtractPackagePath(pathOutput);

                    if (string.IsNullOrEmpty(packagePath))
                    {
                        ShowExportResult("未找到应用的安装路径，请重新选择", false);
                        return;
                    }

                    // 获取远端文件大小
                    // adb -s <设备> shell stat -c %s <路径>
                    var sizeOutput = GetRunCommandOutput("-s", CurrentDevice, "shell", "stat", "-c", "%s", packagePath);
                    long.TryParse(sizeOutput, out var remoteFileSize);

                    var fileName = $"{_selectedPackage}.apk";
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                    ShowExportProgress();

                    if (remoteFileSize <= 0)
                    {
                        // 降级：拿不到大小就同步 pull，不显示进度
                        GetRunCommandOutput("-s", CurrentDevice, "pull", packagePath, filePath);
                        ShowExportResult($"导出完成：{filePath}", true);
                        return;
                    }

                    // 非阻塞启动 pull + 轮询本地文件大小
                    if (File.Exists(filePath)) File.Delete(filePath);

                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(CurrentDevice)
                        .Append("pull").Append(packagePath).Append(filePath)
                        .ToCommandLine();

                    var pullExecutor = new CommandExecutor(cmdStr);
                    using (var process = pullExecutor.StartNonBlocking("adb"))
                    {
                        while (!process.HasExited)
                        {
                            if (File.Exists(filePath))
                            {
                                var info = new FileInfo(filePath);
                                var progress = Math.Min(info.Length * 100.0 / remoteFileSize, 99);
                                UpdateExportProgress(progress);
                            }

                            await Task.Delay(100);
                        }
                    }

                    ShowExportResult($"导出完成：{filePath}", true);
                });
            }
            catch (Exception e)
            {
                ShowExportResult($"导出失败：{e.Message}", false);
            }
            finally
            {
                HideExportProgress();
            }
        }

        [RelayCommand]
        private Task Uninstall()
        {
            return UninstallApplicationAsync();
        }

        private async Task UninstallApplicationAsync()
        {
            if (string.IsNullOrEmpty(_selectedPackage))
            {
                MessageBox.Show("请先选择需要卸载的应用", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CurrentDevice == "未连接任何设备")
            {
                ShowToast("请先刷新并连接设备");
                return;
            }

            // 拷贝局部变量，避免执行期间选中项被切换导致误删
            var package = _selectedPackage;

            var result = MessageBox.Show("确定卸载该应用？", "卸载应用", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            try
            {
                await Task.Run(() =>
                {
                    var output = GetRunCommandOutput("-s", CurrentDevice, "uninstall", package);
                    var success = !string.IsNullOrEmpty(output) &&
                                  output.Trim().StartsWith("Success", StringComparison.OrdinalIgnoreCase);

                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (success)
                        {
                            ApplicationPackages.Remove(package);
                            MessageBox.Show("卸载成功", "卸载应用", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(output ?? "卸载失败，请检查设备连接", "卸载应用", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    MessageBox.Show($"卸载失败：{e.Message}", "卸载应用", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        // ---- 私有辅助函数 -----

        /// <summary>
        /// 执行一条 adb 命令，逐行输出通过回调在 UI 线程处理。
        /// </summary>
        private void RunCommand(string[] args, Action<string> onLine)
        {
            var argument = new ArgumentCreator();
            foreach (var arg in args)
            {
                argument.Append(arg);
            }

            var cmd = argument.ToCommandLine();
            Console.WriteLine(cmd);
            var executor = new CommandExecutor(cmd);
            executor.OnStandardOutput += line =>
            {
                if (string.IsNullOrEmpty(line)) return;
                Application.Current.Dispatcher.BeginInvoke(new Action(() => onLine(line)));
            };
            executor.Execute("adb");
        }

        private void ParseBatteryLine(string line)
        {
            var idx = line.IndexOf(':');
            if (idx < 0) return;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();

            switch (key)
            {
                case "status":
                    BatteryState = value switch
                    {
                        "2" => "正在充电",
                        "5" => "充电完成",
                        _ => "未充电"
                    };

                    break;

                case "level":
                    if (double.TryParse(value, out var level))
                    {
                        BatteryProgress = level;
                        TryCalcBatteryCapacity();
                    }

                    break;

                case "Charge counter":
                    if (long.TryParse(value, out var counter))
                    {
                        _chargeCounterUah = counter;
                        TryCalcBatteryCapacity();
                    }

                    break;

                case "temperature":
                    if (double.TryParse(value, out var temp)) BatteryTemperature = $"{temp * 0.1}℃";
                    break;
            }
        }

        private void TryCalcBatteryCapacity()
        {
            if (_chargeCounterUah <= 0 || !(BatteryProgress > 0)) return;

            // charge_counter 是“当前剩余电量”(μAh)，除以电量百分比反推满充容量
            var fullUah = _chargeCounterUah * 100.0 / BatteryProgress;
            BatteryCapacity = $"{fullUah / 1000.0:F0}mAh";
        }

        private string GetConnectionType(string serial)
        {
            if (WifiRegex.IsMatch(serial)) return "无线连接";
            return serial.StartsWith("emulator-") ? "模拟器" : "有线连接";
        }

        /// <summary>
        /// 平台代号 → 芯片通用名映射（找不到时回退显示原始代号）
        /// </summary>
        private static readonly Dictionary<string, string> CpuPlatformMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // 联发科天玑
                { "mt6983", "天玑 9000" },
                { "mt6985", "天玑 9200" },
                { "mt6989", "天玑 9300" },
                { "mt6991", "天玑 9400" },
                { "mt6897", "天玑 8300" },
                { "mt6896", "天玑 8200" },
                { "mt6895", "天玑 8100" },
                { "mt6893", "天玑 1200" },
                { "mt6891", "天玑 1100" },

                // 高通骁龙
                { "holi", "骁龙 680" },
                { "sm8450", "骁龙 8 Gen 1" },
                { "taro", "骁龙 8 Gen 1" },
                { "sm8475", "骁龙 8+ Gen 1" },
                { "sm8550", "骁龙 8 Gen 2" },
                { "kalama", "骁龙 8 Gen 2" },
                { "sm8650", "骁龙 8 Gen 3" },
            };

        /// <summary>
        /// 按名称包名列表
        /// </summary>
        private void ApplyFilter(string keyword)
        {
            var view = CollectionViewSource.GetDefaultView(ApplicationPackages);

            // 无关键字：移除过滤，避免空谓词无谓遍历
            if (keyword.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                return;
            }

            view.Filter = item =>
                item is string package && package.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            view.Refresh();
        }

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (_, _) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }

        private string GetRunCommandOutput(params string[] args)
        {
            var argument = new ArgumentCreator();
            foreach (var a in args) argument.Append(a);

            var psi = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = argument.ToCommandLine(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 adb 进程");
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


        private void ResetExportProgress()
        {
            ExportProgress = 0;
            ExportProgressVisibility = Visibility.Hidden;
        }

        private void ShowExportProgress()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ExportProgress = 0;
                ExportProgressVisibility = Visibility.Visible;
            }));
        }

        private void UpdateExportProgress(double progress)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { ExportProgress = Math.Round(progress, 0); }));
        }

        private void HideExportProgress()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ExportProgressVisibility = Visibility.Hidden;
            }));
        }

        private void ShowExportResult(string message, bool success)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (success) ExportProgress = 100;
                ExportProgressVisibility = Visibility.Hidden;
                MessageBox.Show(message, "导出应用", MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }));
        }

        private static string ExtractPackagePath(string output)
        {
            if (string.IsNullOrEmpty(output)) return null;

            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                {
                    return line["package:".Length..].Trim();
                }
            }

            return null;
        }
    }
}
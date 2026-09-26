using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevKit.Events;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public class AndroidDebugBridgeViewModel : BindableBase, IDialogAware
    {
        public string Title => "Android Debug Bridge";

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

        #region VM

        private string _currentDevice = "未连接任何设备";

        public string CurrentDevice
        {
            get => _currentDevice;
            set
            {
                _currentDevice = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceBrand;

        public string DeviceBrand
        {
            get => _deviceBrand;
            set
            {
                _deviceBrand = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceModel;

        public string DeviceModel
        {
            get => _deviceModel;
            set
            {
                _deviceModel = value;
                RaisePropertyChanged();
            }
        }

        private string _connectionType;

        public string ConnectionType
        {
            get => _connectionType;
            set
            {
                _connectionType = value;
                RaisePropertyChanged();
            }
        }

        private string _androidVersion;

        public string AndroidVersion
        {
            get => _androidVersion;
            set
            {
                _androidVersion = value;
                RaisePropertyChanged();
            }
        }

        private string _apiCode;

        public string ApiCode
        {
            get => _apiCode;
            set
            {
                _apiCode = value;
                RaisePropertyChanged();
            }
        }

        private string _cpuType;

        public string CpuType
        {
            get => _cpuType;
            set
            {
                _cpuType = value;
                RaisePropertyChanged();
            }
        }

        private string _batteryCapacity;

        public string BatteryCapacity
        {
            get => _batteryCapacity;
            set
            {
                _batteryCapacity = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceSize;

        public string DeviceSize
        {
            get => _deviceSize;
            set
            {
                _deviceSize = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceDpi;

        public string DeviceDpi
        {
            get => _deviceDpi;
            set
            {
                _deviceDpi = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceAbi;

        public string DeviceAbi
        {
            get => _deviceAbi;
            set
            {
                _deviceAbi = value;
                RaisePropertyChanged();
            }
        }

        private string _androidId;

        public string AndroidId
        {
            get => _androidId;
            set
            {
                _androidId = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceIp;

        public string DeviceIp
        {
            get => _deviceIp;
            set
            {
                _deviceIp = value;
                RaisePropertyChanged();
            }
        }

        private string _batteryState;

        public string BatteryState
        {
            get => _batteryState;
            set
            {
                _batteryState = value;
                RaisePropertyChanged();
            }
        }

        private double _batteryProgress;

        public double BatteryProgress
        {
            get => _batteryProgress;
            set
            {
                _batteryProgress = value;
                RaisePropertyChanged();
            }
        }

        private string _batteryTemperature;

        public string BatteryTemperature
        {
            get => _batteryTemperature;
            set
            {
                _batteryTemperature = value;
                RaisePropertyChanged();
            }
        }

        private string _adbVision = "adb";

        public string AdbVision
        {
            get => _adbVision;
            set
            {
                _adbVision = value;
                RaisePropertyChanged();
            }
        }

        private string _toastMessage;

        public string ToastMessage
        {
            get => _toastMessage;
            set
            {
                _toastMessage = value;
                RaisePropertyChanged();
            }
        }

        private bool _isToastVisible;

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set
            {
                _isToastVisible = value;
                RaisePropertyChanged();
            }
        }

        // ----------- ***** -----------

        private Visibility _exportProgressVisibility = Visibility.Hidden;

        public Visibility ExportProgressVisibility
        {
            get => _exportProgressVisibility;
            set
            {
                _exportProgressVisibility = value;
                RaisePropertyChanged();
            }
        }

        private double _exportProgress;

        public double ExportProgress
        {
            get => _exportProgress;
            set
            {
                _exportProgress = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _applicationPackages = new ObservableCollection<string>();

        public ObservableCollection<string> ApplicationPackages
        {
            get => _applicationPackages;
            set
            {
                _applicationPackages = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand RefreshDeviceCommand { set; get; }
        public DelegateCommand<string> AndroidIdLabelClickCommand { set; get; }
        public DelegateCommand<string> DeviceIpLabelClickCommand { set; get; }
        public DelegateCommand OutputImageCommand { set; get; }
        public DelegateCommand ScreenshotCommand { set; get; }
        public DelegateCommand InstallCommand { set; get; }
        public DelegateCommand RebootDeviceCommand { set; get; }
        public DelegateCommand ShutdownDeviceCommand { set; get; }
        public DelegateCommand RefreshApplicationCommand { set; get; }
        public DelegateCommand SortApplicationCommand { set; get; }
        public DelegateCommand<string> PackageSelectedCommand { set; get; }
        public DelegateCommand ExportPackageCommand { set; get; }
        public DelegateCommand UninstallCommand { set; get; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private static readonly Regex InetRegex = new Regex(@"inet\s+(\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.Compiled);
        private static readonly Regex WifiRegex = new Regex(@"^\d{1,3}(?:\.\d{1,3}){3}:\d+$", RegexOptions.Compiled);
        private volatile bool _deviceLoaded;
        private DispatcherTimer _toastTimer;
        private long _chargeCounterUah;

        private string _selectedPackage = string.Empty;
        private bool _isAscending;

        public AndroidDebugBridgeViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            RefreshDeviceCommand = new DelegateCommand(LoadConnectedDevice);
            AndroidIdLabelClickCommand = new DelegateCommand<string>(CopyToClipboard);
            DeviceIpLabelClickCommand = new DelegateCommand<string>(CopyToClipboard);
            OutputImageCommand = new DelegateCommand(ExportScreenshot);

            // ScreenshotCommand = new DelegateCommand(TakeScreenshot);
            // InstallCommand = new DelegateCommand(InstallApplication);
            // RebootDeviceCommand = new DelegateCommand(RebootDevice);
            // ShutdownDeviceCommand = new DelegateCommand(ShutdownDevice);
            // RefreshApplicationCommand = new DelegateCommand(RefreshApplication);
            // SortApplicationCommand = new DelegateCommand(SortApplication);
            // PackageSelectedCommand = new DelegateCommand<string>(PackageSelected);
            // ExportPackageCommand = new DelegateCommand(ExportPackage);
            // UninstallCommand = new DelegateCommand(UninstallApplication);
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
                var split = newLine.Split(new[] { "*" }, StringSplitOptions.RemoveEmptyEntries);
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
                    LoadDeviceDetails();
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
            if (_applicationPackages.Any())
            {
                ApplicationPackages.Clear();
            }

            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //列出第三方的应用
                //adb shell pm list package -3
                var cmdStr = argument.Append("-s").Append(_currentDevice).Append("shell")
                    .Append("pm")
                    .Append("list")
                    .Append("package")
                    .Append("-3")
                    .ToCommandLine();
                var executor = new CommandExecutor(cmdStr);
                executor.OnStandardOutput += delegate(string value)
                {
                    var package = value.Split(new[] { ":" }, StringSplitOptions.None)[1];
                    if (!_applicationPackages.Contains(package))
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

        private async void LoadDeviceDetails()
        {
            // 记录本次选中的设备，用于竞态守卫
            var device = _currentDevice;

            try
            {
                await Task.Run(() =>
                {
                    // 期间已切换设备，丢弃本次
                    if (device != _currentDevice) return;

                    // 品牌
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.product.brand" },
                        v => DeviceBrand = v.Trim());

                    // 型号
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.product.model" },
                        v => DeviceModel = v.Trim());

                    // Android版本
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.build.version.release" },
                        v => AndroidVersion = v.Trim());

                    // Android API Level
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.build.version.sdk" },
                        v => ApiCode = v.Trim());

                    // CPU型号
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.board.platform" },
                        v =>
                        {
                            var platform = v.Trim();
                            if (!string.IsNullOrEmpty(platform))
                            {
                                CpuType = CpuPlatformMap.TryGetValue(platform, out var name) ? name : platform;
                            }
                        });

                    // 分辨率
                    RunCommand(new[] { "-s", device, "shell", "wm", "size" },
                        v =>
                        {
                            var parts = v.Split(':');
                            if (parts.Length > 1) DeviceSize = parts[1].Trim();
                        });

                    // dpi
                    RunCommand(new[] { "-s", device, "shell", "wm", "density" }, v =>
                    {
                        var parts = v.Split(':');
                        if (parts.Length > 1) DeviceDpi = parts[1].Trim();
                    });

                    // ABI
                    RunCommand(new[] { "-s", device, "shell", "getprop", "ro.product.cpu.abilist" },
                        v => DeviceAbi = v.Trim());

                    // Android ID
                    RunCommand(new[] { "-s", device, "shell", "settings", "get", "secure", "android_id" },
                        v => AndroidId = v.Trim());

                    // IP
                    RunCommand(new[] { "-s", device, "shell", "ip", "addr", "show", "wlan0" },
                        v =>
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
                    RunCommand(new[] { "-s", device, "shell", "dumpsys", "battery" }, ParseBatteryLine);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void CopyToClipboard(string text)
        {
            var dataObject = new DataObject(DataFormats.UnicodeText, text);
            Clipboard.SetDataObject(dataObject);

            ShowToast("参数已复制");
        }

        private void ExportScreenshot()
        {
            if (CurrentDevice == "未连接任何设备")
            {
                ShowToast("请先刷新并连接设备");
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "device", _currentDevice }
            };

            _dialogService.ShowDialog("ScreenshotExportDialog", dialogParameters, _ => { });
        }

        //////////////////////////////////////////////////////

        private void TakeScreenshot()
        {
            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //截取屏幕截图并保存到指定位置
                //adb shell screencap -p /sdcard/20241214112123.png 
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
                var cmdStr = argument.Append("-s").Append(_currentDevice).Append("shell")
                    .Append("screencap")
                    .Append("-p")
                    .Append($"/sdcard/{fileName}")
                    .ToCommandLine();
                new CommandExecutor(cmdStr).Execute("adb");
                ExportScreenshot();
            });
        }

        private void RebootDevice()
        {
            var result = MessageBox.Show("确定重启该设备？", "重启设备", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                var argument = new ArgumentCreator();
                //重启设备
                //adb reboot 
                argument.Append("-s").Append(_currentDevice).Append("reboot");
                new CommandExecutor(argument.ToCommandLine()).Execute("adb");
            }
        }

        private void ShutdownDevice()
        {
            var result = MessageBox.Show("确定关闭该设备？", "关机", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                var argument = new ArgumentCreator();
                //关机
                //adb shell reboot -p 
                var cmdStr = argument.Append("-s").Append(_currentDevice)
                    .Append("shell")
                    .Append("reboot")
                    .Append("-p")
                    .ToCommandLine();
                new CommandExecutor(cmdStr).Execute("adb");
            }
        }

        private void InstallApplication()
        {
            var fileDialog = new OpenFileDialog
            {
                // 设置默认格式
                DefaultExt = ".apk",
                Filter = "安装包文件(*.apk)|*.apk"
            };
            var result = fileDialog.ShowDialog();
            if (result != true) return;
            var filePath = fileDialog.FileName;
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("安装包路径错误，请重新选择");
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "LoadingMessage", "软件安装中，请稍后......" }
            };
            _dialogService.Show("LoadingDialog", dialogParameters, delegate { });
            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //覆盖安装应用（apk）
                //adb -s <设备序列号> install  -r 
                var cmdStr = argument.Append("-s").Append(_currentDevice)
                    .Append("install")
                    .Append("-r")
                    .Append(filePath)
                    .ToCommandLine();
                var executor = new CommandExecutor(cmdStr);
                executor.OnStandardOutput += delegate(string value)
                {
                    if (value.Equals("Success"))
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
                            MessageBox.Show(value, "安装应用", MessageBoxButton.OK, MessageBoxImage.Information);
                            GetDeviceApplication();
                        });
                    }
                };
                executor.Execute("adb");
            });
        }

        private void RefreshApplication()
        {
            GetDeviceApplication();
        }

        private void SortApplication()
        {
            var list = _applicationPackages.ToList();

            if (_isAscending)
            {
                list.Sort((x, y) => Comparer<string>.Default.Compare(y, x));
                _isAscending = false;
            }
            else
            {
                list.Sort();
                _isAscending = true;
            }

            ApplicationPackages.Clear();
            foreach (var item in list)
            {
                ApplicationPackages.Add(item);
            }
        }

        private void PackageSelected(string package)
        {
            _selectedPackage = package;
        }

        private async void ExportPackage()
        {
            if (string.IsNullOrEmpty(_selectedPackage))
            {
                MessageBox.Show("请先选择需要导出的应用", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await Task.Run(async () =>
            {
                // Step 1: 获取应用安装路径
                // adb -s <设备序列号> shell pm path <应用包名>
                string packagePath = null;
                {
                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(_currentDevice).Append("shell")
                        .Append("pm")
                        .Append("path")
                        .Append(_selectedPackage)
                        .ToCommandLine();
                    var pathExecutor = new CommandExecutor(cmdStr);
                    pathExecutor.OnStandardOutput += delegate(string value)
                    {
                        if (value.Contains("package:"))
                        {
                            packagePath = value.Replace("package:", "").Trim();
                        }
                    };
                    pathExecutor.Execute("adb");
                }

                if (string.IsNullOrEmpty(packagePath))
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        ExportProgressVisibility = Visibility.Hidden;
                        MessageBox.Show("未找到应用的安装路径，请重新选择", "导出应用",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                // Step 2: 获取远端文件大小
                // adb -s <设备> shell stat -c %s <路径>
                long remoteFileSize = 0;
                {
                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(_currentDevice).Append("shell")
                        .Append("stat")
                        .Append("-c")
                        .Append("%s")
                        .Append(packagePath)
                        .ToCommandLine();
                    var sizeExecutor = new CommandExecutor(cmdStr);
                    sizeExecutor.OnStandardOutput += delegate(string value)
                    {
                        long.TryParse(value.Trim(), out remoteFileSize);
                    };
                    sizeExecutor.Execute("adb");
                }

                var fileName = $"{_selectedPackage}.apk";
                var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\{fileName}";

                Application.Current.Dispatcher.Invoke(delegate { ExportProgressVisibility = Visibility.Visible; });

                {
                    if (remoteFileSize <= 0)
                    {
                        // 降级：不显示进度的同步 pull
                        var argument = new ArgumentCreator();
                        var cmdStr = argument.Append("-s").Append(_currentDevice)
                            .Append("pull")
                            .Append(packagePath)
                            .Append(filePath)
                            .ToCommandLine();
                        new CommandExecutor(cmdStr).Execute("adb");

                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            ExportProgress = 100;
                            ExportProgressVisibility = Visibility.Hidden;
                            MessageBox.Show($"导出完成：{filePath}", "导出应用",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                        return;
                    }
                }

                // Step 3: 非阻塞启动 pull + 轮询本地文件大小
                if (File.Exists(filePath)) File.Delete(filePath);

                {
                    var argument = new ArgumentCreator();
                    var cmdStr = argument.Append("-s").Append(_currentDevice)
                        .Append("pull")
                        .Append(packagePath)
                        .Append(filePath)
                        .ToCommandLine();
                    var pullExecutor = new CommandExecutor(cmdStr);
                    var process = pullExecutor.StartNonBlocking("adb");

                    // Step 4: 每 100ms 轮询本地文件大小，计算进度
                    while (!process.HasExited)
                    {
                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            var progress = Math.Min((double)fileInfo.Length / remoteFileSize * 100, 99);
                            Application.Current.Dispatcher.Invoke(() => { ExportProgress = progress; });
                        }

                        await Task.Delay(100);
                    }

                    process.Close();
                }

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ExportProgress = 100;
                    ExportProgressVisibility = Visibility.Hidden;
                    MessageBox.Show($"导出完成：{filePath}", "导出应用",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });
        }

        private void UninstallApplication()
        {
            if (string.IsNullOrEmpty(_selectedPackage))
            {
                MessageBox.Show("请先选择需要卸载的应用", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("确定卸载该应用？", "卸载应用", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                Task.Run(() =>
                {
                    var argument = new ArgumentCreator();
                    //卸载应用（应用包名）
                    //adb -s <设备序列号> uninstall 
                    var cmdStr = argument.Append("-s").Append(_currentDevice)
                        .Append("uninstall")
                        .Append(_selectedPackage)
                        .ToCommandLine();
                    var executor = new CommandExecutor(cmdStr);
                    executor.OnStandardOutput += delegate(string value)
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            ApplicationPackages.Remove(_selectedPackage);
                            MessageBox.Show(value, "卸载应用", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    };
                    executor.Execute("adb");
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

            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();

            switch (key)
            {
                case "status":
                    switch (value)
                    {
                        case "2": BatteryState = "正在充电"; break;
                        case "5": BatteryState = "充电完成"; break;
                        default: BatteryState = "未充电"; break;
                    }

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
            if (_chargeCounterUah > 0 && BatteryProgress > 0)
            {
                // charge_counter 是“当前剩余电量”(μAh)，除以电量百分比反推满充容量
                var fullUah = _chargeCounterUah * 100.0 / BatteryProgress;
                BatteryCapacity = $"{fullUah / 1000.0:F0}mAh";
            }
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
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }
}
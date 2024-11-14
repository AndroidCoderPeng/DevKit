using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using DevKit.Events;
using DevKit.Utils;
using HandyControl.Controls;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public class AndroidDebugBridgeViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<string> _deviceItems = new ObservableCollection<string>();

        public ObservableCollection<string> DeviceItems
        {
            get => _deviceItems;
            set
            {
                _deviceItems = value;
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

        private string _deviceDensity;

        public string DeviceDensity
        {
            get => _deviceDensity;
            set
            {
                _deviceDensity = value;
                RaisePropertyChanged();
            }
        }

        private double _memoryProgress;

        public double MemoryProgress
        {
            get => _memoryProgress;
            set
            {
                _memoryProgress = value;
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
        public DelegateCommand<string> DeviceSelectedCommand { set; get; }
        public DelegateCommand SortApplicationCommand { set; get; }
        public DelegateCommand<string> PackageSelectedCommand { set; get; }
        public DelegateCommand RebootDeviceCommand { set; get; }
        public DelegateCommand ScreenshotCommand { set; get; }
        public DelegateCommand UninstallCommand { set; get; }
        public DelegateCommand InstallCommand { set; get; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private string _selectedDevice = string.Empty;
        private string _selectedPackage = string.Empty;
        private bool _isAscending;

        /// <summary>
        /// DispatcherTimer与窗体为同一个线程，故如果频繁的执行DispatcherTimer的话，会造成主线程的卡顿。
        /// 用System.Timers.Timer来初始化一个异步的时钟，初始化一个时钟的事件，在时钟的事件中采用BeginInvoke来进行异步委托。
        /// 这样就能防止timer控件的同步事件不停的刷新时，界面的卡顿
        /// </summary>
        private readonly Timer _refreshDeviceTimer = new Timer(1000);

        public AndroidDebugBridgeViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            //定时刷新设备列表，可能会为空，因为开发者模式可能没开
            _refreshDeviceTimer.Elapsed += TimerElapsedEvent_Handler;
            _refreshDeviceTimer.Enabled = true;

            RefreshDeviceCommand = new DelegateCommand(RefreshDevice);
            DeviceSelectedCommand = new DelegateCommand<string>(DeviceSelected);
            SortApplicationCommand = new DelegateCommand(SortApplication);
            PackageSelectedCommand = new DelegateCommand<string>(PackageSelected);
            RebootDeviceCommand = new DelegateCommand(RebootDevice);
            ScreenshotCommand = new DelegateCommand(TakeScreenshot);
            UninstallCommand = new DelegateCommand(UninstallApplication);
            InstallCommand = new DelegateCommand(InstallApplication);
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (!_deviceItems.Any())
            {
                RefreshDevice();
            }
            else
            {
                Console.WriteLine(@"已获取设备列表，停止自动刷新");
                _refreshDeviceTimer.Enabled = false;
            }
        }

        /// <summary>
        /// 刷新设备列表
        /// </summary>
        /// <returns></returns>
        private void RefreshDevice()
        {
            var argument = new ArgumentCreator();
            argument.Append("devices");
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += StandardOutput_EventHandler;
            executor.Execute("adb");
        }

        private void StandardOutput_EventHandler(string output)
        {
            if (string.IsNullOrEmpty(output) || output.Equals("List of devices attached"))
            {
                return;
            }

            var newLine = Regex.Replace(output, @"\s", "*");
            var split = newLine.Split(new[] { "*" }, StringSplitOptions.RemoveEmptyEntries);
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (_deviceItems.Contains(split[0]))
                {
                    return;
                }

                DeviceItems.Add(split[0]);
            });
        }

        private async void DeviceSelected(string device)
        {
            _selectedDevice = device;
            //获取设备详情
            await Task.Run(GetDeviceDetail);

            //获取第三方应用列表
            await Task.Run(GetDeviceApplication);
        }

        private void GetDeviceDetail()
        {
            {
                var argument = new ArgumentCreator();
                //查看 android id
                //adb shell settings get secure android_id 
                argument.Append("-s").Append(_selectedDevice)
                    .Append("shell").Append("settings").Append("get").Append("secure").Append("android_id");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value) { AndroidId = value; };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取设备型号
                //adb shell getprop ro.product.model
                argument.Append("-s").Append(_selectedDevice)
                    .Append("shell").Append("getprop").Append("ro.product.model");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value) { DeviceModel = value; };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取设备品牌
                //adb shell getprop ro.product.brand
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("getprop")
                    .Append("ro.product.brand");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value) { DeviceBrand = value; };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取CPU支持的abi架构列表
                //adb shell getprop ro.product.cpu.abilist
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("getprop")
                    .Append("ro.product.cpu.abilist");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value) { DeviceAbi = value; };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取设备Android系统版本
                //adb shell getprop ro.build.version.release
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("getprop")
                    .Append("ro.build.version.release");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value) { AndroidVersion = value; };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取设备屏幕分辨率
                //adb shell wm size
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("wm").Append("size");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value)
                {
                    //Physical size: 1240x2772
                    DeviceSize = value.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取设备屏幕密度
                //adb shell wm density
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("wm").Append("density");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value)
                {
                    //Physical density: 560
                    DeviceDensity = $"{value.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim()}";
                };
                executor.Execute("adb");
            }

            {
                var argument = new ArgumentCreator();
                //获取手机内存信息
                //adb shell cat /proc/meminfo
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("cat").Append("/proc/meminfo");
                var executor = new CommandExecutor(argument.ToCommandLine());
                var available = 0.0;
                var total = 0.0;
                executor.OnStandardOutput += delegate(string value)
                {
                    var dictionary = value.ToDictionary();
                    foreach (var kvp in dictionary)
                    {
                        switch (kvp.Key)
                        {
                            case "MemAvailable":
                                available = kvp.Value.FormatMemoryValue();
                                break;

                            case "MemTotal":
                                //进一取整
                                total = Math.Ceiling(kvp.Value.FormatMemoryValue());
                                break;
                        }
                    }
                };
                executor.Execute("adb");
                if (total == 0)
                {
                    MemoryProgress = 0;
                }
                else
                {
                    MemoryProgress = Math.Round((total - available) / total, 2) * 100;
                }
            }

            {
                var argument = new ArgumentCreator();
                //监控电池信息
                //adb shell dumpsys battery
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("dumpsys").Append("battery");
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value)
                {
                    var dictionary = value.ToDictionary();
                    foreach (var kvp in dictionary)
                    {
                        switch (kvp.Key)
                        {
                            case "status":
                                // 2:正充电；3：没插充电器；4：不充电； 5：电池充满
                                switch (kvp.Value)
                                {
                                    case "2":
                                        BatteryState = "正在充电";
                                        break;

                                    case "5":
                                        BatteryState = "充电完成";
                                        break;

                                    default:
                                        BatteryState = "未充电";
                                        break;
                                }

                                break;
                            case "level":
                                BatteryProgress = double.Parse(kvp.Value);
                                break;

                            case "temperature":
                                var temperature = int.Parse(kvp.Value) * 0.1;
                                BatteryTemperature = $"{temperature}℃";
                                break;
                        }
                    }
                };
                executor.Execute("adb");
            }
        }

        private void GetDeviceApplication()
        {
            var argument = new ArgumentCreator();
            //列出第三方的应用
            //adb shell pm list package -3
            argument.Append("shell").Append("pm").Append("list").Append("package").Append("-3");
            var executor = new CommandExecutor(argument.ToCommandLine());
            executor.OnStandardOutput += delegate(string value)
            {
                var package = value.Split(new[] { ":" }, StringSplitOptions.None)[1];
                if (!_applicationPackages.Contains(package))
                {
                    Application.Current.Dispatcher.Invoke(delegate { ApplicationPackages.Add(package); });
                }
            };
            executor.Execute("adb");
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

        private void RebootDevice()
        {
            var result = MessageBox.Show("确定重启该设备？", "重启设备", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                var argument = new ArgumentCreator();
                //重启设备
                //adb reboot 
                argument.Append("-s").Append(_selectedDevice).Append("reboot");
                new CommandExecutor(argument.ToCommandLine()).Execute("adb");
            }
        }

        private void TakeScreenshot()
        {
            var argument = new ArgumentCreator();
            //截取屏幕截图并保存到指定位置
            //adb shell screencap -p /sdcard/screen.png 
            argument.Append("-s").Append(_selectedDevice)
                .Append("shell").Append("screencap").Append("-p").Append($"/sdcard/{DateTime.Now:yyyyMMddHHmmss}.png");
            new CommandExecutor(argument.ToCommandLine()).Execute("adb");
            Growl.Success("屏幕抓取成功");
        }

        private void UninstallApplication()
        {
            if (string.IsNullOrEmpty(_selectedPackage))
            {
                Growl.Fatal("操作失败", "请先选择需要卸载的应用");
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
                    argument.Append("-s").Append(_selectedDevice).Append("uninstall").Append(_selectedPackage);
                    var executor = new CommandExecutor(argument.ToCommandLine());
                    executor.OnStandardOutput += delegate(string value)
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            ApplicationPackages.Remove(_selectedPackage);
                            Growl.Success(value);
                        });
                    };
                    executor.Execute("adb");
                });
            }
        }

        /// <summary>
        /// TODO LoadingDialog未实现
        /// </summary>
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
                Growl.Error("安装包路径错误，请重新选择");
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
                argument.Append("-s").Append(_selectedDevice).Append("install").Append("-r").Append(filePath);
                var executor = new CommandExecutor(argument.ToCommandLine());
                executor.OnStandardOutput += delegate(string value)
                {
                    if (value.Equals("Success"))
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            _eventAggregator.GetEvent<CloseLoadingDialogEvent>().Publish();
                            Growl.Success(value);
                        });
                        GetDeviceApplication();
                    }
                };
                executor.Execute("adb");
            });
        }
    }
}
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
        public string Title => "ADB";

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
            _refreshDeviceTimer.Tick -= TimerTickEvent_Handler;
            _refreshDeviceTimer.Stop();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            _refreshDeviceTimer.Tick += TimerTickEvent_Handler;
            _refreshDeviceTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshDeviceTimer.Start();
        }


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

        private bool _hasSelectedDevice;

        public bool HasSelectedDevice
        {
            get => _hasSelectedDevice;
            set
            {
                _hasSelectedDevice = value;
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

        public DelegateCommand<string> DeviceSelectedCommand { set; get; }
        public DelegateCommand RefreshDeviceCommand { set; get; }
        public DelegateCommand OutputImageCommand { set; get; }
        public DelegateCommand ScreenshotCommand { set; get; }
        public DelegateCommand InstallCommand { set; get; }
        public DelegateCommand RebootDeviceCommand { set; get; }
        public DelegateCommand SortApplicationCommand { set; get; }
        public DelegateCommand<string> PackageSelectedCommand { set; get; }
        public DelegateCommand UninstallCommand { set; get; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly DispatcherTimer _refreshDeviceTimer = new DispatcherTimer();
        private string _selectedDevice = string.Empty;
        private string _selectedPackage = string.Empty;
        private bool _isAscending;

        public AndroidDebugBridgeViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            DeviceSelectedCommand = new DelegateCommand<string>(DeviceSelected);
            RefreshDeviceCommand = new DelegateCommand(RefreshDevice);
            OutputImageCommand = new DelegateCommand(PullScreenshot);
            ScreenshotCommand = new DelegateCommand(TakeScreenshot);
            InstallCommand = new DelegateCommand(InstallApplication);
            RebootDeviceCommand = new DelegateCommand(RebootDevice);
            SortApplicationCommand = new DelegateCommand(SortApplication);
            PackageSelectedCommand = new DelegateCommand<string>(PackageSelected);
            UninstallCommand = new DelegateCommand(UninstallApplication);
        }

        private void TimerTickEvent_Handler(object sender, EventArgs e)
        {
            if (!_deviceItems.Any())
            {
                RefreshDevice();
            }
            else
            {
                _refreshDeviceTimer.Tick -= TimerTickEvent_Handler;
                _refreshDeviceTimer.Stop();
            }
        }

        private void DeviceSelected(string device)
        {
            _selectedDevice = device;
            HasSelectedDevice = true;
            //获取设备详情
            Task.Run(() =>
            {
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
                        DeviceDensity =
                            $"{value.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim()}";
                    };
                    executor.Execute("adb");
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
            });
            
            GetDeviceApplication();
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
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("pm").Append("list")
                    .Append("package").Append("-3");
                var executor = new CommandExecutor(argument.ToCommandLine());
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

        /// <summary>
        /// 刷新设备列表
        /// </summary>
        /// <returns></returns>
        private void RefreshDevice()
        {
            if (DeviceItems.Any())
            {
                DeviceItems.Clear();
            }

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
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_deviceItems.Contains(split[0]))
                    {
                        return;
                    }

                    DeviceItems.Add(split[0]);
                }));
            };
            Task.Run(() => { executor.Execute("adb"); });
        }

        private void TakeScreenshot()
        {
            Task.Run(() =>
            {
                var argument = new ArgumentCreator();
                //截取屏幕截图并保存到指定位置
                //adb shell screencap -p /sdcard/20241214112123.png 
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
                argument.Append("-s").Append(_selectedDevice).Append("shell").Append("screencap").Append("-p")
                    .Append($"/sdcard/{fileName}");
                new CommandExecutor(argument.ToCommandLine()).Execute("adb");
                PullScreenshot();
            });
        }

        private void PullScreenshot()
        {
            var dialogParameters = new DialogParameters
            {
                { "device", _selectedDevice }
            };
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _dialogService.ShowDialog("ScreenShotListDialog", dialogParameters, dialogResult =>
                {
                    if (dialogResult.Result == ButtonResult.OK)
                    {
                        var selectedImage = dialogResult.Parameters.GetValue<string>("selectedImage");
                        var fileName = Path.GetFileName(selectedImage);
                        var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/{fileName}";
                        Task.Run(() =>
                        {
                            var argument = new ArgumentCreator();
                            argument.Append("-s").Append(_selectedDevice).Append("pull").Append(selectedImage)
                                .Append(filePath);
                            new CommandExecutor(argument.ToCommandLine()).Execute("adb");
                        });
                    }
                });
            }));
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
                argument.Append("-s").Append(_selectedDevice).Append("install").Append("-r").Append(filePath);
                var executor = new CommandExecutor(argument.ToCommandLine());
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
                    argument.Append("-s").Append(_selectedDevice).Append("uninstall").Append(_selectedPackage);
                    var executor = new CommandExecutor(argument.ToCommandLine());
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
    }
}
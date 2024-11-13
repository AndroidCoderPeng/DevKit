using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using DevKit.Utils;
using DevKit.Views;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class AndroidDebugBridgeViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<string> _deviceItems;

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

        private string _memoryRatio;

        public string MemoryRatio
        {
            get => _memoryRatio;
            set
            {
                _memoryRatio = value;
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

        private ObservableCollection<string> _applicationPackages;

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
        public DelegateCommand RefreshApplicationCommand { set; get; }
        public DelegateCommand<string> PackageSelectedCommand { set; get; }
        public DelegateCommand RebootDeviceCommand { set; get; }
        public DelegateCommand ScreenshotCommand { set; get; }
        public DelegateCommand UninstallCommand { set; get; }
        public DelegateCommand<AndroidDebugBridgeView> InstallCommand { set; get; }

        #endregion

        private string _selectedDevice = string.Empty;
        private string _selectedPackage = string.Empty;

        /// <summary>
        /// DispatcherTimer与窗体为同一个线程，故如果频繁的执行DispatcherTimer的话，会造成主线程的卡顿。
        /// 用System.Timers.Timer来初始化一个异步的时钟，初始化一个时钟的事件，在时钟的事件中采用BeginInvoke来进行异步委托。
        /// 这样就能防止timer控件的同步事件不停的刷新时，界面的卡顿
        /// </summary>
        private readonly Timer _refreshDeviceTimer = new Timer(1000);

        private readonly Timer _refreshDeviceDetailTimer = new Timer(3000);

        public AndroidDebugBridgeViewModel()
        {
            //定时刷新设备列表，可能会为空，因为开发者模式可能没开
            _refreshDeviceTimer.Elapsed += TimerElapsedEvent_Handler;
            _refreshDeviceTimer.Enabled = true;

            RefreshDeviceCommand = new DelegateCommand(RefreshDevice);
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_deviceItems == null || !_deviceItems.Any())
            {
                RefreshDevice();
            }
            else
            {
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
            executor.OnErrorOutput += ErrorOutput_EventHandler;
            executor.Execute("adb");
        }

        private void ErrorOutput_EventHandler(string output)
        {
        }

        private void StandardOutput_EventHandler(string output)
        {
            Console.WriteLine(output);
            var result = new ObservableCollection<string>();
            if (output.Equals("List of devices attached"))
            {
            }
            else
            {
                // 259dc884        device
                // 192.168.3.11:40773      device
                //解析返回值，序列化成 ObservableCollection
                var strings = output.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 1; i < strings.Length; i++)
                {
                    var newLine = Regex.Replace(strings[i], @"\s", "*");
                    var split = newLine.Split(new[] { "*" }, StringSplitOptions.RemoveEmptyEntries);
                    result.Add(split[0]);
                }
            }

            Console.WriteLine(JsonConvert.SerializeObject(result));
            DeviceItems = result;
        }
    }
}
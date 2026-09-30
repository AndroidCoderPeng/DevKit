using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using HandyControl.Tools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class NetConfigurationViewModel : BindableBase, IDialogAware
    {
        public string Title => "网络信息";

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

        private ObservableCollection<string> _addressItems = new ObservableCollection<string>();

        public ObservableCollection<string> AddressItems
        {
            get => _addressItems;
            set
            {
                _addressItems = value;
                RaisePropertyChanged();
            }
        }

        private string _ip4 = string.Empty;

        public string Ip4
        {
            get => _ip4;
            set
            {
                _ip4 = value;
                RaisePropertyChanged();
            }
        }

        private string _subnetMask = string.Empty;

        public string SubnetMask
        {
            get => _subnetMask;
            set
            {
                _subnetMask = value;
                RaisePropertyChanged();
            }
        }

        private string _gateway = string.Empty;

        public string Gateway
        {
            get => _gateway;
            set
            {
                _gateway = value;
                RaisePropertyChanged();
            }
        }

        private string _deviceMac = string.Empty;

        public string DeviceMac
        {
            get => _deviceMac;
            set
            {
                _deviceMac = value;
                RaisePropertyChanged();
            }
        }

        private string _dns = string.Empty;

        public string Dns
        {
            get => _dns;
            set
            {
                _dns = value;
                RaisePropertyChanged();
            }
        }

        private string _adapterType = string.Empty;

        public string AdapterType
        {
            get => _adapterType;
            set
            {
                _adapterType = value;
                RaisePropertyChanged();
            }
        }

        private long _netSpeed;

        public long NetSpeed
        {
            get => _netSpeed;
            set
            {
                _netSpeed = value;
                RaisePropertyChanged();
            }
        }

        private string _dhcp = string.Empty;

        public string Dhcp
        {
            get => _dhcp;
            set
            {
                _dhcp = value;
                RaisePropertyChanged();
            }
        }

        private string _connectionState = "未连接";

        public string ConnectionState
        {
            get => _connectionState;
            set
            {
                _connectionState = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush _connectionStateColor = new SolidColorBrush(Colors.LightGray);

        public SolidColorBrush ConnectionStateColor
        {
            get => _connectionStateColor;
            set
            {
                _connectionStateColor = value;
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

        /////////////////////////////////////////////////////

        private ObservableCollection<CommandCmdModel> _commandItems =
            new ObservableCollection<CommandCmdModel>();

        public ObservableCollection<CommandCmdModel> CommandItems
        {
            get => _commandItems;
            set
            {
                _commandItems = value;
                RaisePropertyChanged();
            }
        }

        private string _targetAddress = string.Empty;

        public string TargetAddress
        {
            get => _targetAddress;
            set
            {
                _targetAddress = value;
                RaisePropertyChanged();
            }
        }

        private bool _isLoopBoxChecked = true;

        public bool IsLoopBoxChecked
        {
            set
            {
                _isLoopBoxChecked = value;
                RaisePropertyChanged();
            }
            get => _isLoopBoxChecked;
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

        public DelegateCommand RefreshIpAddressCommand { set; get; }
        public DelegateCommand<string> AddressItemSelectedCommand { set; get; }
        public DelegateCommand<string> InfoItemCopyCommand { set; get; }
        public DelegateCommand<CommandCmdModel> CommandItemSelectedCommand { set; get; }
        public DelegateCommand TestNetCommand { set; get; }

        #endregion

        private DispatcherTimer _toastTimer;

        public NetConfigurationViewModel(IAppDataService appDataService)
        {
            AddressItems.AddRange(appDataService.GetIPv4Address());
            CommandItems = new ObservableCollection<CommandCmdModel>
            {
                new CommandCmdModel { Command = "ipconfig /all", Description = "查看完整网卡信息", NeedParams = false },
                new CommandCmdModel { Command = "ping", Description = "测试网络连通性", NeedParams = true },
                new CommandCmdModel { Command = "tracert", Description = "查看网络路由路径", NeedParams = true },
                new CommandCmdModel { Command = "nslookup", Description = "查询 DNS 解析", NeedParams = true },
                new CommandCmdModel { Command = "arp -a", Description = "查看 ARP 缓存", NeedParams = false },
                new CommandCmdModel { Command = "route print", Description = "查看本机路由表", NeedParams = false },
                new CommandCmdModel { Command = "netstat -ano", Description = "查看端口和进程 PID", NeedParams = false },
                new CommandCmdModel { Command = "hostname", Description = "查看计算机名", NeedParams = false },
                new CommandCmdModel { Command = "getmac", Description = "查看网卡 MAC 地址", NeedParams = false }
            };

            AddressItemSelectedCommand = new DelegateCommand<string>(ip =>
            {
                // 获取该 IP 的网络信息
                Ip4 = ip;
                SubnetMask = GetSubnetMask(ip);
                Gateway = GetGateway(ip);
                DeviceMac = GetDeviceMac(ip);
                Dns = GetDns(ip);

                // 网络适配器类型以及带宽
                var adapterType = GetAdapterType(ip);
                AdapterType = adapterType.Item1;
                NetSpeed = adapterType.Item2;

                Dhcp = GetDhcp(ip);

                // 连接状态
                var state = GetConnectionState(ip);
                ConnectionState = state.Item1;
                ConnectionStateColor = state.Item2;
            });

            RefreshIpAddressCommand = new DelegateCommand(() =>
            {
                if (_addressItems.Any())
                {
                    AddressItems.Clear();
                }

                AddressItems.AddRange(appDataService.GetIPv4Address());
            });

            InfoItemCopyCommand = new DelegateCommand<string>(item =>
            {
                var dataObject = new DataObject(DataFormats.UnicodeText, item);
                Clipboard.SetDataObject(dataObject);

                ShowToast("参数已复制");
            });

            CommandItemSelectedCommand = new DelegateCommand<CommandCmdModel>(item =>
            {
                // if (commandValue.Equals("ping"))
                // {
                //     return;
                // }
                //
                // Task.Run(() => { ExecuteCommand(commandValue); });
            });

            TestNetCommand = new DelegateCommand(TestNet);
        }

        private void TestNet()
        {
            if (_targetAddress.IsIp())
            {
                Task.Run(() =>
                {
                    var argument = new ArgumentCreator();
                    argument.Append("ping").Append(_targetAddress);
                    if (_isLoopBoxChecked)
                    {
                        argument.Append("-t");
                    }

                    ExecuteCommand(argument.ToCommandLine());
                });
            }
            else
            {
                // Task.Run(async () =>
                // {
                //     var addresses = await Dns.GetHostAddressesAsync(_targetAddress);
                //     var ip = addresses.FirstOrDefault()?.ToString() ?? string.Empty;
                //     if (string.IsNullOrEmpty(ip))
                //     {
                //         MessageBox.Show("请输入正确的目标地址", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                //     }
                //     else
                //     {
                //         var argument = new ArgumentCreator();
                //         argument.Append("ping").Append(ip);
                //         ExecuteCommand(argument.ToCommandLine());
                //     }
                // });
            }
        }

        private void ExecuteCommand(string command)
        {
            var executor = new CommandExecutor($"/c {command}");
            var result = new StringBuilder();
            executor.OnStandardOutput += delegate(string value)
            {
                result.AppendLine(value);
                OutputResult = result.ToString();
            };
            executor.Execute("cmd");
        }

        // ---- 私有辅助函数 -----

        private NetworkInterface FindNetworkInterface(string ip)
        {
            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(network =>
                network.OperationalStatus == OperationalStatus.Up &&
                network.GetIPProperties().UnicastAddresses.Any(address =>
                    address.Address.AddressFamily == AddressFamily.InterNetwork && address.Address.ToString() == ip)
            );
        }

        private string GetSubnetMask(string ip)
        {
            var network = FindNetworkInterface(ip);
            var address = network?.GetIPProperties().UnicastAddresses
                .FirstOrDefault(item => item.Address.ToString() == ip);
            return address?.IPv4Mask?.ToString() ?? string.Empty;
        }

        private string GetGateway(string ip)
        {
            var network = FindNetworkInterface(ip);
            return network?.GetIPProperties().GatewayAddresses
                .Select(item => item.Address)
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString() ?? string.Empty;
        }

        private string GetDeviceMac(string ip)
        {
            var network = FindNetworkInterface(ip);
            return network == null
                ? string.Empty
                : string.Join("-", network.GetPhysicalAddress().GetAddressBytes()
                    .Select(value => value.ToString("X2")));
        }

        private string GetDns(string ip)
        {
            var network = FindNetworkInterface(ip);
            return network == null
                ? string.Empty
                : string.Join(", ",
                    network.GetIPProperties().DnsAddresses
                        .Where(address => address.AddressFamily == AddressFamily.InterNetwork));
        }

        private (string, long) GetAdapterType(string ip)
        {
            var network = FindNetworkInterface(ip);
            if (network == null)
            {
                return ("未知", 0);
            }

            string type;
            switch (network.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                    type = "以太网";
                    break;
                case NetworkInterfaceType.Wireless80211:
                    type = "Wi-Fi";
                    break;
                case NetworkInterfaceType.Loopback:
                    type = "回环网卡";
                    break;
                case NetworkInterfaceType.Tunnel:
                    type = "隧道网卡";
                    break;
                default:
                    type = "未知";
                    break;
            }

            // NetworkInterface.Speed 单位是 bit/s，除以 1000000 得到 Mbps
            var speedMbps = Math.Max(0L, network.Speed / 1000000L);
            return (type, speedMbps);
        }

        private string GetDhcp(string ip)
        {
            var network = FindNetworkInterface(ip);
            if (network == null)
            {
                return string.Empty;
            }

            var ipv4Properties = network.GetIPProperties().GetIPv4Properties();
            return ipv4Properties != null && ipv4Properties.IsDhcpEnabled
                ? "已启用"
                : "未启用";
        }

        private (string, SolidColorBrush ) GetConnectionState(string ip)
        {
            var network = FindNetworkInterface(ip);

            if (network == null)
            {
                return ("未连接", new SolidColorBrush(Colors.LightGray));
            }

            if (network.OperationalStatus == OperationalStatus.Up)
            {
                return ("已连接", new SolidColorBrush(Colors.LimeGreen));
            }

            return ("未连接", new SolidColorBrush(Colors.LightGray));
        }

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
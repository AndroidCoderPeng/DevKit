using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DevKit.DataService;
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

        /////////////////////////////////////////////////////

        private ObservableCollection<string> _commandItems = new ObservableCollection<string>();

        public ObservableCollection<string> CommandItems
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
        public DelegateCommand<string> CommandItemSelectedCommand { set; get; }
        public DelegateCommand TestNetCommand { set; get; }

        #endregion

        public NetConfigurationViewModel(IAppDataService appDataService)
        {
            AddressItems.AddRange(appDataService.GetIPv4Address());
            CommandItems = new ObservableCollection<string>
            {
                "ipconfig", "ping"
            };

            AddressItemSelectedCommand = new DelegateCommand<string>(ip =>
            {
                // 获取该 IP 的网络信息
                Ip4 = ip;
                SubnetMask = GetSubnetMask(ip);
                Gateway = GetGateway(ip);
                DeviceMac = GetDeviceMac(ip);
                Dns = GetDns(ip);
                AdapterType = GetAdapterType(ip);
                Dhcp = GetDhcp(ip);
            });

            RefreshIpAddressCommand = new DelegateCommand(() =>
            {
                if (_addressItems.Any())
                {
                    AddressItems.Clear();
                }

                AddressItems.AddRange(appDataService.GetIPv4Address());
            });

            CommandItemSelectedCommand = new DelegateCommand<string>(ItemSelected);
            TestNetCommand = new DelegateCommand(TestNet);
        }

        private void ItemSelected(string commandValue)
        {
            if (commandValue.Equals("ping"))
            {
                // ping 命令会单独执行
                return;
            }

            Task.Run(() => { ExecuteCommand(commandValue); });
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

        private string GetAdapterType(string ip)
        {
            var network = FindNetworkInterface(ip);
            switch (network?.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                    return "以太网";
                case NetworkInterfaceType.Wireless80211:
                    return "Wi-Fi";
                case NetworkInterfaceType.Loopback:
                    return "回环网卡";
                case NetworkInterfaceType.Tunnel:
                    return "隧道网卡";
                default:
                    return network?.NetworkInterfaceType.ToString() ?? string.Empty;
            }
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
    }
}
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DevKit.Models;

namespace DevKit.DataService
{
    public class AppDataServiceImpl : IAppDataService
    {
        public List<MainMenu> GetAndroidTools()
        {
            return new List<MainMenu>
            {
                new MainMenu { MenuIcon = "\ue71c", MenuName = "ADB", ViewName = "AndroidDebugBridgeView" },
                new MainMenu { MenuIcon = "\ue700", MenuName = "APK", ViewName = "ApplicationPackageView" },
                new MainMenu { MenuIcon = "\ue673", MenuName = "JNI逆向", ViewName = "JNIReverseView" }
            };
        }

        public List<MainMenu> GetSocketTools()
        {
            return new List<MainMenu>
            {
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP客户端", ViewName = "TcpClientView" },
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP服务端", ViewName = "TcpServerView" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP客户端", ViewName = "UdpClientView" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP服务端", ViewName = "UdpServerView" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS客户端", ViewName = "WsClientView" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS服务端", ViewName = "WsServerView" },
                new MainMenu { MenuIcon = "\ue8f6", MenuName = "串口通信", ViewName = "SerialPortView" },
                new MainMenu { MenuIcon = "\ue795", MenuName = "SFTP", ViewName = null }
            };
        }

        public List<MainMenu> GetOtherTools()
        {
            return new List<MainMenu>
            {
                new MainMenu { MenuIcon = "\ue660", MenuName = "颜色处理", ViewName = "ColorResourceView" },
                new MainMenu { MenuIcon = "\ue6b4", MenuName = "网络配置", ViewName = "NetConfigurationView" }
            };
        }

        public List<string> GetIPv4Address()
        {
            var ipv4Addresses = new List<string>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var network in interfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var ipAddresses = network.GetIPProperties().UnicastAddresses;
                foreach (var ip in ipAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.Address.ToString() != "127.0.0.1")
                    {
                        ipv4Addresses.Add(ip.Address.ToString());
                    }
                }
            }

            return ipv4Addresses;
        }
    }
}
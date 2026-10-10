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
                new MainMenu { MenuIcon = "\ue71c", MenuName = "ADB" },
                new MainMenu { MenuIcon = "\ue700", MenuName = "APK" },
                new MainMenu { MenuIcon = "\ue673", MenuName = "JNI逆向" }
            };
        }

        public List<MainMenu> GetSocketTools()
        {
            return new List<MainMenu>
            {
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP客户端" },
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP服务端" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP客户端" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP服务端" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS客户端" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS服务端" }
            };
        }

        public List<MainMenu> GetOtherTools()
        {
            return new List<MainMenu>
            {
                new MainMenu { MenuIcon = "\ue660", MenuName = "颜色处理" },
                new MainMenu { MenuIcon = "\ue6b4", MenuName = "网络配置" },
                new MainMenu { MenuIcon = "\ue70f", MenuName = "视频裁剪" }
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
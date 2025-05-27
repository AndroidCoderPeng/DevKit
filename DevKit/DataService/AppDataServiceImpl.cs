using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DevKit.Models;

namespace DevKit.DataService
{
    public class AppDataServiceImpl : IAppDataService
    {
        public List<MainMenuModel> GetAndroidTools()
        {
            return new List<MainMenuModel>
            {
                new MainMenuModel { MenuIcon = "\ue71c", MenuName = "ADB" },
                new MainMenuModel { MenuIcon = "\ue700", MenuName = "APK" }
            };
        }

        public List<MainMenuModel> GetSocketTools()
        {
            return new List<MainMenuModel>
            {
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "TCP客户端" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "TCP服务端" },
                new MainMenuModel { MenuIcon = "\ue8ab", MenuName = "UDP客户端" },
                new MainMenuModel { MenuIcon = "\ue8ab", MenuName = "UDP服务端" },
                new MainMenuModel { MenuIcon = "\ue8b2", MenuName = "WS客户端" },
                new MainMenuModel { MenuIcon = "\ue8b2", MenuName = "WS服务端" }
            };
        }

        public List<MainMenuModel> GetOtherTools()
        {
            return new List<MainMenuModel>
            {
                new MainMenuModel { MenuIcon = "\ue660", MenuName = "颜色值转换" }
            };
        }

        public string GetIPv4Address()
        {
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
                        // 返回第一个符合条件的IPv4地址
                        return ip.Address.ToString();
                    }
                }
            }

            return null;
        }
    }
}
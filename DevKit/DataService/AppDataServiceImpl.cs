using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DevKit.DataService
{
    public class AppDataServiceImpl : IAppDataService
    {
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
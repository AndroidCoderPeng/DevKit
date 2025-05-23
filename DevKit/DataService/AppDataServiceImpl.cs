using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;

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

        public ApkConfigCache LoadApkCacheConfig()
        {
            using (var dataBase = new DataBaseConnection())
            {
                //表里要么没有数据要么只有一条数据
                var queryResult = dataBase.Table<ApkConfigCache>();
                if (queryResult.Any())
                {
                    return queryResult.First();
                }

                return new ApkConfigCache();
            }
        }

        public ClientConfigCache LoadClientConfigCache(int connectionType)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ClientConfigCache>().Where(x => x.Type == connectionType);
                if (queryResult.Any())
                {
                    return queryResult.First();
                }

                return new ClientConfigCache();
            }
        }

        public List<ExCommandCache> LoadCommandExtensionCaches(int parentType)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ExCommandCache>().Where(x => x.ParentType == parentType);
                return queryResult.ToList();
            }
        }

        public void SaveCacheConfig<T>(T configCache)
        {
            using (var dataBase = new DataBaseConnection())
            {
                if (configCache is ApkConfigCache)
                {
                    if (dataBase.Table<ApkConfigCache>().Any())
                    {
                        dataBase.Update(configCache);
                    }
                    else
                    {
                        dataBase.Insert(configCache);
                    }
                }
                else if (configCache is ClientConfigCache client)
                {
                    var queryResult = dataBase.Table<ClientConfigCache>()
                        .Where(x => x.Id == client.Id && x.Type == client.Type);
                    if (queryResult.Any())
                    {
                        dataBase.Update(client);
                    }
                    else
                    {
                        dataBase.Insert(client);
                    }
                }
                else if (configCache is ExCommandCache command)
                {
                    var queryResult = dataBase.Table<ExCommandCache>()
                        .Where(x =>
                            x.ParentType == command.ParentType &&
                            x.CommandValue == command.CommandValue
                        );
                    if (queryResult.Any())
                    {
                        MessageBox.Show("指令已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dataBase.Insert(command);
                }
            }
        }

        public void DeleteExtensionCommandCache(int connectionType, int cacheId)
        {
            using (var dataBase = new DataBaseConnection())
            {
                dataBase.Table<ExCommandCache>().Where(x =>
                    x.ParentType == connectionType &&
                    x.Id == cacheId
                ).Delete();
            }
        }

        public List<string> GetAllIPv4Addresses()
        {
            var result = new List<string>();
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
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.Address.ToString() != "127.0.0.1")
                        {
                            result.Add($"{ip.Address}");
                        }
                    }
                }
            }

            return result;
        }

        public List<string> GetColorSchemes()
        {
            return new List<string> { "中国传统色系", "低调色系" };
        }

        public async Task<List<ColorResourceCache>> GetColorsByScheme(string colorScheme)
        {
            List<ColorResourceCache> result = null;
            using (var dataBase = new DataBaseConnection())
            {
                await Task.Run(() =>
                {
                    result = dataBase.Table<ColorResourceCache>().Where(x => x.Scheme == colorScheme).ToList();
                });
            }

            return result;
        }
    }
}
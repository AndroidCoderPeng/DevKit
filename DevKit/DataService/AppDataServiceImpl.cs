using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;

namespace DevKit.DataService
{
    public class AppDataServiceImpl : IAppDataService
    {
        public List<MainMenuModel> GetMainMenu()
        {
            return new List<MainMenuModel>
            {
                new MainMenuModel { MenuIcon = "\ue71c", MenuName = "ADB" },
                new MainMenuModel { MenuIcon = "\ue700", MenuName = "APK" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "TCP" },
                new MainMenuModel { MenuIcon = "\ue8ab", MenuName = "UDP" },
                new MainMenuModel { MenuIcon = "\ue8b2", MenuName = "WebSocket" },
                new MainMenuModel { MenuIcon = "\ue8a7", MenuName = "串口" },
                new MainMenuModel { MenuIcon = "\ue672", MenuName = "图标" },
                new MainMenuModel { MenuIcon = "\ue602", MenuName = "转码" },
                new MainMenuModel { MenuIcon = "\ue660", MenuName = "颜色" }
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

        public List<CommandExtensionCache> LoadCommandExtensionCaches(int parentId, int parentType)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<CommandExtensionCache>()
                    .Where(x =>
                        x.ParentId == parentId && x.ParentType == parentType
                    );
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
                        .Where(
                            x => x.Id == client.Id && x.Type == client.Type
                        );
                    if (queryResult.Any())
                    {
                        dataBase.Update(client);
                    }
                    else
                    {
                        dataBase.Insert(client);
                    }
                }
                else if (configCache is CommandExtensionCache command)
                {
                    var queryResult = dataBase.Table<CommandExtensionCache>()
                        .Where(
                            x =>
                                x.ParentId == command.ParentId &&
                                x.ParentType == command.ParentType &&
                                x.CommandValue == command.CommandValue
                        );
                    if (queryResult.Any())
                    {
                        return;
                    }

                    dataBase.Insert(command);
                }
            }
        }

        public void DeleteExtensionCommandCache(int cacheId)
        {
            using (var dataBase = new DataBaseConnection())
            {
                dataBase.Table<CommandExtensionCache>().Delete(x => x.Id == cacheId);
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

        public List<string> GetPlatformTypes()
        {
            return new List<string> { "Windows", "Android", "iOS" };
        }

        public List<PlatformImageTypeModel> GetImageTypesByPlatform(string platform)
        {
            if (platform.Equals("Windows"))
            {
                return new List<PlatformImageTypeModel>
                {
                    new PlatformImageTypeModel { Size = "32*32", },
                    new PlatformImageTypeModel { Size = "64*64", },
                    new PlatformImageTypeModel { Size = "128*128", },
                    new PlatformImageTypeModel { Size = "256*256", }
                };
            }
            else if (platform.Equals("Android"))
            {
                return new List<PlatformImageTypeModel>();
            }
            else
            {
                return new List<PlatformImageTypeModel>();
            }
        }
    }
}
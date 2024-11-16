using System.Collections.Generic;
using System.Linq;
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
                new MainMenuModel { MenuIcon = "\ue8a7", MenuName = "串口通信" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "图标" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "微软翻译" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "转码" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "颜色" }
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

        public TcpClientConfigCache LoadTcpClientConfigCache()
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<TcpClientConfigCache>();
                if (queryResult.Any())
                {
                    return queryResult.First();
                }

                return new TcpClientConfigCache();
            }
        }
        
        public CommandExtensionCache LoadCommandExtensionCache()
        {
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<CommandExtensionCache>();
                if (queryResult.Any())
                {
                    return queryResult.First();
                }

                return new CommandExtensionCache();
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
                else if (configCache is TcpClientConfigCache)
                {
                    if (dataBase.Table<TcpClientConfigCache>().Any())
                    {
                        dataBase.Update(configCache);
                    }
                    else
                    {
                        dataBase.Insert(configCache);
                    }
                }
                else if (configCache is CommandExtensionCache)
                {
                    if (dataBase.Table<CommandExtensionCache>().Any())
                    {
                        dataBase.Update(configCache);
                    }
                    else
                    {
                        dataBase.Insert(configCache);
                    }
                }
            }
        }
    }
}
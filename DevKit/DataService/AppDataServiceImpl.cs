using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Media.Imaging;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Newtonsoft.Json;

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
            return new List<string> { "Windows", "Android" };
        }

        public List<PlatformImageTypeModel> GetImageTypesByPlatform(string platform, Uri uri)
        {
            if (uri != null)
            {
                var bitmapImage = new BitmapImage(uri);
                if (platform.Equals("Windows"))
                {
                    return new List<PlatformImageTypeModel>
                    {
                        new PlatformImageTypeModel { Width = 32, Height = 32, ResultImage = bitmapImage },
                        new PlatformImageTypeModel { Width = 64, Height = 64, ResultImage = bitmapImage },
                        new PlatformImageTypeModel { Width = 128, Height = 128, ResultImage = bitmapImage },
                        new PlatformImageTypeModel { Width = 256, Height = 256, ResultImage = bitmapImage }
                    };
                }

                return new List<PlatformImageTypeModel>
                {
                    new PlatformImageTypeModel
                        { Width = 48, Height = 48, AndroidSizeTag = "mdpi", ResultImage = bitmapImage },
                    new PlatformImageTypeModel
                        { Width = 72, Height = 72, AndroidSizeTag = "hdpi", ResultImage = bitmapImage },
                    new PlatformImageTypeModel
                        { Width = 96, Height = 96, AndroidSizeTag = "xhdpi", ResultImage = bitmapImage },
                    new PlatformImageTypeModel
                        { Width = 144, Height = 144, AndroidSizeTag = "xxhdpi", ResultImage = bitmapImage },
                    new PlatformImageTypeModel
                        { Width = 192, Height = 192, AndroidSizeTag = "xxxhdpi", ResultImage = bitmapImage }
                };
            }

            if (platform.Equals("Windows"))
            {
                return new List<PlatformImageTypeModel>
                {
                    new PlatformImageTypeModel { Width = 32, Height = 32 },
                    new PlatformImageTypeModel { Width = 64, Height = 64 },
                    new PlatformImageTypeModel { Width = 128, Height = 128 },
                    new PlatformImageTypeModel { Width = 256, Height = 256 }
                };
            }

            return new List<PlatformImageTypeModel>
            {
                new PlatformImageTypeModel { Width = 48, Height = 48, AndroidSizeTag = "mdpi" },
                new PlatformImageTypeModel { Width = 72, Height = 72, AndroidSizeTag = "hdpi" },
                new PlatformImageTypeModel { Width = 96, Height = 96, AndroidSizeTag = "xhdpi" },
                new PlatformImageTypeModel { Width = 144, Height = 144, AndroidSizeTag = "xxhdpi" },
                new PlatformImageTypeModel { Width = 192, Height = 192, AndroidSizeTag = "xxxhdpi" }
            };
        }

        public AsciiCodeModel QueryAsciiCodeByHex(string hexCode)
        {
            var asciiJson = File.ReadAllText("AsciiTable.json");
            var models = JsonConvert.DeserializeObject<List<AsciiCodeModel>>(asciiJson);
            return models.Find(x => x.HexValue == hexCode.ToUpper());
        }
    }
}
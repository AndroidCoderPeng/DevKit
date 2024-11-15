using System;
using System.Collections.Generic;
using System.IO;
using DevKit.Configs;
using DevKit.Models;
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
                new MainMenuModel { MenuIcon = "\ue8a7", MenuName = "串口通信" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "图标" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "微软翻译" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "转码" },
                new MainMenuModel { MenuIcon = "\ue8a9", MenuName = "颜色" }
            };
        }

        public void SaveCacheConfig(ConfigCache config)
        {
            var fileName = $@"{AppDomain.CurrentDomain.BaseDirectory}ConfigCache.json";
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }

        public ConfigCache LoadCacheConfig()
        {
            try
            {
                var fileName = $@"{AppDomain.CurrentDomain.BaseDirectory}ConfigCache.json";
                var json = File.ReadAllText(fileName);
                return JsonConvert.DeserializeObject<ConfigCache>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
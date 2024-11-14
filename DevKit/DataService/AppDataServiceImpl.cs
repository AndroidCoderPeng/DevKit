using System.Collections.Generic;
using DevKit.Models;

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
    }
}
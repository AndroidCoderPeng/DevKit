using System.Collections.Generic;

namespace DevKit.DataService
{
    public class AppDataServiceImpl : IAppDataService
    {
        private readonly List<string> _menu = new List<string>
        {
            "ADB", "APK", "TCP", "UDP", "WebSocket",
            "串口通信", "图标", "微软翻译", "转码", "颜色"
        };

        public List<string> GetMainMenu()
        {
            return _menu;
        }
    }
}
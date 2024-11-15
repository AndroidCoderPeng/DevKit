using System.Collections.Generic;
using DevKit.Configs;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetMainMenu();

        void SaveCacheConfig(ConfigCache config);

        ConfigCache LoadCacheConfig();
    }
}
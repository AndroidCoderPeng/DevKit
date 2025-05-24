using System.Collections.Generic;
using System.Threading.Tasks;
using DevKit.Cache;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetAndroidTools();
        
        List<MainMenuModel> GetSocketTools();
        
        List<MainMenuModel> GetOtherTools();

        string GetIPv4Address();
        
        List<ExCommandCache> LoadCommandExtensionCaches(int parentType);

        void DeleteExtensionCommandCache(int connectionType, int cacheId);

        List<string> GetColorSchemes();

        Task<List<ColorResourceCache>> GetColorsByScheme(string colorScheme);
    }
}
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

        ApkConfigCache LoadApkCacheConfig();

        ClientConfigCache LoadClientConfigCache(int connectionType);

        List<ExCommandCache> LoadCommandExtensionCaches(int parentType);

        void SaveCacheConfig<T>(T configCache);

        void DeleteExtensionCommandCache(int connectionType, int cacheId);

        List<string> GetAllIPv4Addresses();

        List<string> GetColorSchemes();

        Task<List<ColorResourceCache>> GetColorsByScheme(string colorScheme);
    }
}
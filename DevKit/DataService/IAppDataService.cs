using System.Collections.Generic;
using DevKit.Cache;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetMainMenu();

        ApkConfigCache LoadApkCacheConfig();

        TcpClientConfigCache LoadTcpClientConfigCache();

        List<CommandExtensionCache> LoadCommandExtensionCaches(int parentId, int parentType);

        void SaveCacheConfig<T>(T configCache);

        void DeleteExtensionCommandCache(int cacheId);
    }
}
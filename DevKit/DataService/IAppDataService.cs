using System;
using System.Collections.Generic;
using DevKit.Cache;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetMainMenu();

        ApkConfigCache LoadApkCacheConfig();

        ClientConfigCache LoadClientConfigCache(int connectionType);

        List<CommandExtensionCache> LoadCommandExtensionCaches(int parentId, int parentType);

        void SaveCacheConfig<T>(T configCache);

        void DeleteExtensionCommandCache(int cacheId);

        List<string> GetAllIPv4Addresses();

        List<string> GetPlatformTypes();

        List<PlatformImageTypeModel> GetImageTypesByPlatform(string platform, Uri uri);
    }
}
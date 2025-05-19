using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevKit.Cache;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetAndroidTools();

        ApkConfigCache LoadApkCacheConfig();

        ClientConfigCache LoadClientConfigCache(int connectionType);

        List<ExCommandCache> LoadCommandExtensionCaches(int parentType);

        void SaveCacheConfig<T>(T configCache);

        void DeleteExtensionCommandCache(int connectionType, int cacheId);

        List<string> GetAllIPv4Addresses();

        List<string> GetPlatformTypes();

        List<PlatformImageTypeModel> GetImageTypesByPlatform(string platform, Uri uri);

        AsciiCodeModel QueryAsciiCodeByHex(string hexCode);

        List<string> GetColorSchemes();

        Task<List<ColorResourceCache>> GetColorsByScheme(string colorScheme);
    }
}
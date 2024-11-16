﻿using System.Collections.Generic;
using DevKit.Cache;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetMainMenu();

        ApkConfigCache LoadApkCacheConfig();

        TcpClientConfigCache LoadTcpClientConfigCache();
        
        CommandExtensionCache LoadCommandExtensionCache();

        void SaveCacheConfig<T>(T configCache);
    }
}
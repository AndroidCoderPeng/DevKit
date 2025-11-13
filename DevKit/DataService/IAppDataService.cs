using System.Collections.Generic;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenuModel> GetAndroidTools();

        List<MainMenuModel> GetSocketTools();

        List<MainMenuModel> GetOtherTools();

        List<string> GetIPv4Address();
    }
}
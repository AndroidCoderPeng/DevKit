using System.Collections.Generic;
using DevKit.Models;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<MainMenu> GetAndroidTools();

        List<MainMenu> GetSocketTools();

        List<MainMenu> GetOtherTools();

        List<string> GetIPv4Address();
    }
}
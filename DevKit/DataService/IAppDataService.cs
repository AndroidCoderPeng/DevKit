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
        
        Task<List<ColorResourceCache>> GetColorsByScheme(string colorScheme);
    }
}
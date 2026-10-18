using System.Collections.Generic;

namespace DevKit.DataService
{
    public interface IAppDataService
    {
        List<string> GetIPv4Address();
    }
}
using System.Collections.Generic;
using DevKit.DataService;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class GenerateIconViewModel : BindableBase
    {
        #region VM

        public List<string> PlatformTypes { get; }

        #endregion

        #region DelegateCommand

        

        #endregion

        public GenerateIconViewModel(IAppDataService dataService)
        {
            PlatformTypes = dataService.GetPlatformTypes();
        }
    }
}
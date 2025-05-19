using System.Collections.Generic;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenuModel> AndroidTools { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand AndroidToolsMouseDoubleClickCommand { set; get; }

        #endregion

        public MainWindowViewModel(IAppDataService dataService)
        {
            AndroidTools = dataService.GetAndroidTools();
        }
    }
}
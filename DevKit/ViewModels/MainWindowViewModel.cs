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
        public List<MainMenuModel> SocketTools { get; }
        public List<MainMenuModel> OtherTools { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand AndroidToolsMouseDoubleClickCommand { set; get; }
        public DelegateCommand SocketToolsMouseDoubleClickCommand { set; get; }
        public DelegateCommand OtherToolsMouseDoubleClickCommand { set; get; }

        #endregion

        public MainWindowViewModel(IAppDataService dataService)
        {
            AndroidTools = dataService.GetAndroidTools();
            SocketTools = dataService.GetSocketTools();
            OtherTools = dataService.GetOtherTools();
        }
    }
}
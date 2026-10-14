using System.Collections.Generic;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenu> AndroidTools { get; }
        public List<MainMenu> SocketTools { get; }
        public List<MainMenu> OtherTools { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand<MainMenu> ToolClickedCommand { get; }

        #endregion

        public MainWindowViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            AndroidTools = dataService.GetAndroidTools();
            SocketTools = dataService.GetSocketTools();
            OtherTools = dataService.GetOtherTools();

            ToolClickedCommand = new DelegateCommand<MainMenu>(menu =>
            {
                if (menu == null || string.IsNullOrWhiteSpace(menu.ViewName))
                {
                    return;
                }

                dialogService.Show(menu.ViewName);
            });
        }
    }
}
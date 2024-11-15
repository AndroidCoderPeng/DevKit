﻿using System.Collections.Generic;
using System.Windows.Controls;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace DevKit.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenuModel> MenuModels { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand<ListBox> ItemSelectedCommand { set; get; }

        #endregion

        private readonly IRegionManager _regionManager;

        public MainWindowViewModel(IRegionManager regionManager, IAppDataService dataService)
        {
            _regionManager = regionManager;

            MenuModels = dataService.GetMainMenu();

            ItemSelectedCommand = new DelegateCommand<ListBox>(OnItemSelected);
        }

        private void OnItemSelected(ListBox box)
        {
            var region = _regionManager.Regions["ContentRegion"];
            switch (box.SelectedIndex)
            {
                case 0:
                    region.RequestNavigate("AndroidDebugBridgeView");
                    break;
                case 1:
                    region.RequestNavigate("ApplicationPackageView");
                    break;
                case 2:
                    region.RequestNavigate("TcpCommunicateView");
                    break;
            }
        }
    }
}
using System;
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

        public DelegateCommand<MainMenuModel> AndroidToolClickedCommand { get; }
        public DelegateCommand<MainMenuModel> SocketToolClickedCommand { get; }
        public DelegateCommand<MainMenuModel> OtherToolClickedCommand { get; }

        #endregion

        public MainWindowViewModel(IAppDataService dataService)
        {
            AndroidTools = dataService.GetAndroidTools();
            SocketTools = dataService.GetSocketTools();
            OtherTools = dataService.GetOtherTools();

            AndroidToolClickedCommand = new DelegateCommand<MainMenuModel>(OnAndroidToolClicked);
            SocketToolClickedCommand = new DelegateCommand<MainMenuModel>(OnSocketToolClicked);
            OtherToolClickedCommand = new DelegateCommand<MainMenuModel>(OnOtherToolClicked);
        }

        private void OnAndroidToolClicked(MainMenuModel model)
        {
            if (model == null) return;
            Console.WriteLine($@"点击了 Android 工具：{model.MenuName}");
        }

        private void OnSocketToolClicked(MainMenuModel model)
        {
            if (model == null) return;
            Console.WriteLine($@"点击了 Socket 工具：{model.MenuName}");
        }

        private void OnOtherToolClicked(MainMenuModel model)
        {
            if (model == null) return;
            Console.WriteLine($@"点击了 Other 工具：{model.MenuName}");
        }
    }
}
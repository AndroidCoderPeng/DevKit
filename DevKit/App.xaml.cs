using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Dialogs;
using DevKit.Models;
using DevKit.Utils;
using DevKit.ViewModels;
using DevKit.Views;
using Newtonsoft.Json;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;

namespace DevKit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Loaded += delegate
            {
                var regionManager = Container.Resolve<IRegionManager>();
                regionManager.RequestNavigate("ContentRegion", "AndroidDebugBridgeView");
                // using (var dataBase = new DataBaseConnection())
                // {
                //     if (!dataBase.Table<ColorResourceCache>().Any())
                //     {
                //         _ = StoreColorCacheAsync();
                //     }
                // }
            };
            return mainWindow;
        }

        private void SwitchTheme(string themeName)
        {
            var resourceDict = new ResourceDictionary();
            switch (themeName)
            {
                case "Dark":
                    resourceDict.Source = new Uri("Colors/DarkColor.xaml", UriKind.Relative);
                    break;
                default:
                    resourceDict.Source = new Uri("Colors/LightColor.xaml", UriKind.Relative);
                    break;
            }

            Current.Resources.MergedDictionaries[1] = resourceDict;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task StoreColorCacheAsync()
        {
            await Task.Run(() =>
            {
                using (var dataBase = new DataBaseConnection())
                {
                    var traditionColorJson = File.ReadAllText("TraditionColor.json");
                    var traditionColorModels = JsonConvert.DeserializeObject<List<ColorModel>>(traditionColorJson);
                    foreach (var colorModel in traditionColorModels)
                    {
                        var cache = new ColorResourceCache
                        {
                            Scheme = "中国传统色系",
                            Name = colorModel.Name,
                            Hex = colorModel.Hex
                        };
                        dataBase.Insert(cache);
                    }

                    var dimColorJson = File.ReadAllText("DimColor.json");
                    var dimColorModels = JsonConvert.DeserializeObject<List<ColorModel>>(dimColorJson);
                    foreach (var colorModel in dimColorModels)
                    {
                        var cache = new ColorResourceCache
                        {
                            Scheme = "低调色系",
                            Name = colorModel.Name,
                            Hex = colorModel.Hex
                        };
                        dataBase.Insert(cache);
                    }
                }
            });
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.RegisterSingleton<IAppDataService, AppDataServiceImpl>();

            //Navigation
            containerRegistry.RegisterForNavigation<TcpServerView, TcpServerViewModel>();
            containerRegistry.RegisterForNavigation<UdpServerView, UdpServerViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketServerView, WebSocketServerViewModel>();
            containerRegistry.RegisterForNavigation<ColorResourceView, ColorResourceViewModel>();

            //Window
            containerRegistry.RegisterDialog<AndroidDebugBridgeView, AndroidDebugBridgeViewModel>();
            containerRegistry.RegisterDialog<ApplicationPackageView, ApplicationPackageViewModel>();
            containerRegistry.RegisterDialog<TcpClientView, TcpClientViewModel>();
            containerRegistry.RegisterForNavigation<UdpClientView, UdpClientViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketClientView, WebSocketClientViewModel>();
            
            //Dialog
            containerRegistry.RegisterDialog<LoadingDialog, LoadingDialogViewModel>();
            containerRegistry.RegisterDialog<CreateKeyDialog, CreateKeyDialogViewModel>();
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
            containerRegistry.RegisterDialog<CommandScriptDialog, CommandScriptDialogViewModel>();
            containerRegistry.RegisterDialog<ScreenShotListDialog, ScreenShotListDialogViewModel>();
        }
    }
}
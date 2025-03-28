using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                using (var dataBase = new DataBaseConnection())
                {
                    if (!dataBase.Table<ColorResourceCache>().Any())
                    {
                        _ = StoreColorCacheAsync();
                    }
                }
            };
            return mainWindow;
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
            containerRegistry.RegisterForNavigation<AndroidDebugBridgeView, AndroidDebugBridgeViewModel>();
            containerRegistry.RegisterForNavigation<ApplicationPackageView, ApplicationPackageViewModel>();
            containerRegistry.RegisterForNavigation<TcpClientView, TcpClientViewModel>();
            containerRegistry.RegisterForNavigation<TcpServerView, TcpServerViewModel>();
            containerRegistry.RegisterForNavigation<UdpClientView, UdpClientViewModel>();
            containerRegistry.RegisterForNavigation<UdpServerView, UdpServerViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketClientView, WebSocketClientViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketServerView, WebSocketServerViewModel>();
            containerRegistry.RegisterForNavigation<SerialPortView, SerialPortViewModel>();
            containerRegistry.RegisterForNavigation<GenerateIconView, GenerateIconViewModel>();
            containerRegistry.RegisterForNavigation<QrCodeView, QrCodeViewModel>();
            containerRegistry.RegisterForNavigation<TranscodingView, TranscodingViewModel>();
            containerRegistry.RegisterForNavigation<ColorResourceView, ColorResourceViewModel>();

            //Dialog or Window
            containerRegistry.RegisterDialog<LoadingDialog, LoadingDialogViewModel>();
            containerRegistry.RegisterDialog<CreateKeyDialog, CreateKeyDialogViewModel>();
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
            containerRegistry.RegisterDialog<ScreenShotListDialog, ScreenShotListDialogViewModel>();
        }
    }
}
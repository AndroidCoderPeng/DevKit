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

                    if (!dataBase.Table<GradientColorResCache>().Any())
                    {
                        _ = StoreGradientColorCacheAsync();
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

        private async Task StoreGradientColorCacheAsync()
        {
            await Task.Run(() =>
            {
                using (var dataBase = new DataBaseConnection())
                {
                    var gradientColorJson = File.ReadAllText("GradientColor.json");
                    var gradientColorModels =
                        JsonConvert.DeserializeObject<List<GradientColorModel>>(gradientColorJson);
                    foreach (var colorModel in gradientColorModels)
                    {
                        if (colorModel.HexArray.Count == 2)
                        {
                            var cache = new GradientColorResCache
                            {
                                FirstColor = colorModel.HexArray[0],
                                SecondColor = colorModel.HexArray[1]
                            };
                            dataBase.Insert(cache);
                        }
                        else if (colorModel.HexArray.Count == 3)
                        {
                            var cache = new GradientColorResCache
                            {
                                FirstColor = colorModel.HexArray[0],
                                SecondColor = colorModel.HexArray[1],
                                ThirdColor = colorModel.HexArray[2]
                            };
                            dataBase.Insert(cache);
                        }
                        else if (colorModel.HexArray.Count == 4)
                        {
                            var cache = new GradientColorResCache
                            {
                                FirstColor = colorModel.HexArray[0],
                                SecondColor = colorModel.HexArray[1],
                                ThirdColor = colorModel.HexArray[2],
                                FourthColor = colorModel.HexArray[3]
                            };
                            dataBase.Insert(cache);
                        }
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
            containerRegistry.RegisterForNavigation<TcpCommunicateView, TcpCommunicateViewModel>();
            containerRegistry.RegisterForNavigation<UdpCommunicateView, UdpCommunicateViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketCommunicateView, WebSocketCommunicateViewModel>();
            containerRegistry.RegisterForNavigation<SerialPortCommunicateView, SerialPortCommunicateViewModel>();
            containerRegistry.RegisterForNavigation<GenerateIconView, GenerateIconViewModel>();
            containerRegistry.RegisterForNavigation<TranscodingView, TranscodingViewModel>();
            containerRegistry.RegisterForNavigation<ColorResourceView, ColorResourceViewModel>();

            //Dialog or Window
            containerRegistry.RegisterDialog<LoadingDialog, LoadingDialogViewModel>();
            containerRegistry.RegisterDialog<CreateKeyDialog, CreateKeyDialogViewModel>();
            containerRegistry.RegisterDialog<TcpClientMessageDialog, TcpClientMessageDialogViewModel>();

            //自定义Window容器，方便修改Window启动位置
            containerRegistry.RegisterDialogWindow<ExCommandWindow>("ExCommandWindow");
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
        }
    }
}
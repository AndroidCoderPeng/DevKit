using System;
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
            mainWindow.Loaded += async (sender, e) => 
            {
                using (var dataBase = new DataBaseConnection())
                {
                    if (!dataBase.Table<ColorResourceCache>().Any())
                    {
                        await InitializeColorDataAsync(dataBase);
                    }
                }
            };
            return mainWindow;
        }

        private async Task InitializeColorDataAsync(DataBaseConnection dataBase)
        {
            const string traditionColorFile = "TraditionColor.json";
            const string dimColorFile = "DimColor.json";

            var traditionColorModels = await DeserializeColorFileAsync(traditionColorFile, "中国传统色系");
            var dimColorModels = await DeserializeColorFileAsync(dimColorFile, "低调色系");

            if (traditionColorModels.Count > 0)
                dataBase.InsertAll(traditionColorModels);

            if (dimColorModels.Count > 0)
                dataBase.InsertAll(dimColorModels);
        }

        private async Task<List<ColorResourceCache>> DeserializeColorFileAsync(string filePath, string scheme)
        {
            using (var reader = new StreamReader(filePath))
            {
                var json = await reader.ReadToEndAsync();
                var models = JsonConvert.DeserializeObject<List<ColorModel>>(json);
                return models.Select(color => new ColorResourceCache
                {
                    Scheme = scheme,
                    Name = color.Name,
                    Hex = color.Hex
                }).ToList();
            }
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

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.RegisterSingleton<IAppDataService, AppDataServiceImpl>();

            //Window
            containerRegistry.RegisterDialog<AndroidDebugBridgeView, AndroidDebugBridgeViewModel>();
            containerRegistry.RegisterDialog<ApplicationPackageView, ApplicationPackageViewModel>();
            containerRegistry.RegisterDialog<TcpClientView, TcpClientViewModel>();
            containerRegistry.RegisterForNavigation<UdpClientView, UdpClientViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketClientView, WebSocketClientViewModel>();
            containerRegistry.RegisterForNavigation<TcpServerView, TcpServerViewModel>();
            containerRegistry.RegisterForNavigation<UdpServerView, UdpServerViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketServerView, WebSocketServerViewModel>();
            containerRegistry.RegisterForNavigation<ColorResourceView, ColorResourceViewModel>();

            //Dialog
            containerRegistry.RegisterDialog<LoadingDialog, LoadingDialogViewModel>();
            containerRegistry.RegisterDialog<CreateKeyDialog, CreateKeyDialogViewModel>();
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
            containerRegistry.RegisterDialog<CommandScriptDialog, CommandScriptDialogViewModel>();
            containerRegistry.RegisterDialog<ScreenShotListDialog, ScreenShotListDialogViewModel>();
        }
    }
}
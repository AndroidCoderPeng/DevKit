using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Dialogs;
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
            var traditionColorModels = await DeserializeColorFileAsync("Colors.json");
            if (traditionColorModels.Count > 0)
            {
                dataBase.InsertAll(traditionColorModels);
            }
        }

        private async Task<List<ColorResourceCache>> DeserializeColorFileAsync(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<ColorResourceCache>>(json);
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.RegisterSingleton<IAppDataService, AppDataServiceImpl>();

            //Dialog
            containerRegistry.RegisterDialog<AndroidDebugBridgeView, AndroidDebugBridgeViewModel>();
            containerRegistry.RegisterDialog<ScreenshotExportDialog, ScreenshotExportDialogViewModel>();
            
            containerRegistry.RegisterDialog<ApplicationPackageView, ApplicationPackageViewModel>();
            containerRegistry.RegisterDialog<AndroidLogcatDialog, AndroidLogcatDialogViewModel>();
            containerRegistry.RegisterDialog<JNIReverseView, JNIReverseViewModel>();
            
            containerRegistry.RegisterDialog<TcpClientView, TcpClientViewModel>();
            containerRegistry.RegisterDialog<TcpServerView, TcpServerViewModel>();
            containerRegistry.RegisterDialog<UdpClientView, UdpClientViewModel>();
            containerRegistry.RegisterDialog<UdpServerView, UdpServerViewModel>();
            containerRegistry.RegisterDialog<WebSocketClientView, WebSocketClientViewModel>();
            containerRegistry.RegisterDialog<WebSocketServerView, WebSocketServerViewModel>();
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
            containerRegistry.RegisterDialog<CommandScriptDialog, CommandScriptDialogViewModel>();
            
            containerRegistry.RegisterDialog<ColorResourceView, ColorResourceViewModel>();
            containerRegistry.RegisterDialog<NetConfigurationView, NetConfigurationViewModel>();
            containerRegistry.RegisterDialog<VideoCutView, VideoCutViewModel>();

            containerRegistry.RegisterDialog<LoadingDialog, LoadingDialogViewModel>();
        }
    }
}
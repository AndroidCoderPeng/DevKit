using System.Windows;
using DevKit.DataService;
using DevKit.Dialogs;
using DevKit.ViewModels;
using DevKit.Views;
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
            };
            return mainWindow;
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
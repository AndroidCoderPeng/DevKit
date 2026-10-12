using System.Windows;
using DevKit.DataService;
using DevKit.Dialogs;
using DevKit.ViewModels;
using DevKit.Views;
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
            return Container.Resolve<MainWindow>();
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
            containerRegistry.RegisterDialog<WsClientView, WsClientViewModel>();
            containerRegistry.RegisterDialog<WsServerView, WsServerViewModel>();
            containerRegistry.RegisterDialog<ExCommandDialog, ExCommandDialogViewModel>();
            containerRegistry.RegisterDialog<CommandScriptDialog, CommandScriptDialogViewModel>();
            
            containerRegistry.RegisterDialog<ColorResourceView, ColorResourceViewModel>();
            containerRegistry.RegisterDialog<NetConfigurationView, NetConfigurationViewModel>();
        }
    }
}
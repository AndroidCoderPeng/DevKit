using System;
using System.Runtime.Versioning;
using System.Windows;
using DevKit.DataService;
using DevKit.Dialogs;
using DevKit.Utils;
using DevKit.ViewModels;
using DevKit.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IAppDataService, AppDataServiceImpl>();

            // 
            // none module dialog registrations
            services.AddDialog<AndroidDebugBridgeView, AndroidDebugBridgeViewModel>();
            services.AddDialog<ApplicationPackageView, ApplicationPackageViewModel>();
            services.AddDialog<JniReverseView, JniReverseViewModel>();
            services.AddDialog<TcpClientView, TcpClientViewModel>();
            services.AddDialog<TcpServerView, TcpServerViewModel>();
            services.AddDialog<UdpClientView, UdpClientViewModel>();
            services.AddDialog<UdpServerView, UdpServerViewModel>();
            services.AddDialog<WsClientView, WsClientViewModel>();
            services.AddDialog<WsServerView, WsServerViewModel>();
            services.AddDialog<SerialPortView, SerialPortViewModel>();
            services.AddDialog<ColorResourceView, ColorResourceViewModel>();
            services.AddDialog<NetConfigurationView, NetConfigurationViewModel>();

            //  module dialog registrations
            services.AddDialog<ScreenshotExportDialog, ScreenshotExportDialogViewModel>();
            services.AddDialog<AndroidLogcatDialog, AndroidLogcatDialogViewModel>();
            services.AddDialog<ExCommandDialog, ExCommandDialogViewModel>();
            services.AddDialog<CommandScriptDialog, CommandScriptDialogViewModel>();

            // Window and ViewModel registrations
            services.AddSingleton<MainWindowViewModel>();

            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
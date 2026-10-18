using System;
using System.Windows;
using DevKit.Dialogs;
using DevKit.Utils;
using DevKit.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var services = new ServiceCollection();
            
            // Dialog service registration（对话框 ViewModel 则通常更适合 AddTransient）
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<ScreenshotExportDialog>();
            services.AddTransient<ScreenshotExportDialogViewModel>();
            services.AddTransient<AndroidLogcatDialog>();
            services.AddTransient<AndroidLogcatDialogViewModel>();
            services.AddTransient<ExCommandDialog>();
            services.AddTransient<ExCommandDialogViewModel>();
            services.AddTransient<CommandScriptDialog>();
            services.AddTransient<CommandScriptDialogViewModel>();
            
            // Window and ViewModel registrations
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<AndroidDebugBridgeViewModel>();
            services.AddTransient<ApplicationPackageViewModel>();
            services.AddTransient<JNIReverseViewModel>();
            services.AddTransient<TcpClientViewModel>();
            services.AddTransient<TcpServerViewModel>();
            services.AddTransient<UdpClientViewModel>();
            services.AddTransient<UdpServerViewModel>();
            services.AddTransient<WsClientViewModel>();
            services.AddTransient<WsServerViewModel>();
            services.AddTransient<SerialPortViewModel>();
            services.AddTransient<ColorResourceViewModel>();
            services.AddTransient<NetConfigurationViewModel>();
            
            Services = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Models;
using DevKit.Utils;
using DevKit.Views;

namespace DevKit.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private List<MainMenu> _androidTools;
        [ObservableProperty] private List<MainMenu> _socketTools;
        [ObservableProperty] private List<MainMenu> _otherTools;

        #endregion

        private readonly IDialogService _dialogService;
        
        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            
            AndroidTools = [
                new MainMenu { MenuIcon = "\ue71c", MenuName = "ADB", ViewType = typeof(AndroidDebugBridgeView) },
                new MainMenu { MenuIcon = "\ue700", MenuName = "APK", ViewType = typeof(ApplicationPackageView) },
                new MainMenu { MenuIcon = "\ue673", MenuName = "JNI逆向", ViewType = typeof(JNIReverseView) }
            ];
            
            SocketTools = [
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP客户端", ViewType = typeof(TcpClientView) },
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP服务端", ViewType = typeof(TcpServerView) },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP客户端", ViewType = typeof(UdpClientView) },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP服务端", ViewType = typeof(UdpServerView) },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS客户端", ViewType = typeof(WsClientView) },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS服务端", ViewType = typeof(WsServerView) },
                new MainMenu { MenuIcon = "\ue8f6", MenuName = "串口通信", ViewType = typeof(SerialPortView) },
                new MainMenu { MenuIcon = "\ue795", MenuName = "SFTP", ViewType = null }
            ];
            
            OtherTools = [
                new MainMenu { MenuIcon = "\ue660", MenuName = "颜色处理", ViewType = typeof(ColorResourceView) },
                new MainMenu { MenuIcon = "\ue6b4", MenuName = "网络配置", ViewType = typeof(NetConfigurationView) }
            ];
        }
        
        [RelayCommand]
        private void ToolItemClicked(MainMenu menu)
        {
            if (menu?.ViewType == null)
            {
                return;
            }

            // 显示独立的窗口（Window）——非模态
            _dialogService.Show(menu.ViewType);
        }
    }
}
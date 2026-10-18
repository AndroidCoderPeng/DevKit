using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Models;

namespace DevKit.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private List<MainMenu> _androidTools;
        [ObservableProperty] private List<MainMenu> _socketTools;
        [ObservableProperty] private List<MainMenu> _otherTools;

        #endregion

        public MainWindowViewModel()
        {
            AndroidTools = GetAndroidTools();
            SocketTools = GetSocketTools();
            OtherTools = GetOtherTools();
        }
        
        private List<MainMenu> GetAndroidTools()
        {
            return
            [
                new MainMenu { MenuIcon = "\ue71c", MenuName = "ADB", ViewName = "AndroidDebugBridgeView" },
                new MainMenu { MenuIcon = "\ue700", MenuName = "APK", ViewName = "ApplicationPackageView" },
                new MainMenu { MenuIcon = "\ue673", MenuName = "JNI逆向", ViewName = "JNIReverseView" }
            ];
        }

        private List<MainMenu> GetSocketTools()
        {
            return
            [
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP客户端", ViewName = "TcpClientView" },
                new MainMenu { MenuIcon = "\ue8a9", MenuName = "TCP服务端", ViewName = "TcpServerView" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP客户端", ViewName = "UdpClientView" },
                new MainMenu { MenuIcon = "\ue8ab", MenuName = "UDP服务端", ViewName = "UdpServerView" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS客户端", ViewName = "WsClientView" },
                new MainMenu { MenuIcon = "\ue8b2", MenuName = "WS服务端", ViewName = "WsServerView" },
                new MainMenu { MenuIcon = "\ue8f6", MenuName = "串口通信", ViewName = "SerialPortView" },
                new MainMenu { MenuIcon = "\ue795", MenuName = "SFTP", ViewName = null }
            ];
        }

        private List<MainMenu> GetOtherTools()
        {
            return
            [
                new MainMenu { MenuIcon = "\ue660", MenuName = "颜色处理", ViewName = "ColorResourceView" },
                new MainMenu { MenuIcon = "\ue6b4", MenuName = "网络配置", ViewName = "NetConfigurationView" }
            ];
        }
        
        [RelayCommand]
        private void ToolItemClicked(MainMenu menu)
        {
            if (menu == null || string.IsNullOrWhiteSpace(menu.ViewName))
            {
                return;
            }

            // 显示独立的窗口（Window）——非模态
            dialogService.Show(menu.ViewName);
        }
    }
}
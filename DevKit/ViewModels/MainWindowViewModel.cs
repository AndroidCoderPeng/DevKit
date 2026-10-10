using System.Collections.Generic;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenu> AndroidTools { get; }
        public List<MainMenu> SocketTools { get; }
        public List<MainMenu> OtherTools { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand<MainMenu> AndroidToolClickedCommand { get; }
        public DelegateCommand<MainMenu> SocketToolClickedCommand { get; }
        public DelegateCommand<MainMenu> OtherToolClickedCommand { get; }

        #endregion

        private readonly IDialogService _dialogService;

        public MainWindowViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;

            AndroidTools = dataService.GetAndroidTools();
            SocketTools = dataService.GetSocketTools();
            OtherTools = dataService.GetOtherTools();

            AndroidToolClickedCommand = new DelegateCommand<MainMenu>(OnAndroidToolClicked);
            SocketToolClickedCommand = new DelegateCommand<MainMenu>(OnSocketToolClicked);
            OtherToolClickedCommand = new DelegateCommand<MainMenu>(OnOtherToolClicked);
        }

        private readonly Dictionary<string, string> _androidToolMap = new Dictionary<string, string>
        {
            { "ADB", "AndroidDebugBridgeView" },
            { "APK", "ApplicationPackageView" },
            { "JNI逆向", "JNIReverseView" }
        };

        private void OnAndroidToolClicked(MainMenu menu)
        {
            if (menu == null || !_androidToolMap.TryGetValue(menu.MenuName, out var viewName)) return;
            _dialogService.Show(viewName);
        }

        private readonly Dictionary<string, string> _socketToolMap = new Dictionary<string, string>
        {
            { "TCP客户端", "TcpClientView" },
            { "TCP服务端", "TcpServerView" },
            { "UDP客户端", "UdpClientView" },
            { "UDP服务端", "UdpServerView" },
            { "WS客户端", "WebSocketClientView" },
            { "WS服务端", "WebSocketServerView" }
        };
        
        private void OnSocketToolClicked(MainMenu menu)
        {
            if (menu == null || !_socketToolMap.TryGetValue(menu.MenuName, out var viewName)) return;
            _dialogService.Show(viewName);
        }

        private readonly Dictionary<string, string> _otherToolMap = new Dictionary<string, string>
        {
            { "颜色处理", "ColorResourceView" },
            { "网络配置", "NetConfigurationView" },
            { "视频裁剪", "VideoCutView" }
        };
        
        private void OnOtherToolClicked(MainMenu menu)
        {
            if (menu == null || !_otherToolMap.TryGetValue(menu.MenuName, out var viewName)) return;
            _dialogService.Show(viewName);
        }
    }
}
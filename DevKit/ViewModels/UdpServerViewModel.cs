using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UdpServer=TouchSocket.Sockets.UdpSession;

namespace DevKit.ViewModels
{
    public class UdpServerViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<string> _localAddressCollection = new ObservableCollection<string>();

        public ObservableCollection<string> LocalAddressCollection
        {
            set
            {
                _localAddressCollection = value;
                RaisePropertyChanged();
            }
            get => _localAddressCollection;
        }

        private string _listenStateColor = "DarkGray";

        public string ListenStateColor
        {
            set
            {
                _listenStateColor = value;
                RaisePropertyChanged();
            }
            get => _listenStateColor;
        }

        private int _listenPort = 3030;

        public int ListenPort
        {
            set
            {
                _listenPort = value;
                RaisePropertyChanged();
            }
            get => _listenPort;
        }

        private string _listenState = "监听";

        public string ListenState
        {
            set
            {
                _listenState = value;
                RaisePropertyChanged();
            }
            get => _listenState;
        }

        private ObservableCollection<ConnectedClientModel> _clientCollection =
            new ObservableCollection<ConnectedClientModel>();

        public ObservableCollection<ConnectedClientModel> ClientCollection
        {
            set
            {
                _clientCollection = value;
                RaisePropertyChanged();
            }
            get => _clientCollection;
        }

        private string _isContentViewVisible = "Collapsed";

        public string IsContentViewVisible
        {
            set
            {
                _isContentViewVisible = value;
                RaisePropertyChanged();
            }
            get => _isContentViewVisible;
        }

        private string _isEmptyImageVisible = "Visible";

        public string IsEmptyImageVisible
        {
            set
            {
                _isEmptyImageVisible = value;
                RaisePropertyChanged();
            }
            get => _isEmptyImageVisible;
        }
        
        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<ConnectedClientModel> ClientItemSelectionChangedCommand { set; get; }

        #endregion
        
        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly UdpServer _udpServer = new UdpServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private bool _isListening;
        private ConnectedClientModel _connectedClient;

        public UdpServerViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();
        }

        private void InitDefaultConfig()
        {
            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();

            _udpServer.Received = (client, e) =>
            {
                var endPoint = e.EndPoint;
                if (!_clientCollection.Any(udp =>
                        udp.Ip == endPoint.GetIP() &&
                        udp.Port == endPoint.GetPort()))
                {
                    var clientModel = new ConnectedClientModel
                    {
                        Ip = endPoint.GetIP(),
                        Port = endPoint.GetPort()
                    };

                    Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                }

                foreach (var udp in _clientCollection)
                {
                    if (udp.Ip == endPoint.GetIP() &&
                        udp.Port == endPoint.GetPort())
                    {
                        var bytes = e.ByteBlock.ToArray();
                        
                    }
                }

                return EasyTask.CompletedTask;
            };
        }
        
        private void ServerListen()
        {
            if (_isListening)
            {
                if (RuntimeCache.IsClientViewShowing)
                {
                    MessageBox.Show("客户端已打开，无法停止监听", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _udpServer.Stop();
                _isListening = false;
                ListenState = "监听";
                ListenStateColor = "DarkGray";
            }
            else
            {
                try
                {
                    _udpServer.Setup(new TouchSocketConfig().SetBindIPHost(new IPHost(_listenPort)));
                    _udpServer.Start();
                    _isListening = true;
                    ListenState = "停止";
                    ListenStateColor = "LimeGreen";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClientItemDoubleClick(ConnectedClientModel client)
        {
            if (client == null)
            {
                return;
            }
            
            if (RuntimeCache.IsClientViewShowing)
            {
                MessageBox.Show("请勿重复打开消息界面", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _connectedClient = client;
        }
    }
}
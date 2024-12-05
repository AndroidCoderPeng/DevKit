using System;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using WebSocketServer = TouchSocket.Http.HttpService;

namespace DevKit.ViewModels
{
    public class WebSocketServerViewModel : BindableBase
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

        private int _comboBoxSelectedIndex;

        public int ComboBoxSelectedIndex
        {
            set
            {
                _comboBoxSelectedIndex = value;
                RaisePropertyChanged();
            }
            get => _comboBoxSelectedIndex;
        }

        private int _listenPort = 9000;

        public int ListenPort
        {
            set
            {
                _listenPort = value;
                RaisePropertyChanged();
            }
            get => _listenPort;
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

        private string _requestPath = string.Empty;

        public string RequestPath
        {
            set
            {
                _requestPath = value;
                RaisePropertyChanged();
            }
            get => _requestPath;
        }

        private string _webSocketUrl = string.Empty;

        public string WebSocketUrl
        {
            set
            {
                _webSocketUrl = value;
                RaisePropertyChanged();
            }
            get => _webSocketUrl;
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

        private string _connectedClientAddress = string.Empty;

        public string ConnectedClientAddress
        {
            set
            {
                _connectedClientAddress = value;
                RaisePropertyChanged();
            }
            get => _connectedClientAddress;
        }

        private bool _showHex = true;

        public bool ShowHex
        {
            set
            {
                _showHex = value;
                RaisePropertyChanged();
            }
            get => _showHex;
        }

        private ObservableCollection<MessageModel> _messageCollection = new ObservableCollection<MessageModel>();

        public ObservableCollection<MessageModel> MessageCollection
        {
            set
            {
                _messageCollection = value;
                RaisePropertyChanged();
            }
            get => _messageCollection;
        }

        private ObservableCollection<ExCommandCache> _exCommandCollection = new ObservableCollection<ExCommandCache>();

        public ObservableCollection<ExCommandCache> ExCommandCollection
        {
            set
            {
                _exCommandCollection = value;
                RaisePropertyChanged();
            }
            get => _exCommandCollection;
        }

        private bool _loopSend;

        public bool LoopSend
        {
            set
            {
                _loopSend = value;
                RaisePropertyChanged();
            }
            get => _loopSend;
        }

        private long _commandInterval = 1000;

        public long CommandInterval
        {
            set
            {
                _commandInterval = value;
                RaisePropertyChanged();
            }
            get => _commandInterval;
        }

        private string _userInputText = string.Empty;

        public string UserInputText
        {
            set
            {
                _userInputText = value;
                RaisePropertyChanged();
            }
            get => _userInputText;
        }

        private bool _sendHex = true;

        public bool SendHex
        {
            set
            {
                _sendHex = value;
                RaisePropertyChanged();
            }
            get => _sendHex;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<ConnectedClientModel> ClientItemSelectionChangedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly WebSocketServer _webSocketServer = new WebSocketServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private bool _isListening;
        private ConnectedClientModel _connectedClient;

        public WebSocketServerViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ServerListenCommand = new DelegateCommand(ServerListen);
            // ClientItemSelectionChangedCommand = new DelegateCommand<ConnectedClientModel>(ClientItemSelectionChanged);
            // LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            // ClearMessageCommand = new DelegateCommand(ClearMessage);
            // SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpServer)
                .ToObservableCollection();

            // _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;
        }

        private void ServerListen()
        {
            if (_isListening)
            {
                _webSocketServer.Stop();
                _isListening = false;
                ListenState = "监听";
                ListenStateColor = "DarkGray";
                WebSocketUrl = string.Empty;
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig()
                        .SetListenIPHosts(_listenPort)
                        .ConfigurePlugins(cfg => { cfg.UseWebSocket().SetWSUrl($"/{_requestPath}"); });
                    _webSocketServer.Setup(socketConfig);
                    _webSocketServer.Start();
                    _isListening = true;
                    ListenState = "停止";
                    ListenStateColor = "LimeGreen";
                    WebSocketUrl =
                        $"ws://{_localAddressCollection[_comboBoxSelectedIndex]}:{_listenPort}/{_requestPath}";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
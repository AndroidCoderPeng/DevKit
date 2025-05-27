using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using Timer = System.Timers.Timer;
using WebSocketServer = TouchSocket.Http.HttpService;

namespace DevKit.ViewModels
{
    public class WebSocketServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "WebSocket服务端";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            if (_webSocketServer.ServerState != ServerState.Running) return;
            _webSocketServer.Stop();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _localHost = string.Empty;

        public string LocalHost
        {
            set
            {
                _localHost = value;
                RaisePropertyChanged();
            }
            get => _localHost;
        }

        private string _listenPort = "9000";

        public string ListenPort
        {
            set
            {
                _listenPort = value;
                RaisePropertyChanged();
            }
            get => _listenPort;
        }

        private string _customPath = string.Empty;

        public string CustomPath
        {
            set
            {
                _customPath = value;
                RaisePropertyChanged();
            }
            get => _customPath;
        }

        private string _webSocketPath = string.Empty;

        public string WebSocketPath
        {
            set
            {
                _webSocketPath = value;
                RaisePropertyChanged();
            }
            get => _webSocketPath;
        }

        private string _listenStateColor = "LightGray";

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

        private ObservableCollection<WebSocketClientModel> _clients = new ObservableCollection<WebSocketClientModel>();

        public ObservableCollection<WebSocketClientModel> Clients
        {
            set
            {
                _clients = value;
                RaisePropertyChanged();
            }
            get => _clients;
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

        private string _clientAddress = string.Empty;

        public string ClientAddress
        {
            set
            {
                _clientAddress = value;
                RaisePropertyChanged();
            }
            get => _clientAddress;
        }

        private ObservableCollection<LogModel> _logs = new ObservableCollection<LogModel>();

        public ObservableCollection<LogModel> Logs
        {
            set
            {
                _logs = value;
                RaisePropertyChanged();
            }
            get => _logs;
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

        private string _commandInterval = "1000";

        public string CommandInterval
        {
            set
            {
                _commandInterval = value;
                RaisePropertyChanged();
            }
            get => _commandInterval;
        }

        private bool _isHexSelected = true;

        public bool IsHexSelected
        {
            set
            {
                _isHexSelected = value;
                RaisePropertyChanged();
            }
            get => _isHexSelected;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand CopyWebSocketPathCommand { set; get; }
        public DelegateCommand<WebSocketClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private readonly WebSocketServer _webSocketServer = new WebSocketServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private WebSocketClientModel _selectedClient;

        public WebSocketServerViewModel(IAppDataService dataService)
        {
            LocalHost = dataService.GetIPv4Address();

            ServerListenCommand = new DelegateCommand(OnServerListened);
            CopyWebSocketPathCommand = new DelegateCommand(CopyWebSocketPath);
            ClientItemClickedCommand = new DelegateCommand<WebSocketClientModel>(OnClientItemClicked);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
        }

        private void OnServerListened()
        {
            if (!_listenPort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_webSocketServer.ServerState == ServerState.Running)
            {
                _webSocketServer.Stop();
                ListenState = "监听";
                ListenStateColor = "LightGray";
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig()
                        .SetListenIPHosts(Convert.ToInt32(_listenPort))
                        .ConfigurePlugins(plugin =>
                        {
                            plugin.UseWebSocket().SetWSUrl(_customPath);
                            InitListenStateEvent(plugin);
                        });
                    _webSocketServer.Setup(socketConfig);
                    _webSocketServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                    WebSocketPath = $"ws://{_localHost}:{_listenPort}/{_customPath}";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitListenStateEvent(IPluginManager plugin)
        {
            plugin.AddWebSocketHandshakedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var model = new WebSocketClientModel
                {
                    Ip = session.IP,
                    Port = session.Port,
                    WebSocket = websocket,
                    IsConnected = true
                };

                OnClientItemClicked(model);
                Application.Current.Dispatcher.Invoke(() => { Clients.Add(_selectedClient); });
                await EasyTask.CompletedTask;
            });
            plugin.AddWebSocketReceivedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var webSocketClient = _clients.FirstOrDefault(x => x.Ip == session.IP && x.Port == session.Port);
                if (webSocketClient != null)
                {
                    //默认显示为UTF8编码
                    var log = new LogModel
                    {
                        Content = e.DataFrame.ToText(),
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = 0
                    };
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        webSocketClient.MessageCount++;
                        webSocketClient.Logs.Add(log);
                    });
                }

                await EasyTask.CompletedTask;
            });
            plugin.AddWebSocketClosedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var webSocketClient = Clients.FirstOrDefault(x => x.Ip == session.IP && x.Port == session.Port);
                if (webSocketClient != null)
                {
                    webSocketClient.IsConnected = false;
                }

                await EasyTask.CompletedTask;
            });
        }

        private void CopyWebSocketPath()
        {
            if (string.IsNullOrWhiteSpace(_webSocketPath))
            {
                MessageBox.Show("请先开启监听", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Clipboard.SetText(_webSocketPath);
        }

        private void OnClientItemClicked(WebSocketClientModel client)
        {
            _selectedClient = client;
            if (_selectedClient != null)
            {
                IsContentViewVisible = "Visible";
                IsEmptyImageVisible = "Collapsed";

                client.MessageCount = 0;
                ClientAddress = $"{client.Ip}:{client.Port}";
                // 显示当前选中客户端的消息
                Logs = _selectedClient.Logs;
            }
        }

        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
        }

        private void SendMessage()
        {
            // if (string.IsNullOrWhiteSpace(_userInputText))
            // {
            //     MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // if (_loopSend)
            // {
            //     if (!_commandInterval.IsNumber())
            //     {
            //         MessageBox.Show("循环发送时间间隔数据格式错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //         return;
            //     }
            //
            //     _loopSendMessageTimer.Interval = double.Parse(_commandInterval);
            //     _loopSendMessageTimer.Enabled = true;
            // }
            // else
            // {
            //     Send(true);
            // }
        }

        private void Send(bool isMainThread)
        {
            // _socketClient.WebSocket?.SendAsync(_userInputText);
            //     
            // // using (var dataBase = new DataBaseConnection())
            // // {
            // //     var cache = new ClientMessageCache
            // //     {
            // //         ClientIp = _connectedClient.Ip,
            // //         ClientPort = _connectedClient.Port,
            // //         ClientType = ConnectionType.WebSocketClient,
            // //         MessageContent = _userInputText,
            // //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            // //         IsSend = 1
            // //     };
            // //     dataBase.Insert(cache);
            // // }
            //     
            // var message = new MessageModel
            // {
            //     Content = _userInputText,
            //     Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //     IsSend = true
            // };
            //
            // if (isMainThread)
            // {
            //     MessageCollection.Add(message);
            // }
            // else
            // {
            //     Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
            // }
        }
    }
}
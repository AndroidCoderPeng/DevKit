using System;
using System.Collections.ObjectModel;
using System.Linq;
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
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using WebSocketServer = TouchSocket.Http.HttpService;

namespace DevKit.ViewModels
{
    public class WebSocketServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "WS服务端";

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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
        
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

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand CopyWebSocketUrlCommand { set; get; }
        public DelegateCommand<ConnectedClientModel> ClientItemSelectionChangedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly WebSocketServer _webSocketServer = new WebSocketServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private ConnectedClientModel _connectedClient;

        public WebSocketServerViewModel(IAppDataService dataService)
        {
            //获取本机所有IPv4地址
            LocalAddressCollection = dataService.GetAllIPv4Addresses().ToObservableCollection();
            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            ServerListenCommand = new DelegateCommand(ServerListen);
            CopyWebSocketUrlCommand = new DelegateCommand(CopyWebSocketUrl);
            ClientItemSelectionChangedCommand = new DelegateCommand<ConnectedClientModel>(ClientItemSelectionChanged);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void ServerListen()
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
                WebSocketUrl = string.Empty;
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig()
                        .SetListenIPHosts(int.Parse(ListenPort))
                        .ConfigurePlugins(cfg =>
                        {
                            cfg.UseWebSocket().SetWSUrl($"/{_requestPath}");

                            //连接
                            cfg.Add(typeof(IWebSocketHandshakedPlugin),
                                (IWebSocket webSocket, HttpContextEventArgs e) =>
                                {
                                    var session = webSocket.Client;
                                    var clientModel = new ConnectedClientModel
                                    {
                                        Ip = session.IP,
                                        Port = session.Port,
                                        WebSocket = webSocket,
                                        IsConnected = true
                                    };

                                    Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                                    return EasyTask.CompletedTask;
                                });

                            //收到消息
                            cfg.Add(typeof(IWebSocketReceivedPlugin), (IWebSocket webSocket, WSDataFrameEventArgs e) =>
                            {
                                var session = webSocket.Client;
                                var client = _clientCollection.First(x => x.Ip == session.IP && x.Port == session.Port);
                                client.MessageCount++;
                                using (var dataBase = new DataBaseConnection())
                                {
                                    var cache = new ClientMessageCache
                                    {
                                        ClientIp = session.IP,
                                        ClientPort = session.Port,
                                        ClientType = ConnectionType.WebSocketClient,
                                        MessageContent = e.DataFrame.ToText(),
                                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                                        IsSend = 0
                                    };
                                    dataBase.Insert(cache);
                                }

                                if (_isContentViewVisible.Equals("Visible") &&
                                    _connectedClient.Ip == session.IP && _connectedClient.Port == session.Port)
                                {
                                    var messageModel = new MessageModel
                                    {
                                        Content = e.DataFrame.ToText(),
                                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                                        IsSend = false
                                    };

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MessageCollection.Add(messageModel);
                                    });
                                }

                                return EasyTask.CompletedTask;
                            });

                            //断开
                            cfg.Add(typeof(IWebSocketClosedPlugin), (IWebSocket webSocket, ClosedEventArgs e) =>
                            {
                                var session = webSocket.Client;
                                _clientCollection.First(
                                    x => x.Ip == session.IP && x.Port == session.Port
                                ).IsConnected = false;
                                return EasyTask.CompletedTask;
                            });
                        });
                    _webSocketServer.Setup(socketConfig);
                    _webSocketServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                    WebSocketUrl =
                        $"ws://{_localAddressCollection[_comboBoxSelectedIndex]}:{_listenPort}/{_requestPath}";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyWebSocketUrl()
        {
            if (string.IsNullOrWhiteSpace(_webSocketUrl))
            {
                MessageBox.Show("请先开启监听", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Clipboard.SetText(_webSocketUrl);
        }

        private void ClientItemSelectionChanged(ConnectedClientModel client)
        {
            if (client == null)
            {
                return;
            }

            client.MessageCount = 0;
            _connectedClient = client;
            ConnectedClientAddress = $"{client.Ip}:{client.Port}";
            MessageCollection.Clear();
            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ClientMessageCache>()
                    .Where(x =>
                        x.ClientIp == _connectedClient.Ip &&
                        x.ClientPort == _connectedClient.Port &&
                        x.ClientType == ConnectionType.WebSocketClient
                    );
                if (queryResult.Any())
                {
                    IsContentViewVisible = "Visible";
                    IsEmptyImageVisible = "Collapsed";

                    foreach (var cache in queryResult)
                    {
                        var messageModel = new MessageModel
                        {
                            Content = cache.MessageContent,
                            Time = cache.Time,
                            IsSend = cache.IsSend == 1
                        };

                        MessageCollection.Add(messageModel);
                    }
                }
                else
                {
                    IsContentViewVisible = "Collapsed";
                    IsEmptyImageVisible = "Visible";
                }
            }
        }

        private void LoopUnchecked()
        {
            _loopSendMessageTimer.Enabled = false;
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            Send(false);
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
            _connectedClient.MessageCount = 0;
            using (var dataBase = new DataBaseConnection())
            {
                dataBase.Table<ClientMessageCache>().Where(x =>
                    x.ClientIp == _connectedClient.Ip &&
                    x.ClientPort == _connectedClient.Port &&
                    x.ClientType == ConnectionType.WebSocketClient
                ).Delete();
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_loopSend)
            {
                if (!_commandInterval.IsNumber())
                {
                    MessageBox.Show("循环发送时间间隔数据格式错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _loopSendMessageTimer.Interval = double.Parse(_commandInterval);
                _loopSendMessageTimer.Enabled = true;
            }
            else
            {
                Send(true);
            }
        }

        private void Send(bool isMainThread)
        {
            _connectedClient.WebSocket?.SendAsync(_userInputText);
                
            using (var dataBase = new DataBaseConnection())
            {
                var cache = new ClientMessageCache
                {
                    ClientIp = _connectedClient.Ip,
                    ClientPort = _connectedClient.Port,
                    ClientType = ConnectionType.WebSocketClient,
                    MessageContent = _userInputText,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = 1
                };
                dataBase.Insert(cache);
            }
                
            var message = new MessageModel
            {
                Content = _userInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
           
            if (isMainThread)
            {
                MessageCollection.Add(message);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
            }
        }
    }
}
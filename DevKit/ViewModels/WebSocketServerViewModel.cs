using System;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
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
                ListenStateColor = "DarkGray";
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
                            cfg.Add(typeof(IWebSocketHandshakedPlugin),
                                (IWebSocket webSocket, HttpContextEventArgs e) =>
                                {
                                    var session = webSocket.Client;
                                    var clientModel = new ConnectedClientModel
                                    {
                                        Ip = session.IP,
                                        Port = session.Port,
                                        ConnectedWebSocket = webSocket,
                                        IsConnected = true
                                    };

                                    Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                                    return EasyTask.CompletedTask;
                                });

                            cfg.Add(typeof(IWebSocketReceivedPlugin), (IWebSocket webSocket, WSDataFrameEventArgs e) =>
                            {
                                var session = webSocket.Client;
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (var client in _clientCollection)
                                    {
                                        if (client.Ip == session.IP && client.Port == session.Port)
                                        {
                                            if (_isEmptyImageVisible.Equals("Visible"))
                                            {
                                                client.TextMsgCollection.Add(e.DataFrame.ToText());
                                                client.MessageCount++;
                                            }

                                            if (_isContentViewVisible.Equals("Visible"))
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

                                            break;
                                        }
                                    }

                                    MessageCollection.Clear();
                                });
                                return EasyTask.CompletedTask;
                            });

                            cfg.Add(typeof(IWebSocketClosedPlugin), (IWebSocket webSocket, ClosedEventArgs e) =>
                            {
                                var session = webSocket.Client;
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (var client in _clientCollection)
                                    {
                                        if (client.Ip == session.IP && client.Port == session.Port)
                                        {
                                            client.IsConnected = false;
                                            client.MessageCount = 0;
                                            client.ConnectedWebSocket = null;
                                            client.MessageCollection.Clear();
                                            //显示空白图
                                            IsContentViewVisible = "Collapsed";
                                            IsEmptyImageVisible = "Visible";
                                            break;
                                        }
                                    }

                                    MessageCollection.Clear();
                                });
                                return EasyTask.CompletedTask;
                            });
                        });
                    _webSocketServer.Setup(socketConfig);
                    _webSocketServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "LimeGreen";
                    WebSocketUrl =
                        $"ws://{_localAddressCollection[_comboBoxSelectedIndex]}:{_listenPort}/{_requestPath}";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
            if (client.IsConnected)
            {
                IsContentViewVisible = "Visible";
                IsEmptyImageVisible = "Collapsed";

                foreach (var msg in client.TextMsgCollection)
                {
                    var messageModel = new MessageModel
                    {
                        Content = msg,
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = false
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

        private void LoopUnchecked()
        {
            _loopSendMessageTimer.Enabled = false;
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            _connectedClient.ConnectedWebSocket?.SendAsync(_userInputText);
            var message = new MessageModel
            {
                Content = _userInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
            _connectedClient.MessageCollection.Clear();
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
                _connectedClient.ConnectedWebSocket?.SendAsync(_userInputText);
                var message = new MessageModel
                {
                    Content = _userInputText,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = true
                };
                MessageCollection.Add(message);
            }
        }
    }
}
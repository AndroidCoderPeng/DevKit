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
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using WebSocketClient = TouchSocket.Http.WebSockets.WebSocketClient;

namespace DevKit.ViewModels
{
    public class WebSocketClientViewModel : BindableBase
    {
        #region VM

        private string _remoteAddress = string.Empty;

        public string RemoteAddress
        {
            set
            {
                _remoteAddress = value;
                RaisePropertyChanged();
            }
            get => _remoteAddress;
        }

        private string _buttonState = "连接";

        public string ButtonState
        {
            set
            {
                _buttonState = value;
                RaisePropertyChanged();
            }
            get => _buttonState;
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

        private string _connectionStateColor = "DarkGray";

        public string ConnectionStateColor
        {
            set
            {
                _connectionStateColor = value;
                RaisePropertyChanged();
            }
            get => _connectionStateColor;
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

        private int _listenPort;

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

        private ObservableCollection<ConnectedClientModel> _websocketClientCollection =
            new ObservableCollection<ConnectedClientModel>();

        public ObservableCollection<ConnectedClientModel> WebSocketClientCollection
        {
            set
            {
                _websocketClientCollection = value;
                RaisePropertyChanged();
            }
            get => _websocketClientCollection;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly WebSocketClient _webSocketClient = new WebSocketClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private ClientConfigCache _clientCache;

        public WebSocketClientViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            InitDefaultConfig();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.WebSocketClient);
            RemoteAddress = _clientCache.RemoteAddress;

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            _webSocketClient.Handshaked = (client, e) =>
            {
                ConnectionStateColor = "LimeGreen";
                ButtonState = "断开";
                return EasyTask.CompletedTask;
            };

            _webSocketClient.Received = (client, e) =>
            {
                var messageModel = new MessageModel
                {
                    Content = e.DataFrame.ToText(),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                return EasyTask.CompletedTask;
            };

            _webSocketClient.Closed = (client, e) =>
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
                return EasyTask.CompletedTask;
            };

            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();
        }

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress))
            {
                MessageBox.Show("WebSocket服务端地址未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_webSocketClient.Online)
            {
                _webSocketClient.Close();
            }
            else
            {
                _webSocketClient.Setup(new TouchSocketConfig().SetRemoteIPHost($"{_remoteAddress}"));
                try
                {
                    _webSocketClient.Connect();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void LoopUnchecked()
        {
            Console.WriteLine(@"取消循环发送指令");
            _loopSendMessageTimer.Enabled = false;
        }

        private void SendMessage()
        {
            _clientCache.Type = ConnectionType.WebSocketClient;
            _clientCache.RemoteAddress = _remoteAddress;
            _dataService.SaveCacheConfig(_clientCache);

            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_buttonState.Equals("连接"))
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_loopSend)
            {
                Console.WriteLine(@"开启循环发送指令");
                _loopSendMessageTimer.Interval = _commandInterval;
                _loopSendMessageTimer.Enabled = true;
            }
            else
            {
                _webSocketClient.SendAsync(_userInputText);
                var message = new MessageModel
                {
                    Content = _userInputText,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = true
                };
                MessageCollection.Add(message);
            }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_buttonState.Equals("连接"))
            {
                Console.WriteLine(@"WebSocket未连接");
                return;
            }

            _webSocketClient.SendAsync(_userInputText);
            var message = new MessageModel
            {
                Content = _userInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
        }
    }
}
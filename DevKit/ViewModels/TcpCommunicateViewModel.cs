using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using DevKit.Utils.Socket.Server;
using DotNetty.Transport.Channels;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TcpClient = DevKit.Utils.Socket.Client.TcpClient;

namespace DevKit.ViewModels
{
    public class TcpCommunicateViewModel : BindableBase
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

        private string _remotePort = string.Empty;

        public string RemotePort
        {
            set
            {
                _remotePort = value;
                RaisePropertyChanged();
            }
            get => _remotePort;
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

        private ObservableCollection<TcpClientModel> _tcpClientCollection = new ObservableCollection<TcpClientModel>();

        public ObservableCollection<TcpClientModel> TcpClientCollection
        {
            set
            {
                _tcpClientCollection = value;
                RaisePropertyChanged();
            }
            get => _tcpClientCollection;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }
        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<TcpClientModel> ClientItemDoubleClickCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private readonly TcpServer _tcpServer = new TcpServer();
        private ClientConfigCache _clientCache;

        public TcpCommunicateViewModel(IAppDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            SendMessageCommand = new DelegateCommand(SendMessage);
            ServerListenCommand = new DelegateCommand(ServerListen);
            ClientItemDoubleClickCommand = new DelegateCommand<TcpClientModel>(ClientItemDoubleClick);

            eventAggregator.GetEvent<ExecuteExCommandEvent>().Subscribe(delegate(string commandValue)
            {
                if (_buttonState.Equals("连接"))
                {
                    MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var message = new MessageModel();
                if (_clientCache.SendHex == 1)
                {
                    if (!commandValue.IsHex())
                    {
                        MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                _tcpClient.SendAsync(commandValue);

                message.Content = commandValue;
                message.Time = DateTime.Now.ToString("HH:mm:ss.fff");
                message.IsSend = true;
                MessageCollection.Add(message);
            });
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.TcpClient);
            RemoteAddress = _clientCache.RemoteAddress;
            RemotePort = _clientCache.RemotePort.ToString();
            ShowHex = _clientCache.ShowHex == 1;
            SendHex = _clientCache.SendHex == 1;

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            _tcpClient.OnConnected += delegate
            {
                ConnectionStateColor = "LimeGreen";
                ButtonState = "断开";
            };
            _tcpClient.OnDisconnected += delegate
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
            };
            _tcpClient.OnConnectFailed += delegate(object sender, Exception exception)
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
                MessageBox.Show(exception.Message, "出错了", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            _tcpClient.OnDataReceived += delegate(object sender, byte[] bytes)
            {
                var messageModel = new MessageModel
                {
                    Content = _clientCache.ShowHex == 1
                        ? BitConverter.ToString(bytes).Replace("-", " ")
                        : Encoding.UTF8.GetString(bytes),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
            };

            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();

            _tcpServer.OnConnected += delegate(object sender, IChannelHandlerContext context)
            {
                var address = context.Channel.RemoteAddress;
                if (address is IPEndPoint endPoint)
                {
                    var clientModel = new TcpClientModel();

                    var iPv4 = endPoint.Address.MapToIPv4();
                    clientModel.Ip = iPv4.ToString();
                    clientModel.Port = endPoint.Port;

                    Application.Current.Dispatcher.Invoke(() => { TcpClientCollection.Add(clientModel); });
                }
            };
            _tcpServer.OnDisconnected += delegate(object sender, IChannelHandlerContext context)
            {
                var address = context.Channel.RemoteAddress;
                if (address is IPEndPoint endPoint)
                {
                    var iPv4 = endPoint.Address.MapToIPv4();
                    var port = endPoint.Port;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TcpClientModel clientModel = null;
                        foreach (var client in _tcpClientCollection)
                        {
                            if (client.Ip == iPv4.ToString() && client.Port == port)
                            {
                                clientModel = client;
                                break;
                            }
                        }

                        if (clientModel != null)
                        {
                            TcpClientCollection.Remove(clientModel);
                        }
                    });
                }
            };
            _tcpServer.OnDataReceived += delegate(object sender, IChannelHandlerContext context, byte[] bytes)
            {
                var address = context.Channel.RemoteAddress;
                if (address is IPEndPoint endPoint)
                {
                    var iPv4 = endPoint.Address.MapToIPv4();
                    var port = endPoint.Port;

                    foreach (var client in _tcpClientCollection)
                    {
                        if (client.Ip == iPv4.ToString() && client.Port == port)
                        {
                            client.MessageCount++;
                            break;
                        }
                    }
                }
            };
        }

        private void ShowHexChecked()
        {
            var boxResult = MessageBox.Show(
                "切换到HEX显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            );
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为string
                    var hex = model.Content.StringToHex();
                    var msg = new MessageModel
                    {
                        Content = hex.Replace("-", " "),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;

                _clientCache.ShowHex = 1;
            }
        }

        private void ShowHexUnchecked()
        {
            var boxResult = MessageBox.Show("确定切换到字符串显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为Hex，先转bytes[]，再转string
                    var bytes = model.Content.HexToBytes();
                    var msg = new MessageModel
                    {
                        Content = bytes.ByteArrayToString(),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;

                _clientCache.ShowHex = 0;
            }
        }

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_tcpClient.IsRunning())
            {
                _tcpClient.Close();
            }
            else
            {
                _tcpClient.Start(_remoteAddress, Convert.ToInt32(_remotePort));
            }
        }

        private void AddExtensionCommand()
        {
            var dialogParameters = new DialogParameters
            {
                { "ParentId", _clientCache.Id },
                { "ConnectionType", ConnectionType.TcpClient }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { }, "ExCommandWindow");
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void SendHexChecked()
        {
            _clientCache.SendHex = 1;
        }

        private void SendHexUnchecked()
        {
            _clientCache.SendHex = 0;
        }

        private void LoopUnchecked()
        {
            Console.WriteLine(@"取消循环发送指令");
            _loopSendMessageTimer.Enabled = false;
        }

        private void SendMessage()
        {
            _clientCache.Type = ConnectionType.TcpClient;
            _clientCache.RemoteAddress = _remoteAddress;
            _clientCache.RemotePort = Convert.ToInt32(_remotePort);
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
                var message = new MessageModel();
                if (_clientCache.SendHex == 1)
                {
                    if (!_userInputText.IsHex())
                    {
                        MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                _tcpClient.SendAsync(_userInputText);

                message.Content = _userInputText;
                message.Time = DateTime.Now.ToString("HH:mm:ss.fff");
                message.IsSend = true;
                MessageCollection.Add(message);
            }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_buttonState.Equals("连接"))
            {
                Console.WriteLine(@"TCP未连接");
                return;
            }

            var message = new MessageModel();
            if (_clientCache.SendHex == 1)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            _tcpClient.SendAsync(_userInputText);
            message.Content = _userInputText;
            message.Time = DateTime.Now.ToString("HH:mm:ss.fff");
            message.IsSend = true;
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
        }

        private void ServerListen()
        {
            if (_tcpServer.IsRunning())
            {
                _tcpServer.StopListen();
            }
            else
            {
                _tcpServer.StartListen(_listenPort, ListenStateHandler);
            }
        }

        private void ListenStateHandler(int state)
        {
            if (state == 1)
            {
                ListenState = "停止";
                ListenStateColor = "LimeGreen";
            }
            else
            {
                ListenState = "监听";
                ListenStateColor = "DarkGray";

                //断开客户端（没找到服务端断开监听，只能在服务端断开时候主动调一下客户端关闭）
                _tcpClient.Close();
            }
        }

        private void ClientItemDoubleClick(TcpClientModel clientModel)
        {
            var dialogParameters = new DialogParameters
            {
                { "TcpClientModel", clientModel }
            };
            _dialogService.Show("TcpClientMessageDialog", dialogParameters, delegate { });
        }
    }
}
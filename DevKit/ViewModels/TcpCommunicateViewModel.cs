using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using TcpClient = TouchSocket.Sockets.TcpClient;
using TcpServer = TouchSocket.Sockets.TcpService;

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

        private bool _clientShowHex = true;

        public bool ClientShowHex
        {
            set
            {
                _clientShowHex = value;
                RaisePropertyChanged();
            }
            get => _clientShowHex;
        }

        private ObservableCollection<MessageModel> _clientMessageCollection = new ObservableCollection<MessageModel>();

        public ObservableCollection<MessageModel> ClientMessageCollection
        {
            set
            {
                _clientMessageCollection = value;
                RaisePropertyChanged();
            }
            get => _clientMessageCollection;
        }

        private ObservableCollection<ExCommandCache> _clientExCommandCollection =
            new ObservableCollection<ExCommandCache>();

        public ObservableCollection<ExCommandCache> ClientExCommandCollection
        {
            set
            {
                _clientExCommandCollection = value;
                RaisePropertyChanged();
            }
            get => _clientExCommandCollection;
        }

        private bool _clientSendHex = true;

        public bool ClientSendHex
        {
            set
            {
                _clientSendHex = value;
                RaisePropertyChanged();
            }
            get => _clientSendHex;
        }

        private bool _clientLoopSend;

        public bool ClientLoopSend
        {
            set
            {
                _clientLoopSend = value;
                RaisePropertyChanged();
            }
            get => _clientLoopSend;
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

        private string _clientUserInputText = string.Empty;

        public string ClientUserInputText
        {
            set
            {
                _clientUserInputText = value;
                RaisePropertyChanged();
            }
            get => _clientUserInputText;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand ClientConnectRemoteCommand { set; get; }
        public DelegateCommand ClientShowHexCheckedCommand { set; get; }
        public DelegateCommand ClientShowHexUncheckedCommand { set; get; }
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand ClientAddExtensionCommand { set; get; }
        public DelegateCommand ClientLoopUncheckedCommand { set; get; }
        public DelegateCommand ClientSendHexCheckedCommand { set; get; }
        public DelegateCommand ClientSendHexUncheckedCommand { set; get; }
        public DelegateCommand ClientClearMessageCommand { set; get; }
        public DelegateCommand ClientSendMessageCommand { set; get; }
        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<ConnectedClientModel> ClientItemDoubleClickCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private readonly TcpServer _tcpServer = new TcpServer();
        private ClientConfigCache _clientCache;
        private bool _isListening;
        private ConnectedClientModel _connectedClient;

        public TcpCommunicateViewModel(IAppDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<TcpServerMessageEvent>().Subscribe(delegate(object cmd)
            {
                if (cmd is byte[] bytes)
                {
                    _tcpServer.Send(_connectedClient?.Id, bytes);
                }
                else
                {
                    _tcpServer.Send(_connectedClient?.Id, (string)cmd);
                }
            });

            InitDefaultConfig();

            ClientConnectRemoteCommand = new DelegateCommand(ClientConnectRemote);
            ClientShowHexCheckedCommand = new DelegateCommand(ClientShowHexChecked);
            ClientShowHexUncheckedCommand = new DelegateCommand(ClientShowHexUnchecked);
            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            ClientAddExtensionCommand = new DelegateCommand(ClientAddExtension);
            ClientLoopUncheckedCommand = new DelegateCommand(ClientLoopUnchecked);
            ClientSendHexCheckedCommand = new DelegateCommand(ClientSendHexChecked);
            ClientSendHexUncheckedCommand = new DelegateCommand(ClientSendHexUnchecked);
            ClientClearMessageCommand = new DelegateCommand(ClientClearMessage);
            ClientSendMessageCommand = new DelegateCommand(ClientSendMessage);
            ServerListenCommand = new DelegateCommand(ServerListen);
            ClientItemDoubleClickCommand = new DelegateCommand<ConnectedClientModel>(ClientItemDoubleClick);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.TcpClient);
            RemoteAddress = _clientCache.RemoteAddress;
            RemotePort = _clientCache.RemotePort.ToString();
            ClientShowHex = _clientCache.ShowHex == 1;
            ClientSendHex = _clientCache.SendHex == 1;

            ClientExCommandCollection = _dataService
                .LoadCommandExtensionCaches(ConnectionType.TcpClient)
                .ToObservableCollection();

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            //成功连接到服务器
            _tcpClient.Connected = (client, e) =>
            {
                ConnectionStateColor = "LimeGreen";
                ButtonState = "断开";
                return EasyTask.CompletedTask;
            };

            //从服务器断开连接，当连接不成功时不会触发。
            _tcpClient.Closed = (client, e) =>
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
                return EasyTask.CompletedTask;
            };

            //从服务器收到信息
            _tcpClient.Received = (client, e) =>
            {
                var byteBlock = e.ByteBlock;
                var messageModel = new MessageModel
                {
                    Content = _clientCache.ShowHex == 1
                        ? BitConverter.ToString(byteBlock.ToArray()).Replace("-", " ")
                        : byteBlock.Span.ToString(Encoding.UTF8),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { ClientMessageCollection.Add(messageModel); });
                return EasyTask.CompletedTask;
            };

            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();

            //有客户端成功连接
            _tcpServer.Connected = (client, e) =>
            {
                var clientModel = new ConnectedClientModel
                {
                    ClientType = ConnectionType.TcpClient,
                    Id = client.Id,
                    Ip = client.IP,
                    Port = client.Port
                };

                Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                return EasyTask.CompletedTask;
            };

            _tcpServer.Closed = (client, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectedClientModel clientModel = null;
                    foreach (var tcp in _clientCollection)
                    {
                        if (tcp.Id == client.Id && tcp.ClientType == ConnectionType.TcpClient)
                        {
                            clientModel = tcp;
                            break;
                        }
                    }

                    if (clientModel != null)
                    {
                        ClientCollection.Remove(clientModel);
                    }
                });
                return EasyTask.CompletedTask;
            };

            _tcpServer.Received = (client, e) =>
            {
                foreach (var tcp in _clientCollection)
                {
                    if (tcp.Id == client.Id && tcp.ClientType == ConnectionType.TcpClient)
                    {
                        var bytes = e.ByteBlock.ToArray();
                        _eventAggregator.GetEvent<TcpClientMessageEvent>().Publish(bytes);
                        //子窗口处于打开状态，不统计消息
                        if (!RuntimeCache.IsClientViewShowing)
                        {
                            tcp.MessageCollection.Add(bytes);
                            tcp.MessageCount++;
                        }

                        break;
                    }
                }

                return EasyTask.CompletedTask;
            };
        }

        private void ClientShowHexChecked()
        {
            var boxResult = MessageBox.Show(
                "切换到HEX显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            );
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in ClientMessageCollection)
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

                ClientMessageCollection = collection;

                _clientCache.ShowHex = 1;
            }
        }

        private void ClientShowHexUnchecked()
        {
            var boxResult = MessageBox.Show("确定切换到字符串显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in ClientMessageCollection)
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

                ClientMessageCollection = collection;

                _clientCache.ShowHex = 0;
            }
        }

        private void ClientConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_tcpClient.Online)
            {
                _tcpClient.Close();
            }
            else
            {
                _tcpClient.Setup(new TouchSocketConfig().SetRemoteIPHost($"{_remoteAddress}:{_remotePort}"));
                _tcpClient.Connect();
            }
        }

        private void DropDownOpened()
        {
            ClientExCommandCollection = _dataService
                .LoadCommandExtensionCaches(ConnectionType.TcpClient)
                .ToObservableCollection();
        }

        private void DeleteExCmd(object obj)
        {
            var result = MessageBox.Show(
                "确定删除此条扩展指令？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                _dataService.DeleteExtensionCommandCache(ConnectionType.TcpClient, (int)obj);
            }
        }

        private void DropDownClosed(ComboBox box)
        {
            if (box.SelectedIndex == -1)
            {
                box.SelectedIndex = 0;
            }

            var commandCache = _clientExCommandCollection[box.SelectedIndex];
            ClientUserInputText = commandCache.CommandValue;
        }

        private void ClientAddExtension()
        {
            var dialogParameters = new DialogParameters
            {
                { "ConnectionType", ConnectionType.TcpClient }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { });
        }

        private void ClientClearMessage()
        {
            ClientMessageCollection?.Clear();
        }

        private void ClientSendHexChecked()
        {
            _clientCache.SendHex = 1;
        }

        private void ClientSendHexUnchecked()
        {
            _clientCache.SendHex = 0;
        }

        private void ClientLoopUnchecked()
        {
            Console.WriteLine(@"取消循环发送指令");
            _loopSendMessageTimer.Enabled = false;
        }

        private void ClientSendMessage()
        {
            _clientCache.Type = ConnectionType.TcpClient;
            _clientCache.RemoteAddress = _remoteAddress;
            _clientCache.RemotePort = Convert.ToInt32(_remotePort);
            _dataService.SaveCacheConfig(_clientCache);

            if (string.IsNullOrWhiteSpace(_clientUserInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_buttonState.Equals("连接"))
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_clientLoopSend)
            {
                Console.WriteLine(@"开启循环发送指令");
                _loopSendMessageTimer.Interval = _commandInterval;
                _loopSendMessageTimer.Enabled = true;
            }
            else
            {
                if (_clientCache.SendHex == 1)
                {
                    if (!_clientUserInputText.IsHex())
                    {
                        MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //以byte[]发送
                    _tcpClient.SendAsync(_clientUserInputText.HexToBytes());
                }
                else
                {
                    _tcpClient.SendAsync(_clientUserInputText);
                }

                var message = new MessageModel
                {
                    Content = _clientUserInputText,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = true
                };
                ClientMessageCollection.Add(message);
            }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_buttonState.Equals("连接"))
            {
                Console.WriteLine(@"TCP未连接");
                return;
            }

            if (_clientCache.SendHex == 1)
            {
                if (!_clientUserInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _tcpClient.Send(_clientUserInputText.HexToBytes());
            }
            else
            {
                _tcpClient.Send(_clientUserInputText);
            }

            var message = new MessageModel
            {
                Content = _clientUserInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
            Application.Current.Dispatcher.Invoke(() => { ClientMessageCollection.Add(message); });
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

                _tcpServer.Stop();
                _isListening = false;
                ListenState = "监听";
                ListenStateColor = "DarkGray";
            }
            else
            {
                try
                {
                    _tcpServer.Setup(new TouchSocketConfig().SetListenIPHosts(_listenPort));
                    _tcpServer.Start();
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
            var dialogParameters = new DialogParameters
            {
                { "ClientModel", client }
            };
            _dialogService.Show("TcpClientMessageDialog", dialogParameters, delegate { });
            //窗口打开，消息已读，数量置0
            client.MessageCount = 0;
            RuntimeCache.IsClientViewShowing = true;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UdpClient = TouchSocket.Sockets.UdpSession;
using UdpServer = TouchSocket.Sockets.UdpSession;

namespace DevKit.ViewModels
{
    public class UdpCommunicateViewModel : BindableBase
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

        private ObservableCollection<ExCommandCache> _exCommandCollection =
            new ObservableCollection<ExCommandCache>();

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

        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }
        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand ItemDoubleClickCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly UdpClient _udpClient = new UdpClient();
        private readonly UdpServer _udpServer = new UdpServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private ClientConfigCache _clientCache;
        private bool _isListening;

        public UdpCommunicateViewModel(IAppDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            
            InitDefaultConfig();

            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
            ServerListenCommand = new DelegateCommand(ServerListen);
            // ItemDoubleClickCommand = new DelegateCommand(ClientItemDoubleClick);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.UdpClient);
            RemoteAddress = _clientCache.RemoteAddress;
            RemotePort = _clientCache.RemotePort.ToString();
            ShowHex = _clientCache.ShowHex == 1;
            SendHex = _clientCache.SendHex == 1;

            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.UdpClient)
                .ToObservableCollection();

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            _udpClient.Received = (client, e) =>
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

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                return EasyTask.CompletedTask;
            };

            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();

            _udpServer.Received = (client, e) =>
            {
                var endPoint = e.EndPoint;
                if (!_clientCollection.Any(udp =>
                        udp.Ip == endPoint.GetIP() &&
                        udp.Port == endPoint.GetPort() &&
                        udp.ClientType == ConnectionType.UdpClient))
                {
                    var clientModel = new ConnectedClientModel
                    {
                        ClientType = ConnectionType.UdpClient,
                        Ip = endPoint.GetIP(),
                        Port = endPoint.GetPort()
                    };

                    Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                }

                foreach (var udp in _clientCollection)
                {
                    if (udp.Ip == endPoint.GetIP() &&
                        udp.Port == endPoint.GetPort() &&
                        udp.ClientType == ConnectionType.UdpClient)
                    {
                        var bytes = e.ByteBlock.ToArray();
                        // _eventAggregator.GetEvent<TcpClientMessageEvent>().Publish(bytes);
                        //子窗口处于打开状态，不统计消息
                        if (!RuntimeCache.IsClientViewShowing)
                        {
                            udp.MessageCollection.Add(bytes);
                            udp.MessageCount++;
                        }
                    }
                }

                return EasyTask.CompletedTask;
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

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.UdpClient)
                .ToObservableCollection();
        }

        private void DeleteExCmd(object obj)
        {
            var result = MessageBox.Show(
                "确定删除此条扩展指令？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                _dataService.DeleteExtensionCommandCache(ConnectionType.UdpClient, (int)obj);
            }
        }

        private void DropDownClosed(ComboBox box)
        {
            if (box.SelectedIndex == -1)
            {
                box.SelectedIndex = 0;
            }

            var commandCache = _exCommandCollection[box.SelectedIndex];
            UserInputText = commandCache.CommandValue;
        }

        private void AddExtensionCommand()
        {
            var dialogParameters = new DialogParameters
            {
                { "ConnectionType", ConnectionType.UdpClient }
            };
            _dialogService.ShowDialog("ExCommandDialog", dialogParameters, delegate { });
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
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _clientCache.Type = ConnectionType.UdpClient;
            _clientCache.RemoteAddress = _remoteAddress;
            _clientCache.RemotePort = Convert.ToInt32(_remotePort);
            _dataService.SaveCacheConfig(_clientCache);

            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _udpClient.Setup(
                new TouchSocketConfig().SetRemoteIPHost(new IPHost($"{_remoteAddress}:{_remotePort}"))
            );
            _udpClient.Start();

            if (_loopSend)
            {
                Console.WriteLine(@"开启循环发送指令");
                _loopSendMessageTimer.Interval = _commandInterval;
                _loopSendMessageTimer.Enabled = true;
            }
            else
            {
                if (_clientCache.SendHex == 1)
                {
                    if (!_userInputText.IsHex())
                    {
                        MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _udpClient.Send(_userInputText.HexToBytes());
                }
                else
                {
                    _udpClient.Send(_userInputText);
                }

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
            if (_clientCache.SendHex == 1)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _udpClient.Send(_userInputText.HexToBytes());
            }
            else
            {
                _udpClient.Send(_userInputText);
            }

            var message = new MessageModel
            {
                Content = _userInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
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
    }
}
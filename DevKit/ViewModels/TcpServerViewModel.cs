using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using TcpServer = TouchSocket.Sockets.TcpService;

namespace DevKit.ViewModels
{
    public class TcpServerViewModel : BindableBase
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
        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly TcpServer _tcpServer = new TcpServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private bool _isListening;
        private ConnectedClientModel _connectedClient;

        public TcpServerViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ServerListenCommand = new DelegateCommand(ServerListen);
            ClientItemSelectionChangedCommand = new DelegateCommand<ConnectedClientModel>(ClientItemSelectionChanged);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            AddExtensionCommand = new DelegateCommand(AddExtension);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            //获取本机所有IPv4地址
            LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpServer)
                .ToObservableCollection();

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            //有客户端成功连接
            _tcpServer.Connected = (client, e) =>
            {
                ConnectedClientAddress = $"{client.IP}:{client.Port}";
                var clientModel = new ConnectedClientModel
                {
                    Id = client.Id,
                    Ip = client.IP,
                    Port = client.Port,
                    IsConnected = true,
                    ConnectColorBrush = "LimeGreen"
                };

                Application.Current.Dispatcher.Invoke(() => { ClientCollection.Add(clientModel); });
                return EasyTask.CompletedTask;
            };

            _tcpServer.Closed = (client, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var tcp in _clientCollection)
                    {
                        if (tcp.Id == client.Id)
                        {
                            tcp.IsConnected = false;
                            tcp.ConnectColorBrush = "DarkGray";
                            tcp.MessageCount = 0;
                            tcp.MessageCollection.Clear();
                            //显示空白图
                            IsContentViewVisible = "Collapsed";
                            IsEmptyImageVisible = "Visible";
                            break;
                        }
                    }

                    MessageCollection.Clear();
                });
                return EasyTask.CompletedTask;
            };

            _tcpServer.Received = (client, e) =>
            {
                foreach (var tcp in _clientCollection)
                {
                    if (tcp.Id == client.Id)
                    {
                        var bytes = e.ByteBlock.ToArray();
                        //内容界面不可见时，才需要更新消息数量
                        if (_isEmptyImageVisible.Equals("Visible"))
                        {
                            tcp.MessageCollection.Add(bytes);
                            tcp.MessageCount++;
                        }

                        if (_isContentViewVisible.Equals("Visible"))
                        {
                            var messageModel = new MessageModel
                            {
                                Content = _showHex
                                    ? BitConverter.ToString(bytes).Replace("-", " ")
                                    : Encoding.UTF8.GetString(bytes),
                                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                                IsSend = false
                            };

                            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                        }

                        break;
                    }
                }

                return EasyTask.CompletedTask;
            };
        }

        private void ServerListen()
        {
            if (!_listenPort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (_isListening)
            {
                _tcpServer.Stop();
                _isListening = false;
                ListenState = "监听";
                ListenStateColor = "DarkGray";
            }
            else
            {
                try
                {
                    _tcpServer.Setup(new TouchSocketConfig().SetListenIPHosts(int.Parse(_listenPort)));
                    _tcpServer.Start();
                    _isListening = true;
                    ListenState = "停止";
                    ListenStateColor = "LimeGreen";
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
            if (client.IsConnected)
            {
                IsContentViewVisible = "Visible";
                IsEmptyImageVisible = "Collapsed";

                foreach (var bytes in client.MessageCollection)
                {
                    var messageModel = new MessageModel
                    {
                        Content = _showHex
                            ? BitConverter.ToString(bytes).Replace("-", " ")
                            : Encoding.UTF8.GetString(bytes),
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
            }
        }

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpServer)
                .ToObservableCollection();
        }

        private void DeleteExCmd(object obj)
        {
            var result = MessageBox.Show(
                "确定删除此条扩展指令？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                _dataService.DeleteExtensionCommandCache(ConnectionType.TcpServer, (int)obj);
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

        private void AddExtension()
        {
            var dialogParameters = new DialogParameters
            {
                { "ConnectionType", ConnectionType.TcpServer }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { });
        }

        private void LoopUnchecked()
        {
            _loopSendMessageTimer.Enabled = false;
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_sendHex)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _tcpServer.Send(_connectedClient.Id, _userInputText.HexToBytes());
            }
            else
            {
                _tcpServer.Send(_connectedClient.Id, _userInputText);
            }

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
                if (_sendHex)
                {
                    if (!_userInputText.IsHex())
                    {
                        MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _tcpServer.Send(_connectedClient.Id, _userInputText.HexToBytes());
                }
                else
                {
                    _tcpServer.Send(_connectedClient.Id, _userInputText);
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
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
using UdpServer = TouchSocket.Sockets.UdpSession;

namespace DevKit.ViewModels
{
    public class UdpServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "UDP服务端";

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

        private string _listenPort = "5000";

        public string ListenPort
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

        private ObservableCollection<SocketClientModel> _clients = new ObservableCollection<SocketClientModel>();

        public ObservableCollection<SocketClientModel> Clients
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
        public DelegateCommand<SocketClientModel> ClientItemSelectionChangedCommand { set; get; }
        public DelegateCommand ShowHexCheckBoxClickCommand { set; get; }
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
        private readonly UdpServer _udpServer = new UdpServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private bool _isListening;
        private SocketClientModel _socketClient;

        public UdpServerViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ServerListenCommand = new DelegateCommand(ServerListen);
            ClientItemSelectionChangedCommand = new DelegateCommand<SocketClientModel>(ClientItemSelectionChanged);
            ShowHexCheckBoxClickCommand = new DelegateCommand(ShowHexCheckBoxClick);
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
            // LocalAddressCollection = _dataService.GetAllIPv4Addresses().ToObservableCollection();

            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.UdpServer)
                .ToObservableCollection();

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            _udpServer.Received = (client, e) =>
            {
                var endPoint = e.EndPoint;
                if (!_clients.Any(
                        x => x.Ip == endPoint.GetIP() && x.Port == endPoint.GetPort())
                   )
                {
                    var clientModel = new SocketClientModel
                    {
                        Ip = endPoint.GetIP(),
                        Port = endPoint.GetPort()
                    };

                    Application.Current.Dispatcher.Invoke(() => { Clients.Add(clientModel); });
                }

                var bytes = e.ByteBlock.ToArray();
                var udp = _clients.First(x => x.Ip == endPoint.GetIP() && x.Port == endPoint.GetPort());
                udp.MessageCount++;
                // using (var dataBase = new DataBaseConnection())
                // {
                //     var cache = new ClientMessageCache
                //     {
                //         ClientIp = endPoint.GetIP(),
                //         ClientPort = endPoint.GetPort(),
                //         ClientType = ConnectionType.UdpClient,
                //         MessageContent = _showHex
                //             ? BitConverter.ToString(bytes).Replace("-", " ")
                //             : Encoding.UTF8.GetString(bytes),
                //         ByteArrayContent = BitConverter.ToString(bytes),
                //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                //         IsSend = 0
                //     };
                //     dataBase.Insert(cache);
                // }

                if (_isContentViewVisible.Equals("Visible") &&
                    endPoint.GetIP() == _socketClient.Ip &&
                    endPoint.GetPort() == _socketClient.Port)
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
                _udpServer.Stop();
                _isListening = false;
                ListenState = "监听";
                ListenStateColor = "LightGray";
            }
            else
            {
                try
                {
                    _udpServer.Setup(new TouchSocketConfig().SetBindIPHost(new IPHost(int.Parse(_listenPort))));
                    _udpServer.Start();
                    _isListening = true;
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClientItemSelectionChanged(SocketClientModel client)
        {
            if (client == null)
            {
                return;
            }

            client.MessageCount = 0;
            _socketClient = client;
            ConnectedClientAddress = $"{client.Ip}:{client.Port}";
            MessageCollection.Clear();
            // using (var dataBase = new DataBaseConnection())
            // {
            //     var queryResult = dataBase.Table<ClientMessageCache>()
            //         .Where(x =>
            //             x.ClientIp == _connectedClient.Ip &&
            //             x.ClientPort == _connectedClient.Port &&
            //             x.ClientType == ConnectionType.UdpClient
            //         );
            //     if (queryResult.Any())
            //     {
            //         IsContentViewVisible = "Visible";
            //         IsEmptyImageVisible = "Collapsed";
            //
            //         foreach (var cache in queryResult)
            //         {
            //             var messageModel = new MessageModel
            //             {
            //                 Content = _showHex ? cache.ByteArrayContent.Replace("-", " ") : cache.MessageContent,
            //                 Time = cache.Time,
            //                 IsSend = cache.IsSend == 1
            //             };
            //
            //             MessageCollection.Add(messageModel);
            //         }
            //     }
            //     else
            //     {
            //         IsContentViewVisible = "Collapsed";
            //         IsEmptyImageVisible = "Visible";
            //     }
            // }
        }

        private void ShowHexCheckBoxClick()
        {
            // using (var dataBase = new DataBaseConnection())
            // {
            //     var queryResult = dataBase.Table<ClientMessageCache>().Where(x =>
            //         x.ClientIp == _connectedClient.Ip &&
            //         x.ClientPort == _connectedClient.Port &&
            //         x.ClientType == ConnectionType.UdpClient
            //     );
            //
            //     if (_showHex)
            //     {
            //         var boxResult = MessageBox.Show(
            //             "确定切换到HEX显示吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            //         );
            //         if (boxResult == MessageBoxResult.OK)
            //         {
            //             MessageCollection.Clear();
            //             foreach (var cache in queryResult)
            //             {
            //                 var msg = new MessageModel
            //                 {
            //                     Content = cache.MessageContent,
            //                     Time = cache.Time,
            //                     IsSend = cache.IsSend == 1
            //                 };
            //                 MessageCollection.Add(msg);
            //             }
            //         }
            //         else
            //         {
            //             ShowHex = false;
            //         }
            //     }
            //     else
            //     {
            //         var boxResult = MessageBox.Show(
            //             "确定切换到字符串显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            //         );
            //         if (boxResult == MessageBoxResult.OK)
            //         {
            //             MessageCollection.Clear();
            //             foreach (var cache in queryResult)
            //             {
            //                 var msg = new MessageModel
            //                 {
            //                     Content = cache.ByteArrayContent.HexToBytes().ByteArrayToString(),
            //                     Time = cache.Time,
            //                     IsSend = cache.IsSend == 1
            //                 };
            //                 MessageCollection.Add(msg);
            //             }
            //         }
            //         else
            //         {
            //             ShowHex = true;
            //         }
            //     }
            // }
        }

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.UdpServer)
                .ToObservableCollection();
        }

        private void DeleteExCmd(object obj)
        {
            var result = MessageBox.Show(
                "确定删除此条扩展指令？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                _dataService.DeleteExtensionCommandCache(ConnectionType.UdpServer, (int)obj);
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
                { "ConnectionType", ConnectionType.UdpServer }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { });
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
            _socketClient.MessageCount = 0;
            // using (var dataBase = new DataBaseConnection())
            // {
            //     dataBase.Table<ClientMessageCache>().Where(x =>
            //         x.ClientIp == _connectedClient.Ip &&
            //         x.ClientPort == _connectedClient.Port &&
            //         x.ClientType == ConnectionType.UdpClient
            //     ).Delete();
            // }
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
            var endPoint = new IPEndPoint(IPAddress.Parse(_socketClient.Ip), _socketClient.Port);
            if (_sendHex)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _udpServer.Send(endPoint, _userInputText.HexToBytes());
            }
            else
            {
                _udpServer.Send(endPoint, _userInputText);
            }

            // using (var dataBase = new DataBaseConnection())
            // {
            //     var cache = new ClientMessageCache
            //     {
            //         ClientIp = _connectedClient.Ip,
            //         ClientPort = _connectedClient.Port,
            //         ClientType = ConnectionType.UdpClient,
            //         MessageContent = _userInputText,
            //         ByteArrayContent = BitConverter.ToString(_userInputText.HexToBytes()),
            //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //         IsSend = 1
            //     };
            //     dataBase.Insert(cache);
            // }

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
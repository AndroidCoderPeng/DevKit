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
using HandyControl.Tools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using TcpClient = TouchSocket.Sockets.TcpClient;

namespace DevKit.ViewModels
{
    public class TcpClientViewModel : BindableBase
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

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private ClientConfigCache _clientCache;

        public TcpClientViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            AddExtensionCommand = new DelegateCommand(AddExtension);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.TcpClient);
            RemoteAddress = _clientCache.RemoteAddress;
            RemotePort = _clientCache.RemotePort.ToString();
            ShowHex = _clientCache.ShowHex == 1;
            SendHex = _clientCache.SendHex == 1;

            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpClient)
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

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
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

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //判断是否是IP和端口和理性
            if (!_remoteAddress.IsIp())
            {
                MessageBox.Show("IP格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_remotePort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_tcpClient.Online)
            {
                _tcpClient.Close();
            }
            else
            {
                _tcpClient.Setup(new TouchSocketConfig().SetRemoteIPHost($"{_remoteAddress}:{_remotePort}"));
                try
                {
                    _tcpClient.Connect();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpClient)
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

            var commandCache = _exCommandCollection[box.SelectedIndex];
            UserInputText = commandCache.CommandValue;
        }

        private void AddExtension()
        {
            var dialogParameters = new DialogParameters
            {
                { "ConnectionType", ConnectionType.TcpClient }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { });
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
                if (_clientCache.SendHex == 1)
                {
                    if (!_userInputText.IsHex())
                    {
                        MessageBox.Show("错误的HEX格式数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //以byte[]发送
                    _tcpClient.SendAsync(_userInputText.HexToBytes());
                }
                else
                {
                    _tcpClient.SendAsync(_userInputText);
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
            if (_buttonState.Equals("连接"))
            {
                Console.WriteLine(@"TCP未连接");
                return;
            }

            if (_clientCache.SendHex == 1)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _tcpClient.Send(_userInputText.HexToBytes());
            }
            else
            {
                _tcpClient.Send(_userInputText);
            }

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
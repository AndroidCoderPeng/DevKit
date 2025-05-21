using System;
using System.Collections.Generic;
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
using UdpClient = TouchSocket.Sockets.UdpSession;

namespace DevKit.ViewModels
{
    public class UdpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "UDP客户端";

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

        #endregion

        #region DelegateCommand

        public DelegateCommand ShowHexCheckBoxClickCommand { set; get; }
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly UdpClient _udpClient = new UdpClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private readonly List<MessageModel> _messageTemp = new List<MessageModel>();
        private ClientConfigCache _clientCache;

        public UdpClientViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            ShowHexCheckBoxClickCommand = new DelegateCommand(ShowHexCheckBoxClick);
            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
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

            SetupUdpConfig();
        }

        private void ShowHexCheckBoxClick()
        {
            if (_showHex)
            {
                var boxResult = MessageBox.Show(
                    "确定切换到HEX显示吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                );
                if (boxResult == MessageBoxResult.OK)
                {
                    MessageCollection.Clear();
                    foreach (var model in _messageTemp)
                    {
                        var msg = new MessageModel
                        {
                            Content = model.Content,
                            Time = model.Time,
                            IsSend = model.IsSend
                        };
                        MessageCollection.Add(msg);
                    }

                    _clientCache.ShowHex = 1;
                }
                else
                {
                    ShowHex = false;
                }
            }
            else
            {
                var boxResult = MessageBox.Show(
                    "确定切换到字符串显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                );
                if (boxResult == MessageBoxResult.OK)
                {
                    MessageCollection.Clear();
                    foreach (var model in _messageTemp)
                    {
                        var msg = new MessageModel
                        {
                            Content = model.Bytes.ByteArrayToString(),
                            Time = model.Time,
                            IsSend = model.IsSend
                        };
                        MessageCollection.Add(msg);
                    }

                    _clientCache.ShowHex = 0;
                }
                else
                {
                    ShowHex = true;
                }
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
            //清空消息缓存
            _messageTemp?.Clear();
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
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //判断是否是IP和端口合理性
            // if (!_remoteAddress.IsIp())
            // {
            //     MessageBox.Show("IP格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }

            if (!_remotePort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            Send(false);
        }

        private void Send(bool isMainThread)
        {
            SetupUdpConfig();
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
                Bytes = _userInputText.HexToBytes(),
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };
            //缓存发送的消息
            _messageTemp.Add(message);

            if (isMainThread)
            {
                MessageCollection.Add(message);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
            }
        }

        private void SetupUdpConfig()
        {
            if (!string.IsNullOrWhiteSpace(_remoteAddress) && !string.IsNullOrWhiteSpace(_remotePort))
            {
                _udpClient.Setup(new TouchSocketConfig().UseUdpReceive().SetRemoteIPHost(
                    new IPHost($"{_remoteAddress}:{_remotePort}"))
                );
                _udpClient.Received = (client, e) =>
                {
                    var byteBlock = e.ByteBlock;
                    var messageModel = new MessageModel
                    {
                        Content = _clientCache.ShowHex == 1
                            ? BitConverter.ToString(byteBlock.ToArray()).Replace("-", " ")
                            : byteBlock.Span.ToString(Encoding.UTF8),
                        Bytes = byteBlock.ToArray(),
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = false
                    };
                    //缓存收到的消息
                    _messageTemp.Add(messageModel);
                    Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                    return EasyTask.CompletedTask;
                };
                _udpClient.Start();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class TcpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "TCP客户端";

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

        private string _remotePort = "9000";

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

        private string _connectionStateColor = "red";

        public string ConnectionStateColor
        {
            set
            {
                _connectionStateColor = value;
                RaisePropertyChanged();
            }
            get => _connectionStateColor;
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

        private bool _isHexSelected = true;

        public bool IsHexSelected
        {
            set
            {
                _isHexSelected = value;
                RaisePropertyChanged();
            }
            get => _isHexSelected;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand SaveCommunicationCommand { set; get; }
        public DelegateCommand ClearCommunicationCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand<string> ItemSelectionChangedCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand<string> CopyCommand { set; get; }
        public DelegateCommand<object> EditCommand { set; get; }
        public DelegateCommand<object> DeleteCommand { set; get; }

        public DelegateCommand ShowHexCheckBoxClickCommand { set; get; } //TODO 暂时用不上
        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private readonly List<MessageModel> _messageTemp = new List<MessageModel>();
        private ClientConfigCache _clientCache;

        public TcpClientViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            using (var dataBase = new DataBaseConnection())
            {
                //加载连接配置缓存
                var queryResult = dataBase.Table<ClientConfigCache>()
                    .Where(x => x.ClientType == "TCP")
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();
                if (queryResult != null)
                {
                    RemoteAddress = queryResult.RemoteAddress;
                    RemotePort = queryResult.RemotePort.ToString();
                }

                //加载扩展指令缓存
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == "TCP")
                    .ToList();
                ExCommandCollection = commandCache.ToObservableCollection();
            }

            InitConnectStateEvent();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            SaveCommunicationCommand = new DelegateCommand(SaveCommunicationLog);
            ClearCommunicationCommand = new DelegateCommand(ClearCommunicationLog);
            AddExtensionCommand = new DelegateCommand(AddExtension);
            ItemSelectionChangedCommand = new DelegateCommand<string>(OnCommandItemSelected);
            SendCommand = new DelegateCommand(SendMessage);
            CopyCommand = new DelegateCommand<string>(OnCopy);
            EditCommand = new DelegateCommand<object>(OnEdit);
            DeleteCommand = new DelegateCommand<object>(OnDelete);

            // ShowHexCheckBoxClickCommand = new DelegateCommand(ShowHexCheckBoxClick);
            // DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            // DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            // LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;
        }

        /// <summary>
        /// 连接状态监听
        /// </summary>
        private void InitConnectStateEvent()
        {
            _tcpClient.Connected = (client, e) =>
            {
                ConnectionStateColor = "Lime";
                ButtonState = "断开";
                //更新连接配置缓存
                using (var dataBase = new DataBaseConnection())
                {
                    var queryResult = dataBase.Table<ClientConfigCache>()
                        .Where(x => x.ClientType == "TCP")
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();
                    if (queryResult != null)
                    {
                        queryResult.RemoteAddress = _remoteAddress;
                        queryResult.RemotePort = Convert.ToInt32(_remotePort);
                        dataBase.Update(queryResult);
                    }
                    else
                    {
                        var config = new ClientConfigCache
                        {
                            ClientType = "TCP",
                            RemoteAddress = _remoteAddress,
                            RemotePort = Convert.ToInt32(_remotePort)
                        };
                        dataBase.Insert(config);
                    }
                }

                return EasyTask.CompletedTask;
            };

            _tcpClient.Closed = (client, e) =>
            {
                ConnectionStateColor = "LightGray";
                ButtonState = "连接";
                return EasyTask.CompletedTask;
            };

            _tcpClient.Received = (client, e) =>
            {
                var byteBlock = e.ByteBlock;
                var messageModel = new MessageModel
                {
                    // Content = _clientCache.ShowHex == 1
                    //     ? BitConverter.ToString(byteBlock.ToArray()).Replace("-", " ")
                    //     : byteBlock.Span.ToString(Encoding.UTF8),
                    Bytes = byteBlock.ToArray(),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                //缓存收到的消息
                _messageTemp.Add(messageModel);
                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                return EasyTask.CompletedTask;
            };
        }

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //判断是否是IP和端口合理性
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

            using (var dataBase = new DataBaseConnection())
            {
                var queryResult = dataBase.Table<ClientConfigCache>()
                    .Where(x => x.ClientType == "TCP")
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();
                if (queryResult == null) return;
                queryResult.RemoteAddress = _remoteAddress;
                queryResult.RemotePort = Convert.ToInt32(_remotePort);
                dataBase.Update(queryResult);
            }
        }

        private void SaveCommunicationLog()
        {
        }

        private void ClearCommunicationLog()
        {
            _messageTemp?.Clear();
            MessageCollection?.Clear();
        }

        private void AddExtension()
        {
            _dialogService.Show("ExCommandDialog", null, delegate(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                var commandValue = result.Parameters.GetValue<string>("CommandValue");
                var annotation = result.Parameters.GetValue<string>("Annotation");
                using (var dataBase = new DataBaseConnection())
                {
                    var exCommand = new ExCommandCache
                    {
                        ClientType = "TCP",
                        CommandValue = commandValue,
                        Annotation = annotation
                    };
                    dataBase.Insert(exCommand);
                    //刷新列表
                    ExCommandCollection.Clear();
                    var commandCache = dataBase.Table<ExCommandCache>()
                        .Where(x => x.ClientType == "TCP")
                        .ToList();
                    ExCommandCollection = commandCache.ToObservableCollection();
                }
            });
        }

        private void OnCommandItemSelected(string command)
        {
            UserInputText = command;
        }

        private void OnCopy(string value)
        {
            Clipboard.SetText(value);
        }

        private void OnEdit(object id)
        {
            var dialogParameters = new DialogParameters();
            ExCommandCache exCommand;

            using (var dataBase = new DataBaseConnection())
            {
                exCommand = dataBase.Table<ExCommandCache>().First(x => x.Id == (int)id);
                dialogParameters.Add("ExCommandCache", exCommand);
            }

            _dialogService.Show("ExCommandDialog", dialogParameters, delegate(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                var commandValue = result.Parameters.GetValue<string>("CommandValue");
                var annotation = result.Parameters.GetValue<string>("Annotation");
                exCommand.CommandValue = commandValue;
                exCommand.Annotation = annotation;
                using (var dataBase = new DataBaseConnection())
                {
                    dataBase.Update(exCommand);
                    //刷新列表
                    ExCommandCollection.Clear();
                    var commandCache = dataBase.Table<ExCommandCache>()
                        .Where(x => x.ClientType == "TCP")
                        .ToList();
                    ExCommandCollection = commandCache.ToObservableCollection();
                }
            });
        }

        private void OnDelete(object id)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var itemToDelete = dataBase.Table<ExCommandCache>()
                    .First(x => x.Id == (int)id);
                dataBase.Delete(itemToDelete);
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == "TCP")
                    .ToList();
                ExCommandCollection = commandCache.ToObservableCollection();
            }
        }

        private void SendMessage()
        {
            if (!_tcpClient.Online)
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_isHexSelected)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("16进制格式数据错误，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // var bytes = _userInputText.ByHexStringToBytes();
                // _tcpClient.Send(bytes);
                // UpdateCommunicationLog(bytes, "发");
            }
            else
            {
                var bytes = _userInputText.ToUTF8Bytes();
                _tcpClient.Send(bytes);
                UpdateCommunicationLog(bytes, "发");
            }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (!_tcpClient.Online)
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendMessage();
        }

        private void UpdateCommunicationLog(byte[] bytes, string direction)
        {
            var message = new MessageModel
            {
                Content = _userInputText,
                Bytes = _userInputText.HexToBytes(),
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };

            //缓存发送的消息
            _messageTemp.Add(message);
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
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

                    // _clientCache.ShowHex = 1;
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

                    // _clientCache.ShowHex = 0;
                }
                else
                {
                    ShowHex = true;
                }
            }
        }

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpClient)
                .ToObservableCollection();
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

        private void LoopUnchecked()
        {
            _loopSendMessageTimer.Enabled = false;
        }
    }
}
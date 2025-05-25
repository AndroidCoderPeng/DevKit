using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
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
    public class TcpServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "TCP服务端";

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
            if (_tcpServer.ServerState != ServerState.Running) return;
            _tcpServer.Stop();
            ListenState = "监听";
            ListenStateColor = "LightGray";
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _localHost = string.Empty;

        public string LocalHost
        {
            set
            {
                _localHost = value;
                RaisePropertyChanged();
            }
            get => _localHost;
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

        private string _clientAddress = string.Empty;

        public string ClientAddress
        {
            set
            {
                _clientAddress = value;
                RaisePropertyChanged();
            }
            get => _clientAddress;
        }

        private ObservableCollection<LogCache> _logs = new ObservableCollection<LogCache>();

        public ObservableCollection<LogCache> Logs
        {
            set
            {
                _logs = value;
                RaisePropertyChanged();
            }
            get => _logs;
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

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<SocketClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private const string ClientType = "TCP";
        private readonly TcpServer _tcpServer = new TcpServer();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();

        public TcpServerViewModel(IAppDataService dataService)
        {
            LocalHost = dataService.GetIPv4Address();

            InitListenStateEvent();

            ServerListenCommand = new DelegateCommand(OnServerListened);
            ClientItemClickedCommand = new DelegateCommand<SocketClientModel>(OnClientItemClicked);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
            SendCommand = new DelegateCommand(OnMessageSend);
            TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
            ComboBoxItemSelectedCommand = new DelegateCommand<object>(OnComboBoxItemSelected);
        }

        private void InitListenStateEvent()
        {
            _tcpServer.Connected = (client, e) =>
            {
                var model = new SocketClientModel
                {
                    Id = client.Id,
                    Ip = client.IP,
                    Port = client.Port,
                    IsConnected = true,
                    IsSelected = false
                };

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { Clients.Add(model); }));
                return EasyTask.CompletedTask;
            };

            _tcpServer.Closed = (client, e) =>
            {
                var tcp = _clients.FirstOrDefault(x => x.Id == client.Id);
                if (tcp != null)
                {
                    tcp.IsConnected = false;
                }

                return EasyTask.CompletedTask;
            };

            _tcpServer.Received = (client, e) =>
            {
                var tcp = _clients.FirstOrDefault(x => x.Id == client.Id);
                if (tcp != null)
                {
                    tcp.MessageCount++;
                    UpdateCommunicationLog($"{client.IP}:{client.Port}", "", e.ByteBlock.ToArray());
                }

                return EasyTask.CompletedTask;
            };
        }

        private void OnServerListened()
        {
            if (!_listenPort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_tcpServer.ServerState == ServerState.Running)
            {
                _tcpServer.Stop();
                ListenState = "监听";
                ListenStateColor = "LightGray";
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig().SetListenOptions(options =>
                    {
                        options.Add(new TcpListenOption
                        {
                            IpHost = new IPHost($"{_localHost}:{_listenPort}")
                        });
                    });
                    _tcpServer.Setup(socketConfig);
                    _tcpServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnClientItemClicked(SocketClientModel client)
        {
            var clientModel = _clients.FirstOrDefault(x => x == client);
            if (clientModel != null)
            {
                clientModel.IsSelected = true;
                Logs.Clear();
                IsContentViewVisible = "Visible";
                IsEmptyImageVisible = "Collapsed";

                client.MessageCount = 0;
                ClientAddress = $"{client.Ip}:{client.Port}";
                // 显示当前选中客户端的消息
                using (var dataBase = new DataBaseConnection())
                {
                    var queryResult = dataBase.Table<LogCache>()
                        .Where(x => x.ClientType == ClientType && x.HostAddress == _clientAddress)
                        .ToList();
                    Logs = queryResult.ToObservableCollection();
                }
            }
        }

        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
        }

        private void OnMessageSend()
        {
            SendMessage(_userInputText);
        }

        private void OnTimeChecked()
        {
            if (!_commandInterval.IsNumber())
            {
                MessageBox.Show("时间间隔仅支持正整数", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _loopSendCommandTimer.Tick += TimerTickEvent_Handler;
            _loopSendCommandTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(_commandInterval));
            _loopSendCommandTimer.Start();
        }

        private void OnTimeUnchecked()
        {
            _loopSendCommandTimer.Tick -= TimerTickEvent_Handler;
            _loopSendCommandTimer.Stop();
        }

        private void TimerTickEvent_Handler(object sender, EventArgs e)
        {
            if (_tcpServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendMessage(_userInputText);
        }

        private void SendMessage(string command)
        {
            if (_tcpServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var client = Clients.FirstOrDefault(c => c.IsSelected);
            if (client == null)
            {
                MessageBox.Show("未选中客户端，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] bytes;
            if (_isHexSelected)
            {
                if (!command.IsHex())
                {
                    MessageBox.Show("16进制格式数据错误，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bytes = command.Replace(" ", "").ByHexStringToBytes();
            }
            else
            {
                bytes = command.ToUTF8Bytes();
            }

            _tcpServer.GetClient(client.Id).Send(bytes);
            UpdateCommunicationLog($"{client.Ip}:{client.Port}", command, bytes);
        }

        private void UpdateCommunicationLog(string host, string command, byte[] bytes)
        {
            if (command.Equals(""))
            {
                //默认显示为UTF8编码
                using (var dataBase = new DataBaseConnection())
                {
                    var log = new LogCache
                    {
                        ClientType = ClientType,
                        HostAddress = host,
                        Content = bytes.ByBytesToHexString(" "),
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = 0
                    };
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { Logs.Add(log); }));
                    dataBase.Insert(log);
                }
            }
            else
            {
                using (var dataBase = new DataBaseConnection())
                {
                    var log = new LogCache
                    {
                        ClientType = ClientType,
                        HostAddress = host,
                        Content = command,
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = 1
                    };
                    Logs.Add(log);
                    dataBase.Insert(log);
                }
            }
        }

        private void OnComboBoxItemSelected(object index)
        {
            if (index == null) return;

            if (index.ToString().Equals("0"))
            {
                //转为16进制显示
                foreach (var log in _logs)
                {
                    var bytes = log.Content.ToUTF8Bytes();
                    log.Content = bytes.ByBytesToHexString(" ");
                }
            }
            else if (index.ToString().Equals("1"))
            {
                //转为ASCII显示
                foreach (var log in _logs)
                {
                    var bytes = log.Content.Replace(" ", "").ByHexStringToBytes();
                    log.Content = Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}
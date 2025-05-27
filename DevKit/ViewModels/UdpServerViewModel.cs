using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
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
            if (_udpServer.ServerState != ServerState.Running) return;
            _udpServer.Stop();
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

        private ObservableCollection<UdpClientModel> _clients = new ObservableCollection<UdpClientModel>();

        public ObservableCollection<UdpClientModel> Clients
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

        private ObservableCollection<LogModel> _logs = new ObservableCollection<LogModel>();

        public ObservableCollection<LogModel> Logs
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
        public DelegateCommand<UdpClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private readonly UdpServer _udpServer = new UdpServer();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private UdpClientModel _selectedClient;

        public UdpServerViewModel(IAppDataService dataService)
        {
            LocalHost = dataService.GetIPv4Address();

            InitListenStateEvent();

            ServerListenCommand = new DelegateCommand(OnServerListened);
            ClientItemClickedCommand = new DelegateCommand<UdpClientModel>(OnClientItemClicked);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
            SendCommand = new DelegateCommand(OnMessageSend);
            TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
            ComboBoxItemSelectedCommand = new DelegateCommand<object>(OnComboBoxItemSelected);
        }

        private void InitListenStateEvent()
        {
            _udpServer.Received = (session, e) =>
            {
                var endPoint = e.EndPoint;
                var udp = _clients.FirstOrDefault(x => x.Ip == endPoint.GetIP() && x.Port == endPoint.GetPort());
                if (udp != null)
                {
                    _selectedClient = udp;
                }
                else
                {
                    _selectedClient = new UdpClientModel
                    {
                        Ip = endPoint.GetIP(),
                        Port = endPoint.GetPort(),
                        TargetEndPoint = endPoint
                    };
                }

                var log = new LogModel
                {
                    Content = e.ByteBlock.ToArray().ByBytesToHexString(" "),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = 0
                };
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!Clients.Contains(_selectedClient))
                    {
                        Clients.Add(_selectedClient);
                        OnClientItemClicked(_selectedClient);
                    }

                    _selectedClient.MessageCount++;
                    _selectedClient.Logs.Add(log);
                }));

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

            if (_udpServer.ServerState == ServerState.Running)
            {
                _udpServer.Stop();
                ListenState = "监听";
                ListenStateColor = "LightGray";
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig().SetBindIPHost(new IPHost($"{_localHost}:{_listenPort}"));
                    _udpServer.Setup(socketConfig);
                    _udpServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnClientItemClicked(UdpClientModel client)
        {
            _selectedClient = client;
            if (_selectedClient != null)
            {
                IsContentViewVisible = "Visible";
                IsEmptyImageVisible = "Collapsed";

                client.MessageCount = 0;
                ClientAddress = $"{client.Ip}:{client.Port}";
                // 显示当前选中客户端的消息
                Logs = _selectedClient.Logs;
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
            if (_udpServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendMessage(_userInputText);
        }

        private void SendMessage(string command)
        {
            if (_udpServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedClient == null)
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
                bytes = command.ToUtf8Bytes();
            }

            _udpServer.Send(_selectedClient.TargetEndPoint, bytes);
            var log = new LogModel
            {
                Content = command,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = 1
            };
            _selectedClient.Logs.Add(log);
        }

        private void OnComboBoxItemSelected(object index)
        {
            if (index == null) return;

            if (index.ToString().Equals("0"))
            {
                //转为16进制显示
                foreach (var log in _selectedClient.Logs)
                {
                    var bytes = log.Content.ToUtf8Bytes();
                    log.Content = bytes.ByBytesToHexString(" ");
                }
            }
            else if (index.ToString().Equals("1"))
            {
                //转为ASCII显示
                foreach (var log in _selectedClient.Logs)
                {
                    var bytes = log.Content.Replace(" ", "").ByHexStringToBytes();
                    log.Content = Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}
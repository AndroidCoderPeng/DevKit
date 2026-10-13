using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Transports;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _stateOuterBackgroundColor = "#FFF7F9FC";

        public string StateOuterBackgroundColor
        {
            get => _stateOuterBackgroundColor;
            set => SetProperty(ref _stateOuterBackgroundColor, value);
        }

        private string _stateOuterBorderColor = "#FFEEEEF0";

        public string StateOuterBorderColor
        {
            get => _stateOuterBorderColor;
            set => SetProperty(ref _stateOuterBorderColor, value);
        }

        private string _stateInnerBackgroundColor = "#E7EBF0";

        public string StateInnerBackgroundColor
        {
            get => _stateInnerBackgroundColor;
            set => SetProperty(ref _stateInnerBackgroundColor, value);
        }

        private string _stateInnerBorderColor = "#93A0AE";

        public string StateInnerBorderColor
        {
            get => _stateInnerBorderColor;
            set => SetProperty(ref _stateInnerBorderColor, value);
        }

        private string _stateTextColor = "#5F6B7A";

        public string StateTextColor
        {
            get => _stateTextColor;
            set => SetProperty(ref _stateTextColor, value);
        }

        private string _stateText = "未监听";

        public string StateText
        {
            get => _stateText;
            set => SetProperty(ref _stateText, value);
        }

        private string _selectedIp = string.Empty;

        public string SelectedIp
        {
            get => _selectedIp;
            set => SetProperty(ref _selectedIp, value);
        }

        private List<string> _localHostSource = new List<string>();

        public List<string> LocalHostSource
        {
            set
            {
                _localHostSource = value;
                RaisePropertyChanged();
            }
            get => _localHostSource;
        }

        private string _localPort = "12121";

        public string LocalPort
        {
            get => _localPort;
            set => SetProperty(ref _localPort, value);
        }

        private bool _isReusePortEnabled = true;

        public bool IsReusePortEnabled
        {
            get => _isReusePortEnabled;
            set => SetProperty(ref _isReusePortEnabled, value);
        }

        private string _listenButtonText = "开始监听";

        public string ListenButtonText
        {
            get => _listenButtonText;
            set => SetProperty(ref _listenButtonText, value);
        }

        private string _runningTime = "00:00:00";

        public string RunningTime
        {
            get => _runningTime;
            set => SetProperty(ref _runningTime, value);
        }

        private int _onlineClientCount;

        public int OnlineClientCount
        {
            get => _onlineClientCount;
            set => SetProperty(ref _onlineClientCount, value);
        }

        private int _clientCount;

        public int ClientCount
        {
            get => _clientCount;
            set => SetProperty(ref _clientCount, value);
        }

        private int _unReadMessageCount;

        public int UnReadMessageCount
        {
            get => _unReadMessageCount;
            set => SetProperty(ref _unReadMessageCount, value);
        }

        private int _sendCount;

        public int SendCount
        {
            get => _sendCount;
            set => SetProperty(ref _sendCount, value);
        }

        private int _sendByteSize;

        public int SendByteSize
        {
            get => _sendByteSize;
            set => SetProperty(ref _sendByteSize, value);
        }

        private int _receiveCount;

        public int ReceiveCount
        {
            get => _receiveCount;
            set => SetProperty(ref _receiveCount, value);
        }

        private int _receiveByteSize;

        public int ReceiveByteSize
        {
            get => _receiveByteSize;
            set => SetProperty(ref _receiveByteSize, value);
        }

        private ObservableCollection<TcpServerClient> _clientItems = new ObservableCollection<TcpServerClient>();

        public ObservableCollection<TcpServerClient> ClientItems
        {
            get => _clientItems;
            set => SetProperty(ref _clientItems, value);
        }

        private TcpServerClient _selectedClient;

        public TcpServerClient SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        private bool _isLogHexSelected = true;

        public bool IsLogHexSelected
        {
            get => _isLogHexSelected;
            set
            {
                if (SetProperty(ref _isLogHexSelected, value))
                {
                    RaisePropertyChanged(nameof(IsLogAsciiSelected));
                    // foreach (var log in Logs)
                    // {
                    //     if (log.RawData != null)
                    //     {
                    //         log.Content = FormatLogContent(log.RawData);
                    //     }
                    // }
                }
            }
        }

        public bool IsLogAsciiSelected
        {
            get => !_isLogHexSelected;
            set
            {
                if (value)
                {
                    IsLogHexSelected = false;
                }
            }
        }

        private ObservableCollection<SocketMessage> _logs = new ObservableCollection<SocketMessage>();

        public ObservableCollection<SocketMessage> Logs
        {
            get => _logs;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var previous = _logs;
                if (!SetProperty(ref _logs, value))
                    return;

                previous.CollectionChanged -= Logs_CollectionChanged;
                _logs.CollectionChanged += Logs_CollectionChanged;
                RaiseLogStatisticsChanged();
            }
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

        private bool _isSendHexSelected = true;

        public bool IsSendHexSelected
        {
            get => _isSendHexSelected;
            set
            {
                if (SetProperty(ref _isSendHexSelected, value))
                {
                    RaisePropertyChanged(nameof(IsSendAsciiSelected));
                    RaisePropertyChanged(nameof(PlaceHolderText));
                }
            }
        }

        public string PlaceHolderText => IsSendAsciiSelected ? "例如：AT + RST" : "例如：aa550101，点格式化 → AA 55 01 01";

        public bool IsSendAsciiSelected
        {
            get => !_isSendHexSelected;
            set
            {
                if (value)
                {
                    IsSendHexSelected = false;
                }
            }
        }

        private bool _isLoopSendEnabled;

        public bool IsLoopSendEnabled
        {
            get => _isLoopSendEnabled;
            set => SetProperty(ref _isLoopSendEnabled, value);
        }

        private string _interval = "1000";

        public string Interval
        {
            get => _interval;
            set => SetProperty(ref _interval, value);
        }

        private string _toastMessage;

        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        private bool _isToastVisible;

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        private string _lastDataTime = string.Empty;

        public string LastDataTime
        {
            get => _lastDataTime;
            set => SetProperty(ref _lastDataTime, value);
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ListenPortCommand { set; get; }
        public DelegateCommand DisconnectAllCommand { set; get; } // TODO: 断开所有连接
        public DelegateCommand DisconnectClientCommand { set; get; } // TODO: 断开指定连接
        public DelegateCommand<TcpServerClient> ClientItemSelectedCommand { set; get; }

        #endregion

        private TcpServerTransport _server;
        private DispatcherTimer _runningTimer;
        private DateTime _listenStartedAt;
        private DispatcherTimer _toastTimer;

        public TcpServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
            if (_localHostSource.Any())
            {
                SelectedIp = _localHostSource.First();
            }

            _logs.CollectionChanged += Logs_CollectionChanged;

            ListenPortCommand = new DelegateCommand(() => _ = StartListeningAsync());


            ClientItemSelectedCommand = new DelegateCommand<TcpServerClient>(client =>
            {
                if (client == null)
                {
                    return;
                }

                if (!client.IsConnected)
                {
                    ShowToast($"{client.Ip}:{client.Port} 已断开");
                    return;
                }
                
                SelectedClient = client;

                // 切到该客户端 = 查看，未读清零
                client.MarkAllRead();
                Logs = client.Logs;
                RefreshUnreadMessageCount();
            });
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseLogStatisticsChanged();
        }

        private void RaiseLogStatisticsChanged()
        {
            RaisePropertyChanged(nameof(SendCount));
            RaisePropertyChanged(nameof(SendByteSize));
            RaisePropertyChanged(nameof(ReceiveCount));
            RaisePropertyChanged(nameof(ReceiveByteSize));
        }

        private async Task StartListeningAsync()
        {
            if (_server == null)
            {
                _server = new TcpServerTransport();
                _server.ClientConnected += OnClientConnected;
                _server.ClientClosed += OnClientClosed;
                _server.DataReceived += OnClientDataReceived;
            }

            if (!_server.IsRunning)
            {
                try
                {
                    await _server.StartAsync(SelectedIp, int.Parse(LocalPort), CancellationToken.None);
                    ApplyListenState(ListenState.Listening);
                    StartRunningTimer();
                }
                catch (Exception ex)
                {
                    ApplyListenState(ListenState.Error);
                    MessageBox.Show($"监听失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await _server.StopAsync();
                ApplyListenState(ListenState.NotListening);
                StopRunningTimer();

                // 停止后清掉旧的连接记录（StopAsync 会断开所有客户端）
                OnlineClientCount = 0;
                UnReadMessageCount = 0;
                SelectedClient = null;
            }
        }

        private void OnClientConnected(ServerClient info)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ClientItems.Add(new TcpServerClient
                {
                    Id = info.Id,
                    Ip = info.Ip,
                    Port = info.Port,
                    IsConnected = true
                });

                ClientCount = ClientItems.Count;
                OnlineClientCount = ClientItems.Count(x => x.IsConnected);
            }));
        }

        private void OnClientClosed(ServerClient info)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client != null)
                {
                    client.IsConnected = false;
                }

                OnlineClientCount = ClientItems.Count(x => x.IsConnected);
            }));
        }

        private void OnClientDataReceived(ServerClient info, byte[] bytes)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client == null)
                {
                    return;
                }

                var isViewing = ReferenceEquals(_selectedClient, client);
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                client.Logs.Add(new SocketMessage
                {
                    RawData = bytes,
                    Content = FormatLogContent(bytes),
                    Time = time,
                    IsSend = 0,
                    IsRead = isViewing   // 正在看 → 直接算已读；否则进未读
                });
                client.MessageCount++;

                ReceiveCount++;
                ReceiveByteSize += bytes.Length;
                LastDataTime = time;

                RefreshUnreadMessageCount();
            }));
        }

        private void RefreshUnreadMessageCount()
        {
            // 统计面板上的"未读消息"= 所有客户端未读数之和
            UnReadMessageCount = ClientItems.Sum(x => x.UnreadLogCount);
        }
        
        private void ApplyListenState(ListenState state)
        {
            switch (state)
            {
                case ListenState.Listening:
                    StateText = "已监听";
                    ListenButtonText = "停止监听";
                    StateOuterBackgroundColor = "#5917A95C";
                    StateOuterBorderColor = "#1F17A95C";
                    StateInnerBackgroundColor = "#BEE7D1";
                    StateInnerBorderColor = "#16A34A";
                    StateTextColor = "#16A34A";
                    break;

                case ListenState.Error:
                    StateText = "监听失败";
                    ListenButtonText = "开始监听"; // 保持可重试
                    StateOuterBackgroundColor = "#66F2994A";
                    StateOuterBorderColor = "#1FF2994A";
                    StateInnerBackgroundColor = "#FBE1C9";
                    StateInnerBorderColor = "#F2994A";
                    StateTextColor = "#F2994A";
                    break;

                default: // NotListening
                    StateText = "未监听";
                    ListenButtonText = "开始监听";
                    StateOuterBackgroundColor = "#FFF7F9FC";
                    StateOuterBorderColor = "#FFEEEEF0";
                    StateInnerBackgroundColor = "#E7EBF0";
                    StateInnerBorderColor = "#93A0AE";
                    StateTextColor = "#5F6B7A";
                    break;
            }
        }

        private void StartRunningTimer()
        {
            StopRunningTimer();

            RunningTime = "00:00:00";
            _listenStartedAt = DateTime.Now;

            _runningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _runningTimer.Tick += RunningTimerTick;
            _runningTimer.Start();
        }

        private void StopRunningTimer()
        {
            if (_runningTimer == null)
            {
                return;
            }

            _runningTimer.Stop();
            _runningTimer.Tick -= RunningTimerTick;
            _runningTimer = null;
        }

        private void RunningTimerTick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _listenStartedAt;
            RunningTime = $"{(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        private string FormatLogContent(byte[] bytes)
        {
            return IsLogHexSelected
                ? BitConverter.ToString(bytes).Replace("-", " ")
                : Encoding.UTF8.GetString(bytes);
        }
        
        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }
}
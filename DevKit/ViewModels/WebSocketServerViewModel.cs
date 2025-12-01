using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using WebSocketServer = TouchSocket.Http.HttpService;

namespace DevKit.ViewModels
{
    public class WebSocketServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "WebSocket服务端";

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
            if (_webSocketServer.ServerState != ServerState.Running) return;
            _webSocketServer.Stop();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private List<string> _localHostSource;

        public List<string> LocalHostSource
        {
            set
            {
                _localHostSource = value;
                RaisePropertyChanged();
            }
            get => _localHostSource;
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

        private string _webSocketPath = string.Empty;

        public string WebSocketPath
        {
            set
            {
                _webSocketPath = value;
                RaisePropertyChanged();
            }
            get => _webSocketPath;
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

        private ObservableCollection<WebSocketClientModel> _clients = new ObservableCollection<WebSocketClientModel>();

        public ObservableCollection<WebSocketClientModel> Clients
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

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand CopyWebSocketPathCommand { set; get; }
        public DelegateCommand<WebSocketClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }

        #endregion

        public DelegateCommand<string> HostSelectedCommand { set; get; }
        private readonly WebSocketServer _webSocketServer = new WebSocketServer();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private string _selectedHost;
        private WebSocketClientModel _selectedClient;

        public WebSocketServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
            _selectedHost = LocalHostSource?.First();

            HostSelectedCommand = new DelegateCommand<string>(HostSelected);
            ServerListenCommand = new DelegateCommand(OnServerListened);
            CopyWebSocketPathCommand = new DelegateCommand(CopyWebSocketPath);
            ClientItemClickedCommand = new DelegateCommand<WebSocketClientModel>(OnClientItemClicked);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
            SendCommand = new DelegateCommand(OnMessageSend);
            TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
        }

        private void HostSelected(string host)
        {
            _selectedHost = host;
        }
        
        private void OnServerListened()
        {
            if (!_listenPort.IsNumber())
            {
                MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_webSocketServer.ServerState == ServerState.Running)
            {
                _webSocketServer.Stop();
                ListenState = "监听";
                ListenStateColor = "LightGray";
            }
            else
            {
                try
                {
                    var socketConfig = new TouchSocketConfig()
                        .SetListenIPHosts(Convert.ToInt32(_listenPort))
                        .ConfigurePlugins(plugin =>
                        {
                            plugin.UseWebSocket();
                            InitListenStateEvent(plugin);
                        });
                    _webSocketServer.Setup(socketConfig);
                    _webSocketServer.Start();
                    ListenState = "停止";
                    ListenStateColor = "Lime";
                    WebSocketPath = $"ws://{_selectedHost}:{_listenPort}";
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitListenStateEvent(IPluginManager plugin)
        {
            plugin.AddWebSocketHandshakedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var model = new WebSocketClientModel
                {
                    Ip = session.IP,
                    Port = session.Port,
                    WebSocket = websocket,
                    IsConnected = true
                };

                OnClientItemClicked(model);
                Application.Current.Dispatcher.Invoke(() => { Clients.Add(_selectedClient); });
                await EasyTask.CompletedTask;
            });
            plugin.AddWebSocketReceivedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var webSocketClient = _clients.FirstOrDefault(x => x.Ip == session.IP && x.Port == session.Port);
                if (webSocketClient != null)
                {
                    //默认显示为UTF8编码
                    var log = new LogModel
                    {
                        Content = e.DataFrame.ToText(),
                        Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                        IsSend = 0
                    };
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        webSocketClient.MessageCount++;
                        webSocketClient.Logs.Add(log);
                    });
                }

                await EasyTask.CompletedTask;
            });
            plugin.AddWebSocketClosedPlugin(async (websocket, e) =>
            {
                var session = websocket.Client;
                var webSocketClient = Clients.FirstOrDefault(x => x.Ip == session.IP && x.Port == session.Port);
                if (webSocketClient != null)
                {
                    webSocketClient.IsConnected = false;
                }

                await EasyTask.CompletedTask;
            });
        }

        private void CopyWebSocketPath()
        {
            if (string.IsNullOrWhiteSpace(_webSocketPath))
            {
                MessageBox.Show("请先开启监听", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Clipboard.SetText(_webSocketPath);
        }

        private void OnClientItemClicked(WebSocketClientModel client)
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
            if (_webSocketServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendMessage(_userInputText);
        }

        private void SendMessage(string command)
        {
            if (_webSocketServer.ServerState != ServerState.Running)
            {
                MessageBox.Show("未开启监听，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedClient == null)
            {
                MessageBox.Show("未选中客户端，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _selectedClient.WebSocket.SendAsync(command);
            var log = new LogModel
            {
                Content = command,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = 1
            };
            _selectedClient.Logs.Add(log);
        }
    }
}
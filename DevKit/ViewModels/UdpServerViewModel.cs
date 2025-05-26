using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Newtonsoft.Json;
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
        public DelegateCommand<SocketClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private readonly UdpServer _udpServer = new UdpServer();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private SocketClientModel _selectedClient;

        public UdpServerViewModel(IAppDataService dataService)
        {
            LocalHost = dataService.GetIPv4Address();

            InitListenStateEvent();

            ServerListenCommand = new DelegateCommand(OnServerListened);
            // ClientItemClickedCommand = new DelegateCommand<SocketClientModel>(OnClientItemClicked);
            // CopyLogCommand = new DelegateCommand<string>(CopyLog);
            // SendCommand = new DelegateCommand(OnMessageSend);
            // TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            // TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
            // ComboBoxItemSelectedCommand = new DelegateCommand<object>(OnComboBoxItemSelected);
        }

        private void InitListenStateEvent()
        {
            _udpServer.Received = (client, e) =>
            {
                Console.WriteLine(JsonConvert.SerializeObject(client.Config));
                
                // var endPoint = e.EndPoint;
                // if (!_clients.Any(x => x.Ip == endPoint.GetIP() && x.Port == endPoint.GetPort())
                //    )
                // {
                //     var clientModel = new SocketClientModel
                //     {
                //         Ip = endPoint.GetIP(),
                //         Port = endPoint.GetPort()
                //     };
                //
                //     Application.Current.Dispatcher.Invoke(() => { Clients.Add(clientModel); });
                // }
                //
                // var bytes = e.ByteBlock.ToArray();
                // var udp = _clients.First(x => x.Ip == endPoint.GetIP() && x.Port == endPoint.GetPort());
                // udp.MessageCount++;
            
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
                    var socketConfig = new TouchSocketConfig().SetListenOptions(options =>
                    {
                        options.Add(new TcpListenOption
                        {
                            IpHost = new IPHost($"{_localHost}:{_listenPort}")
                        });
                    });
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
    }
}
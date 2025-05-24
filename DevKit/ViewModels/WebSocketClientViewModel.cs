using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using WebSocketClient = TouchSocket.Http.WebSockets.WebSocketClient;

namespace DevKit.ViewModels
{
    public class WebSocketClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "WebSocket客户端";

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

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand SaveCommunicationCommand { set; get; }
        public DelegateCommand ClearCommunicationCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand<string> DataGridItemSelectedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand<string> CopyCommand { set; get; }
        public DelegateCommand<object> EditCommand { set; get; }
        public DelegateCommand<object> DeleteCommand { set; get; }
        public DelegateCommand OpenScriptCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private const string ClientType = "WebSocket";
        private readonly IDialogService _dialogService;
        private readonly WebSocketClient _webSocketClient = new WebSocketClient();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();
        private IEnumerator<string> _commandEnumerator;

        public WebSocketClientViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            
        }

        private void InitDefaultConfig()
        {
            // _clientCache = _dataService.LoadClientConfigCache(ConnectionType.WebSocketClient);
            // RemoteAddress = _clientCache.RemoteAddress;
            //
            // _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;
            //
            // _webSocketClient.Handshaked = (client, e) =>
            // {
            //     ConnectionStateColor = "Lime";
            //     ButtonState = "断开";
            //     return EasyTask.CompletedTask;
            // };
            //
            // _webSocketClient.Received = (client, e) =>
            // {
            //     var messageModel = new MessageModel
            //     {
            //         Content = e.DataFrame.ToText(),
            //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //         IsSend = false
            //     };
            //
            //     Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
            //     return EasyTask.CompletedTask;
            // };
            //
            // _webSocketClient.Closed = (client, e) =>
            // {
            //     ConnectionStateColor = "LightGray";
            //     ButtonState = "连接";
            //     return EasyTask.CompletedTask;
            // };
        }

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress))
            {
                MessageBox.Show("WebSocket服务端地址未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_remoteAddress.IsWebSocketUrl())
            {
                MessageBox.Show("WebSocket服务端地址格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_webSocketClient.Online)
            {
                _webSocketClient.Close();
            }
            else
            {
                _webSocketClient.Setup(new TouchSocketConfig().SetRemoteIPHost($"{_remoteAddress}"));
                try
                {
                    _webSocketClient.Connect();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SendMessage()
        {
            // _clientCache.RemoteAddress = _remoteAddress;
            // _dataService.SaveConfigCache(_clientCache);
            //
            // if (string.IsNullOrWhiteSpace(_userInputText))
            // {
            //     MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // if (_buttonState.Equals("连接"))
            // {
            //     MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // if (_loopSend)
            // {
            //     if (!_commandInterval.IsNumber())
            //     {
            //         MessageBox.Show("循环发送时间间隔数据格式错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //         return;
            //     }
            //
            //     _loopSendMessageTimer.Interval = double.Parse(_commandInterval);
            //     _loopSendMessageTimer.Enabled = true;
            // }
            // else
            // {
            //     Send(true);
            // }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_buttonState.Equals("连接"))
            {
                Console.WriteLine(@"WebSocket未连接");
                return;
            }

            Send(false);
        }

        private void Send(bool isMainThread)
        {
            _webSocketClient.SendAsync(_userInputText);
            var message = new MessageModel
            {
                Content = _userInputText,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                IsSend = true
            };

            // if (isMainThread)
            // {
            //     MessageCollection.Add(message);
            // }
            // else
            // {
            //     Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
            // }
        }
    }
}
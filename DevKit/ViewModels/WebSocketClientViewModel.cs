﻿using System;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using WebSocketClient = TouchSocket.Http.WebSockets.WebSocketClient;

namespace DevKit.ViewModels
{
    public class WebSocketClientViewModel : BindableBase
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

        private string _connectionStateColor = "LightGray";

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
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly WebSocketClient _webSocketClient = new WebSocketClient();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private ClientConfigCache _clientCache;

        public WebSocketClientViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            InitDefaultConfig();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.WebSocketClient);
            RemoteAddress = _clientCache.RemoteAddress;

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;

            _webSocketClient.Handshaked = (client, e) =>
            {
                ConnectionStateColor = "Lime";
                ButtonState = "断开";
                return EasyTask.CompletedTask;
            };

            _webSocketClient.Received = (client, e) =>
            {
                var messageModel = new MessageModel
                {
                    Content = e.DataFrame.ToText(),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
                return EasyTask.CompletedTask;
            };

            _webSocketClient.Closed = (client, e) =>
            {
                ConnectionStateColor = "LightGray";
                ButtonState = "连接";
                return EasyTask.CompletedTask;
            };
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

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void LoopUnchecked()
        {
            _loopSendMessageTimer.Enabled = false;
        }

        private void SendMessage()
        {
            _clientCache.Type = ConnectionType.WebSocketClient;
            _clientCache.RemoteAddress = _remoteAddress;
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
                Send(true);
            }
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
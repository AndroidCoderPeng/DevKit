﻿using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class TcpCommunicateViewModel : BindableBase
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

        private long _commandInterval = 1000;

        public long CommandInterval
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
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly TcpClient _tcpClient = new TcpClient();
        private TcpClientConfigCache _clientCache;

        public TcpCommunicateViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            InitDefaultConfig();

            ConnectRemoteCommand = new DelegateCommand(ConnectRemote);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadTcpClientConfigCache();
            RemoteAddress = _clientCache.RemoteAddress;
            RemotePort = _clientCache.RemotePort.ToString();
            ShowHex = _clientCache.ShowHex == 1;
            SendHex = _clientCache.SendHex == 1;
            // var extensions = tcpClient.Extension;

            _tcpClient.OnConnected += delegate
            {
                ConnectionStateColor = "LimeGreen";
                ButtonState = "断开";
            };
            _tcpClient.OnDisconnected += delegate
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
            };
            _tcpClient.OnConnectFailed += delegate(object sender, Exception exception)
            {
                ConnectionStateColor = "DarkGray";
                ButtonState = "连接";
                MessageBox.Show(exception.Message, "出错了", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            _tcpClient.OnDataReceived += delegate(object sender, byte[] bytes)
            {
                var messageModel = new MessageModel
                {
                    Content = _clientCache.SendHex == 1
                        ? BitConverter.ToString(bytes).Replace("-", " ")
                        : Encoding.UTF8.GetString(bytes),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
            };
        }

        //TODO 有问题
        private void ShowHexChecked()
        {
            //先弹框，再获取MessageCollection，然后全部转为Hex，再重新渲染，最后存库
            var boxResult = MessageBox.Show("确定切换到Hex显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                // var collection = new ObservableCollection<MessageModel>();
                // foreach (var model in MessageCollection)
                // {
                //     //将model.Content视为string，先转bytes[]，再转Hex
                //     var bytes = model.Content.SerializeMessageByU8();
                //     var msg = new MessageModel
                //     {
                //         Content = BitConverter.ToString(bytes).Replace("-", " "),
                //         Time = model.Time,
                //         IsSend = model.IsSend
                //     };
                //     collection.Add(msg);
                // }
                //
                // MessageCollection = collection;

                _clientCache.ShowHex = 1;
                _dataService.SaveCacheConfig(_clientCache);
            }
        }

        //TODO 有问题
        private void ShowHexUnchecked()
        {
            //先弹框，再获取MessageCollection，然后全部转为string，再重新渲染，最后存库
            var boxResult = MessageBox.Show("确定切换到字符串显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                // var collection = new ObservableCollection<MessageModel>();
                // foreach (var model in MessageCollection)
                // {
                //     //将model.Content视为Hex，先转bytes[]，再转string
                //     var bytes = model.Content.SerializeMessageByU8();
                //     var msg = new MessageModel
                //     {
                //         Content = Encoding.UTF8.GetString(bytes),
                //         Time = model.Time,
                //         IsSend = model.IsSend
                //     };
                //     collection.Add(msg);
                // }
                //
                // MessageCollection = collection;
                
                _clientCache.ShowHex = 0;
                _dataService.SaveCacheConfig(_clientCache);
            }
        }

        private void ConnectRemote()
        {
            if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
            {
                MessageBox.Show("IP或者端口未填写", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _clientCache.RemoteAddress = _remoteAddress;
            _clientCache.RemotePort = Convert.ToInt32(_remotePort);
            _dataService.SaveCacheConfig(_clientCache);

            if (_tcpClient.IsRunning())
            {
                _tcpClient.Close();
            }
            else
            {
                _tcpClient.Start(_remoteAddress, Convert.ToInt32(_remotePort));
            }
        }

        private void AddExtensionCommand()
        {
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void SendHexChecked()
        {
            _clientCache.SendHex = 1;
            _dataService.SaveCacheConfig(_clientCache);
        }

        private void SendHexUnchecked()
        {
            _clientCache.SendHex = 0;
            _dataService.SaveCacheConfig(_clientCache);
        }

        private void SendMessage()
        {
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

            var messageModel = new MessageModel();
            if (_clientCache.SendHex == 1)
            {
                var bytes = _userInputText.SerializeMessageByU8();
                _tcpClient.SendAsync(bytes);
                messageModel.Content = BitConverter.ToString(bytes).Replace("-", " ");
            }
            else
            {
                _tcpClient.SendAsync(_userInputText);
                messageModel.Content = _userInputText;
            }

            messageModel.Time = DateTime.Now.ToString("HH:mm:ss.fff");
            messageModel.IsSend = true;
            MessageCollection.Add(messageModel);
        }
    }
}
using System;
using System.Timers;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
        
        #region VM

        

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand CopyWebSocketUrlCommand { set; get; }
        public DelegateCommand<WebSocketClientModel> ClientItemSelectionChangedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly WebSocketServer _webSocketServer = new WebSocketServer();
        private readonly Timer _loopSendMessageTimer = new Timer();
        private WebSocketClientModel _socketClient;

        public WebSocketServerViewModel(IAppDataService dataService)
        {
            
        }

        private void ServerListen()
        {
            // if (!_listenPort.IsNumber())
            // {
            //     MessageBox.Show("端口格式错误", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // if (_webSocketServer.ServerState == ServerState.Running)
            // {
            //     _webSocketServer.Stop();
            //     ListenState = "监听";
            //     ListenStateColor = "LightGray";
            //     WebSocketUrl = string.Empty;
            // }
            // else
            // {
            //     try
            //     {
            //         var socketConfig = new TouchSocketConfig()
            //             .SetListenIPHosts(int.Parse(ListenPort))
            //             .ConfigurePlugins(cfg =>
            //             {
            //                 cfg.UseWebSocket().SetWSUrl($"/{_requestPath}");
            //
            //                 //连接
            //                 cfg.Add(typeof(IWebSocketHandshakedPlugin),
            //                     (IWebSocket webSocket, HttpContextEventArgs e) =>
            //                     {
            //                         var session = webSocket.Client;
            //                         var clientModel = new WebSocketClientModel
            //                         {
            //                             Ip = session.IP,
            //                             Port = session.Port,
            //                             WebSocket = webSocket,
            //                             IsConnected = true
            //                         };
            //
            //                         Application.Current.Dispatcher.Invoke(() => { Clients.Add(clientModel); });
            //                         return EasyTask.CompletedTask;
            //                     });
            //
            //                 //收到消息
            //                 cfg.Add(typeof(IWebSocketReceivedPlugin), (IWebSocket webSocket, WSDataFrameEventArgs e) =>
            //                 {
            //                     var session = webSocket.Client;
            //                     var client = _clients.First(x => x.Ip == session.IP && x.Port == session.Port);
            //                     client.MessageCount++;
            //                     // using (var dataBase = new DataBaseConnection())
            //                     // {
            //                     //     var cache = new ClientMessageCache
            //                     //     {
            //                     //         ClientIp = session.IP,
            //                     //         ClientPort = session.Port,
            //                     //         ClientType = ConnectionType.WebSocketClient,
            //                     //         MessageContent = e.DataFrame.ToText(),
            //                     //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //                     //         IsSend = 0
            //                     //     };
            //                     //     dataBase.Insert(cache);
            //                     // }
            //
            //                     if (_isContentViewVisible.Equals("Visible") &&
            //                         _socketClient.Ip == session.IP && _socketClient.Port == session.Port)
            //                     {
            //                         var messageModel = new MessageModel
            //                         {
            //                             Content = e.DataFrame.ToText(),
            //                             Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //                             IsSend = false
            //                         };
            //
            //                         Application.Current.Dispatcher.Invoke(() =>
            //                         {
            //                             MessageCollection.Add(messageModel);
            //                         });
            //                     }
            //
            //                     return EasyTask.CompletedTask;
            //                 });
            //
            //                 //断开
            //                 cfg.Add(typeof(IWebSocketClosedPlugin), (IWebSocket webSocket, ClosedEventArgs e) =>
            //                 {
            //                     var session = webSocket.Client;
            //                     _clients.First(
            //                         x => x.Ip == session.IP && x.Port == session.Port
            //                     ).IsConnected = false;
            //                     return EasyTask.CompletedTask;
            //                 });
            //             });
            //         _webSocketServer.Setup(socketConfig);
            //         _webSocketServer.Start();
            //         ListenState = "停止";
            //         ListenStateColor = "Lime";
            //         WebSocketUrl =
            //             $"ws://{_localAddressCollection[_comboBoxSelectedIndex]}:{_listenPort}/{_requestPath}";
            //     }
            //     catch (Exception e)
            //     {
            //         MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     }
            // }
        }

        private void CopyWebSocketUrl()
        {
            // if (string.IsNullOrWhiteSpace(_webSocketUrl))
            // {
            //     MessageBox.Show("请先开启监听", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // Clipboard.SetText(_webSocketUrl);
        }

        private void ClientItemSelectionChanged(WebSocketClientModel client)
        {
            // if (client == null)
            // {
            //     return;
            // }
            //
            // client.MessageCount = 0;
            // _socketClient = client;
            // ConnectedClientAddress = $"{client.Ip}:{client.Port}";
            // MessageCollection.Clear();
            // // using (var dataBase = new DataBaseConnection())
            // // {
            // //     var queryResult = dataBase.Table<ClientMessageCache>()
            // //         .Where(x =>
            // //             x.ClientIp == _connectedClient.Ip &&
            // //             x.ClientPort == _connectedClient.Port &&
            // //             x.ClientType == ConnectionType.WebSocketClient
            // //         );
            // //     if (queryResult.Any())
            // //     {
            // //         IsContentViewVisible = "Visible";
            // //         IsEmptyImageVisible = "Collapsed";
            // //
            // //         foreach (var cache in queryResult)
            // //         {
            // //             var messageModel = new MessageModel
            // //             {
            // //                 Content = cache.MessageContent,
            // //                 Time = cache.Time,
            // //                 IsSend = cache.IsSend == 1
            // //             };
            // //
            // //             MessageCollection.Add(messageModel);
            // //         }
            // //     }
            // //     else
            // //     {
            // //         IsContentViewVisible = "Collapsed";
            // //         IsEmptyImageVisible = "Visible";
            // //     }
            // // }
        }

        private void SendMessage()
        {
            // if (string.IsNullOrWhiteSpace(_userInputText))
            // {
            //     MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void Send(bool isMainThread)
        {
            // _socketClient.WebSocket?.SendAsync(_userInputText);
            //     
            // // using (var dataBase = new DataBaseConnection())
            // // {
            // //     var cache = new ClientMessageCache
            // //     {
            // //         ClientIp = _connectedClient.Ip,
            // //         ClientPort = _connectedClient.Port,
            // //         ClientType = ConnectionType.WebSocketClient,
            // //         MessageContent = _userInputText,
            // //         Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            // //         IsSend = 1
            // //     };
            // //     dataBase.Insert(cache);
            // // }
            //     
            // var message = new MessageModel
            // {
            //     Content = _userInputText,
            //     Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            //     IsSend = true
            // };
            //
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
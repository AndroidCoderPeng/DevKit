using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class WsServerViewModel : BindableBase, IDialogAware
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

        private string _localPort = "24242";

        public string LocalPort
        {
            get => _localPort;
            set => SetProperty(ref _localPort, value);
        }

        private string _wsPath = string.Empty;

        public string WsPath
        {
            get => _wsPath;
            set => SetProperty(ref _wsPath, value);
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
        
        #endregion

        #region DelegateCommand

        

        #endregion

        // private TcpServerTransport _server;
        private DispatcherTimer _runningTimer;
        private DateTime _listenStartedAt;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private bool _isLoopSendInProgress;
        
        public WsServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
            if (_localHostSource.Any())
            {
                SelectedIp = _localHostSource.First();
            }
        }
    }
}
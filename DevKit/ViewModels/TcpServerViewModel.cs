using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevKit.DataService;
using DevKit.Models;
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

        private ObservableCollection<SocketMessage> _logs = new ObservableCollection<SocketMessage>();

        public ObservableCollection<SocketMessage> Logs
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

        public DelegateCommand<string> HostSelectedCommand { set; get; }
        public DelegateCommand ServerListenCommand { set; get; }
        public DelegateCommand<SocketClientModel> ClientItemClickedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        public TcpServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
        }
    }
}
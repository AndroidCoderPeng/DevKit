using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class UdpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "UDP客户端";

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
            PersistConfig();
            // StopCommandScript();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _stateTextColor = "#5F6B7A";

        public string StateTextColor
        {
            get => _stateTextColor;
            set => SetProperty(ref _stateTextColor, value);
        }

        private string _remoteAddress = string.Empty;

        public string RemoteAddress
        {
            get => _remoteAddress;
            set => SetProperty(ref _remoteAddress, value);
        }

        private string _remotePort = "9000";

        public string RemotePort
        {
            get => _remotePort;
            set => SetProperty(ref _remotePort, value);
        }

        private ObservableCollection<ExCommandConfig>
            _exCommandCollection = new ObservableCollection<ExCommandConfig>();

        public ObservableCollection<ExCommandConfig> ExCommandCollection
        {
            get => _exCommandCollection;
            set => SetProperty(ref _exCommandCollection, value);
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

        public long SendCount => _logs.Count(x => x.IsSend == 1);

        public long SendByteSize => _logs.Where(x => x.IsSend == 1).Sum(x => x.RawData?.Length ?? 0);

        public long ReceiveCount => _logs.Count(x => x.IsSend == 0);

        public long ReceiveByteSize => _logs.Where(x => x.IsSend == 0).Sum(x => x.RawData?.Length ?? 0);

        private bool _isLogHexSelected = true;

        public bool IsLogHexSelected
        {
            get => _isLogHexSelected;
            set
            {
                if (SetProperty(ref _isLogHexSelected, value))
                {
                    RaisePropertyChanged(nameof(IsLogAsciiSelected));
                    foreach (var log in Logs)
                    {
                        if (log.RawData != null)
                        {
                            log.Content = FormatLogContent(log.RawData);
                        }
                    }
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

        private string _lastDataTime = string.Empty;

        public string LastDataTime
        {
            get => _lastDataTime;
            set => SetProperty(ref _lastDataTime, value);
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

        #endregion

        #region DelegateCommand

        public DelegateCommand SetEndpointCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand<string> ExCommandItemSelectedCommand { set; get; }
        public DelegateCommand<ExCommandConfig> QuickSendCommand { set; get; }
        public DelegateCommand<ExCommandConfig> EditExCommand { set; get; }
        public DelegateCommand<ExCommandConfig> DeleteExCommand { set; get; }
        public DelegateCommand<string> CopyExCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SaveLogCommand { set; get; }
        public DelegateCommand ClearLogCommand { set; get; }
        public DelegateCommand<string> DisplayRadioChangedCommand { set; get; }
        public DelegateCommand<string> SendRadioChangedCommand { set; get; }
        public DelegateCommand<object> LoopSendSwitchCommand { set; get; }
        public DelegateCommand OpenScriptCommand { set; get; }

        #endregion

        private UdpClientTransport _transport;
        private CancellationTokenSource _transportCts;
        private readonly UdpConfig _config;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private bool _isLoopSendInProgress;
        private IEnumerator<ExCommandConfig> _commandEnumerator;
        private bool _isScriptCommandInProgress;
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();

        public UdpClientViewModel(IDialogService dialogService)
        {
            _config = SettingsStore.Load<UdpConfig>(ConfigSections.FileName, ConfigSections.Udp);
            RemoteAddress = _config.Server.Ip;
            RemotePort = _config.Server.Port;
            ExCommandCollection = _config.ExCommands.ToObservableCollection();

            _logs.CollectionChanged += Logs_CollectionChanged;

            SetEndpointCommand = new DelegateCommand(() =>
            {
                
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
        
        private string FormatLogContent(byte[] bytes)
        {
            return _isLogHexSelected ? BitConverter.ToString(bytes).Replace("-", " ") : Encoding.UTF8.GetString(bytes);
        }
        
        private void PersistConfig()
        {
            _config.Server.Ip = _remoteAddress;
            _config.Server.Port = _remotePort;
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Udp, _config);
        }
    }
}
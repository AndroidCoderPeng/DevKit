using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class TcpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "TCP客户端";

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

        private string _connectionState = "未连接";

        public string ConnectionState
        {
            get => _connectionState;
            set => SetProperty(ref _connectionState, value);
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

        private string _buttonStateText = "连接";

        public string ButtonStateText
        {
            get => _buttonStateText;
            set => SetProperty(ref _buttonStateText, value);
        }

        private string _sessionDuration = "00:00:00";

        public string SessionDuration
        {
            get => _sessionDuration;
            set => SetProperty(ref _sessionDuration, value);
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

        private string _lastDataTime = string.Empty;

        public string LastDataTime
        {
            get => _lastDataTime;
            set => SetProperty(ref _lastDataTime, value);
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
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

        public DelegateCommand ConnectServerCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand<string> ExCommandItemSelectedCommand { set; get; }
        public DelegateCommand<ExCommandConfig> QuickSendCommand { set; get; }
        public DelegateCommand<ExCommandConfig> EditExCommand { set; get; }
        public DelegateCommand<ExCommandConfig> DeleteExCommand { set; get; }
        public DelegateCommand<string> CopyExCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        public DelegateCommand SaveCommunicationCommand { set; get; }

        public DelegateCommand ClearCommunicationCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand OpenScriptCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        private ITransport _transport;

        private CancellationTokenSource _transportCts;
        private bool _isConnecting;
        private DateTime _sessionStartedAt;
        private DispatcherTimer _sessionTimer;

        /// <summary>
        /// TCP 分组的唯一数据源：Server（IP/端口）与 ExCommands（扩展指令）都在这里，
        /// 每次持久化都整体写回，杜绝快照互相覆盖
        /// </summary>
        private readonly TcpConfig _config;

        private DispatcherTimer _toastTimer;

        public TcpClientViewModel(IDialogService dialogService)
        {
            _config = SettingsStore.Load<TcpConfig>(ConfigSections.FileName, ConfigSections.Tcp);
            RemoteAddress = _config.Server.Ip;
            RemotePort = _config.Server.Port;
            ExCommandCollection = _config.ExCommands.ToObservableCollection();

            ConnectServerCommand = new DelegateCommand(() => _ = ConnectServerAsync());

            AddExtensionCommand = new DelegateCommand(() =>
            {
                dialogService.ShowDialog("ExCommandDialog", null, delegate(IDialogResult result)
                {
                    if (result.Result != ButtonResult.OK)
                    {
                        return;
                    }

                    var commandType = result.Parameters.GetValue<string>("Type");
                    var commandValue = result.Parameters.GetValue<string>("Value");
                    var commandMemo = result.Parameters.GetValue<string>("Memo");

                    _config.ExCommands.Add(new ExCommandConfig
                    {
                        Type = commandType,
                        Value = commandValue,
                        Memo = commandMemo
                    });
                    PersistConfig();
                    ExCommandCollection = _config.ExCommands.ToObservableCollection();
                });
            });

            ExCommandItemSelectedCommand = new DelegateCommand<string>(value => { UserInputText = value; });

            QuickSendCommand = new DelegateCommand<ExCommandConfig>(command => { _ = QuickSendAsync(command); });

            EditExCommand = new DelegateCommand<ExCommandConfig>(command =>
            {
                var dialogParameters = new DialogParameters
                {
                    { "ExCommand", command }
                };
                dialogService.ShowDialog("ExCommandDialog", dialogParameters, delegate(IDialogResult result)
                {
                    if (result.Result != ButtonResult.OK)
                    {
                        return;
                    }

                    var commandType = result.Parameters.GetValue<string>("Type");
                    var commandValue = result.Parameters.GetValue<string>("Value");
                    var commandMemo = result.Parameters.GetValue<string>("Memo");

                    command.Type = commandType;
                    command.Value = commandValue;
                    command.Memo = commandMemo;
                    PersistConfig();
                    ExCommandCollection = _config.ExCommands.ToObservableCollection();
                });
            });

            DeleteExCommand = new DelegateCommand<ExCommandConfig>(command =>
            {
                if (command == null || !_config.ExCommands.Remove(command))
                {
                    ShowToast("未找到要删除的扩展指令");
                    return;
                }

                ExCommandCollection.Remove(command);
                PersistConfig();
                ShowToast("扩展指令已删除");
            });

            CopyExCommand = new DelegateCommand<string>(value =>
            {
                Clipboard.SetText(value);
                ShowToast("已复制到剪贴板");
            });

            SendMessageCommand = new DelegateCommand(() => { _ = SendInputAsync(); });

            /////////////////////////////////////////////////////////////////////////////////
            SaveCommunicationCommand = new DelegateCommand(SaveCommunicationLog);
            ClearCommunicationCommand = new DelegateCommand(ClearCommunicationLog);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
            OpenScriptCommand = new DelegateCommand(OpenScriptDialog);
            TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
            ComboBoxItemSelectedCommand = new DelegateCommand<object>(OnComboBoxItemSelected);
        }

        private async Task ConnectServerAsync()
        {
            if (_isConnecting)
            {
                return;
            }

            if (_transport != null)
            {
                await _transport.DisconnectAsync();
                _transport.Dispose();
                _transport = null;
                return;
            }

            _isConnecting = true;
            _transportCts = new CancellationTokenSource();

            var transport = new TcpClientTransport(_remoteAddress, Convert.ToInt32(_remotePort));
            transport.StateChanged += delegate(TransportState state)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (state == TransportState.Connecting)
                    {
                        SetConnectionState("连接中");
                    }
                    else if (state == TransportState.Connected)
                    {
                        SetConnectionState("已连接");

                        // 开始计时
                        StartSessionTimer();
                    }
                    else if (state == TransportState.Disconnected)
                    {
                        SetConnectionState("未连接");

                        // 停止计时
                        StopSessionTimer();
                    }
                }));
            };
            transport.DataReceived += delegate(byte[] bytes)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTransportMessage("", bytes);
                }));
            };

            _transport = transport;

            try
            {
                await transport.ConnectAsync(_transportCts.Token);
            }
            catch (Exception e)
            {
                transport.Dispose();
                _transport = null;
                SetConnectionState("未连接");
                MessageBox.Show(e.Message, "连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private void UpdateTransportMessage(string command, byte[] bytes)
        {
            // TODO: 处理接收数据的逻辑
        }

        private void SetConnectionState(string state)
        {
            ConnectionState = state;
            ButtonStateText = state == "已连接" ? "断开" : "连接";

            if (state == "连接中")
            {
                StateOuterBackgroundColor = "#66F2994A";
                StateOuterBorderColor = "#1FF2994A";
                StateInnerBackgroundColor = "#FBE1C9";
                StateInnerBorderColor = "#F2994A";
                StateTextColor = "#F2994A";
            }
            else if (state == "已连接")
            {
                StateOuterBackgroundColor = "#5917A95C";
                StateOuterBorderColor = "#1F17A95C";
                StateInnerBackgroundColor = "#BEE7D1";
                StateInnerBorderColor = "#16A34A";
                StateTextColor = "#16A34A";
            }
            else
            {
                StateOuterBackgroundColor = "#FFF7F9FC";
                StateOuterBorderColor = "#FFEEEEF0";
                StateInnerBackgroundColor = "#E7EBF0";
                StateInnerBorderColor = "#93A0AE";
                StateTextColor = "#5F6B7A";
            }
        }

        private void StartSessionTimer()
        {
            StopSessionTimer();

            SessionDuration = "00:00:00";
            _sessionStartedAt = DateTime.Now;

            _sessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _sessionTimer.Tick += SessionTickEvent;
            _sessionTimer.Start();
        }

        private void StopSessionTimer()
        {
            if (_sessionTimer == null)
            {
                return;
            }

            _sessionTimer.Stop();
            _sessionTimer.Tick -= SessionTickEvent;
            _sessionTimer = null;

            UpdateSessionDuration();
        }

        private void SessionTickEvent(object sender, EventArgs e)
        {
            UpdateSessionDuration();
        }

        private void UpdateSessionDuration()
        {
            var elapsed = DateTime.Now - _sessionStartedAt;
            SessionDuration = $"{(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        /// <summary>
        /// 把当前 UI 上的 IP/端口 同步进 _config 后整体写回配置文件
        /// </summary>
        private void PersistConfig()
        {
            _config.Server.Ip = _remoteAddress;
            _config.Server.Port = _remotePort;
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Tcp, _config);
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

        private async Task QuickSendAsync(ExCommandConfig command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.Value))
            {
                MessageBox.Show("指令内容为空", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var transport = _transport;
            if (transport == null)
            {
                MessageBox.Show("请先连接 TCP 服务器", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] payload;
            if (command.Type == "HEX")
            {
                var tokens = command.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var bytes = new List<byte>();
                foreach (var token in tokens)
                {
                    if (!byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    {
                        MessageBox.Show("HEX 指令格式无效", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    bytes.Add(value);
                }

                payload = bytes.ToArray();
            }
            else if (command.Type == "ASCII")
            {
                payload = Encoding.UTF8.GetBytes(command.Value);
            }
            else
            {
                MessageBox.Show($"不支持的指令类型：{command.Type}", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await transport.SendAsync(payload, _transportCts?.Token ?? CancellationToken.None);
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                Logs.Add(new SocketMessage
                {
                    Content = command.Value,
                    Time = time,
                    IsSend = 1
                });
                LastDataTime = time;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendInputAsync()
        {
            var value = _userInputText;
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("请输入要发送的内容", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await QuickSendAsync(new ExCommandConfig
            {
                Type = _isHexSelected ? "HEX" : "ASCII",
                Value = value,
                Memo = "用户输入"
            });
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        private async void SaveCommunicationLog()
        {
            if (!_logs.Any())
            {
                MessageBox.Show("没有需要保存的日志", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Log文件(*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (fileDialog.ShowDialog() == true)
            {
                var savePath = fileDialog.FileName;
                try
                {
                    using (var writer = new StreamWriter(savePath))
                    {
                        foreach (var log in _logs)
                        {
                            var logText = log.IsSend == 1
                                ? $"{log.Time}【发送】{log.Content}"
                                : $"{log.Time}【接收】{log.Content}";
                            await writer.WriteLineAsync(logText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存日志时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearCommunicationLog()
        {
            Logs.Clear();
        }

        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
        }

        private void OpenScriptDialog()
        {
            // var dialogParameters = new DialogParameters();
            //
            // using (var dataBase = new DataBaseConnection())
            // {
            //     var commandCache = dataBase.Table<ExCommandCache>()
            //         .Where(x => x.ClientType == ClientType)
            //         .ToList();
            //     dialogParameters.Add("ExCommandCache", commandCache);
            // }
            //
            // _dialogService.Show("CommandScriptDialog", dialogParameters, delegate(IDialogResult result)
            // {
            //     if (result.Result != ButtonResult.OK)
            //     {
            //         return;
            //     }
            //
            //     var commands = result.Parameters.GetValue<List<string>>("SelectedCommands");
            //     var interval = result.Parameters.GetValue<string>("Interval");
            //     _commandEnumerator = commands.GetEnumerator();
            //     _scriptTimer.Tick += ScriptTimerTickEvent_Handler;
            //     _scriptTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(interval));
            //     _scriptTimer.Start();
            // });
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
            // if (!_tcpClient.Connected)
            // {
            //     MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // SendMessage(_userInputText);
        }

        private void OnComboBoxItemSelected(object index)
        {
            if (index == null)
            {
                return;
            }

            if (index.ToString().Equals("0"))
            {
                //转为16进制显示
                // foreach (var log in _logs)
                // {
                //     var bytes = log.Content.ToUtf8Bytes();
                //     log.Content = bytes.ByBytesToHexString(" ");
                // }
            }
            else if (index.ToString().Equals("1"))
            {
                //转为ASCII显示
                // foreach (var log in _logs)
                // {
                //     var bytes = log.Content.Replace(" ", "").ByHexStringToBytes();
                //     log.Content = Encoding.UTF8.GetString(bytes);
                // }
            }
        }
    }
}
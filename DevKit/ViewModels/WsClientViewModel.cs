using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Transports.Enums;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class WsClientViewModel : BindableBase, IDialogAware
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
            PersistConfig();
            StopCommandScript();
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

        private string _remotePort = "8080";

        public string RemotePort
        {
            get => _remotePort;
            set => SetProperty(ref _remotePort, value);
        }

        private string _wsPath = string.Empty;

        public string WsPath
        {
            get => _wsPath;
            set => SetProperty(ref _wsPath, value);
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

        private ObservableCollection<SocketFrameMessage> _logs = new ObservableCollection<SocketFrameMessage>();

        public ObservableCollection<SocketFrameMessage> Logs
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

        private bool _isLogHexSelected;

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

        private bool _isSendHexSelected;

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

        public string PlaceHolderText => IsSendAsciiSelected
            ? "输入要发送的内容：HEX 模式下使用空格分隔，例如 01 03 00 00 00 02"
            : "输入要发送的内容：ASCII 模式下将按 UTF-8 编码发送";

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

        public DelegateCommand ConnectServerCommand { set; get; }
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

        private WsClientTransport _transport;
        private bool _isConnecting;
        private DateTime _sessionStartedAt;
        private DispatcherTimer _sessionTimer;
        private readonly WebSocketConfig _config;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private bool _isLoopSendInProgress;
        private IEnumerator<ExCommandConfig> _commandEnumerator;
        private bool _isScriptCommandInProgress;
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();

        public WsClientViewModel(IDialogService dialogService)
        {
            _config = SettingsStore.Load<WebSocketConfig>(ConfigSections.FileName, ConfigSections.WebSocket);
            RemoteAddress = _config.Server.Ip;
            RemotePort = _config.Server.Port;
            WsPath = _config.Server.Path;
            ExCommandCollection = _config.ExCommands.ToObservableCollection();

            _logs.CollectionChanged += Logs_CollectionChanged;

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

            CopyLogCommand = new DelegateCommand<string>(log =>
            {
                Clipboard.SetText(log);
                ShowToast("已复制到剪贴板");
            });

            SaveLogCommand = new DelegateCommand(() =>
            {
                if (!_logs.Any())
                {
                    MessageBox.Show("没有需要保存的日志", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fileDialog = new SaveFileDialog
                {
                    DefaultExt = ".txt",
                    Filter = "Log文件(*.txt)|*.txt"
                };
                if (fileDialog.ShowDialog() != true) return;

                var savePath = fileDialog.FileName;
                using (var writer = new StreamWriter(savePath))
                {
                    foreach (var log in _logs)
                    {
                        var text = log.IsSend == 1 ? $"{log.Time}【发送】{log.Content}" : $"{log.Time}【接收】{log.Content}";
                        writer.WriteLine(text);
                    }
                }
            });

            ClearLogCommand = new DelegateCommand(() => { Logs.Clear(); });

            DisplayRadioChangedCommand = new DelegateCommand<string>(value =>
            {
                if (value == "0")
                {
                    IsLogHexSelected = true;
                }
                else if (value == "1")
                {
                    IsLogHexSelected = false;
                }
            });

            SendRadioChangedCommand = new DelegateCommand<string>(value =>
            {
                if (value == "0")
                {
                    IsSendHexSelected = true;
                }
                else if (value == "1")
                {
                    IsSendHexSelected = false;
                }
            });

            LoopSendSwitchCommand = new DelegateCommand<object>(value =>
            {
                if (!(value is bool isChecked)) return;

                if (isChecked)
                {
                    StartLoopSending();
                }
                else
                {
                    StopLoopSending();
                }
            });

            OpenScriptCommand = new DelegateCommand(() =>
            {
                var dialogParameters = new DialogParameters
                {
                    { "ExCommands", _exCommandCollection }
                };
                dialogService.ShowDialog("CommandScriptDialog", dialogParameters, delegate(IDialogResult result)
                {
                    if (result.Result != ButtonResult.OK)
                    {
                        return;
                    }

                    var commands = result.Parameters.GetValue<ObservableCollection<ExCommandConfig>>("Commands");
                    var interval = result.Parameters.GetValue<string>("Interval");

                    if (commands == null || commands.Count == 0)
                    {
                        ShowToast("请先添加脚本指令");
                        return;
                    }

                    if (!int.TryParse(interval, NumberStyles.None, CultureInfo.InvariantCulture, out var ms) || ms <= 0)
                    {
                        ShowToast("时间间隔必须是正整数");
                        return;
                    }

                    if (_transport == null || ConnectionState != "已连接")
                    {
                        ShowToast("请先连接 WebSocket 服务器");
                        return;
                    }

                    StopCommandScript();

                    _commandEnumerator = commands.GetEnumerator();
                    _scriptTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(interval));
                    _scriptTimer.Tick += ScriptTimerTickEvent_Handler;
                    _scriptTimer.Start();
                });
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

            var transport = new WsClientTransport(_remoteAddress, Convert.ToInt32(_remotePort), _wsPath);

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
                    else if (state == TransportState.Error)
                    {
                        SetConnectionState("未连接");
                        StopSessionTimer();
                    }
                }));
            };
            transport.FrameReceived += delegate(byte[] bytes, WsFrameKind kind)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateTransportMessage(bytes, kind)));
            };

            _transport = transport;

            try
            {
                await transport.ConnectAsync();
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

        private void UpdateTransportMessage(byte[] bytes, WsFrameKind kind)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            var rawData = (byte[])bytes.Clone();
            var time = DateTime.Now.ToString("HH:mm:ss.fff");

            Logs.Add(new SocketFrameMessage
            {
                RawData = rawData,
                Content = FormatLogContent(rawData),
                Time = time,
                IsSend = 0,
                FrameType = kind == WsFrameKind.Text ? "文本帧" : "二进制帧"
            });

            LastDataTime = time;
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

        private void PersistConfig()
        {
            _config.Server.Ip = _remoteAddress;
            _config.Server.Port = _remotePort;
            _config.Server.Path = _wsPath;
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.WebSocket, _config);
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
                MessageBox.Show("请先连接 WebSocket 服务器", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                await transport.SendAsync(payload);
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                Logs.Add(new SocketFrameMessage
                {
                    RawData = payload,
                    Content = command.Value,
                    Time = time,
                    IsSend = 1,
                    FrameType = command.Type == "HEX" ? "二进制帧" : "文本帧"
                });
                LastDataTime = time;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                Type = _isSendHexSelected ? "HEX" : "ASCII",
                Value = value,
                Memo = "用户输入"
            });
        }

        private void StartLoopSending()
        {
            if (!int.TryParse(_interval, NumberStyles.None, CultureInfo.InvariantCulture, out var ms) || ms <= 0)
            {
                MessageBox.Show("时间间隔必须是正整数", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            if (_transport == null || ConnectionState != "已连接")
            {
                MessageBox.Show("请先连接 WebSocket 服务器", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("请输入要发送的内容", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            _loopSendCommandTimer.Stop();
            _loopSendCommandTimer.Tick -= TimerTickEvent_Handler;
            _loopSendCommandTimer.Interval = TimeSpan.FromMilliseconds(ms);
            _loopSendCommandTimer.Tick += TimerTickEvent_Handler;
            _loopSendCommandTimer.Start();
        }

        private void StopLoopSending()
        {
            _loopSendCommandTimer.Stop();
            _loopSendCommandTimer.Tick -= TimerTickEvent_Handler;
        }

        private void TimerTickEvent_Handler(object sender, EventArgs e)
        {
            if (_isLoopSendInProgress)
                return;

            _isLoopSendInProgress = true;
            _ = SendLoopInputAsync();
        }

        private async Task SendLoopInputAsync()
        {
            try
            {
                await SendInputAsync();
            }
            catch (Exception ex)
            {
                StopLoopSending();
                IsLoopSendEnabled = false;

                MessageBox.Show($"循环发送失败：{ex.Message}", "发送失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoopSendInProgress = false;
            }
        }

        private void StopCommandScript()
        {
            _scriptTimer.Stop();
            _scriptTimer.Tick -= ScriptTimerTickEvent_Handler;
            _commandEnumerator?.Dispose();
            _commandEnumerator = null;
            _isScriptCommandInProgress = false;
        }

        private void ScriptTimerTickEvent_Handler(object sender, EventArgs e)
        {
            _ = ScriptTimerTickEvent_HandlerAsync();
        }

        private async Task ScriptTimerTickEvent_HandlerAsync()
        {
            if (_isScriptCommandInProgress)
            {
                return;
            }

            _isScriptCommandInProgress = true;
            try
            {
                if (_commandEnumerator == null || !_commandEnumerator.MoveNext())
                {
                    StopCommandScript();
                    return;
                }

                await QuickSendAsync(_commandEnumerator.Current);
            }
            catch (Exception ex)
            {
                StopCommandScript();
                MessageBox.Show($"脚本发送失败：{ex.Message}", "发送失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isScriptCommandInProgress = false;
            }
        }
    }
}
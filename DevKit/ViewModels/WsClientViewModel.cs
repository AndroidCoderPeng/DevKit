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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Dialogs;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Transports.Enums;
using DevKit.Utils;
using Microsoft.Win32;

namespace DevKit.ViewModels
{
    public partial class WsClientViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private string _stateOuterBackgroundColor = "#FFF7F9FC";
        [ObservableProperty] private string _stateOuterBorderColor = "#FFEEEEF0";
        [ObservableProperty] private string _stateInnerBackgroundColor = "#E7EBF0";
        [ObservableProperty] private string _stateInnerBorderColor = "#93A0AE";
        [ObservableProperty] private string _stateTextColor = "#5F6B7A";
        [ObservableProperty] private string _connectionState = "未连接";
        [ObservableProperty] private string _remoteAddress = string.Empty;
        [ObservableProperty] private string _remotePort = "8080";
        [ObservableProperty] private string _wsPath = string.Empty;
        [ObservableProperty] private string _buttonStateText = "连接";
        [ObservableProperty] private string _sessionDuration = "00:00:00";
        [ObservableProperty] private ObservableCollection<ExCommandConfig> _exCommandCollection = [];
        [ObservableProperty] private string _toastMessage;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private string _userInputText = string.Empty;
        [ObservableProperty] private ObservableCollection<SocketFrameMessage> _logs = [];
        [ObservableProperty] private string _lastDataTime = string.Empty;
        [ObservableProperty] private bool _isLoopSendEnabled;
        [ObservableProperty] private string _interval = "1000";

        public long SendCount => Logs.Count(x => x.IsSend == 1);
        public long SendByteSize => Logs.Where(x => x.IsSend == 1).Sum(x => x.RawData?.Length ?? 0);
        public long ReceiveCount => Logs.Count(x => x.IsSend == 0);
        public long ReceiveByteSize => Logs.Where(x => x.IsSend == 0).Sum(x => x.RawData?.Length ?? 0);

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLogAsciiSelected))]
        private bool _isLogHexSelected = true;

        public bool IsLogAsciiSelected
        {
            get => !IsLogHexSelected;
            set
            {
                if (value)
                {
                    IsLogHexSelected = false;
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSendAsciiSelected))]
        [NotifyPropertyChangedFor(nameof(PlaceHolderText))]
        private bool _isSendHexSelected;

        public bool IsSendAsciiSelected
        {
            get => !IsSendHexSelected;
            set
            {
                if (value)
                {
                    IsSendHexSelected = false;
                }
            }
        }

        public string PlaceHolderText => IsSendAsciiSelected
            ? "输入要发送的内容：HEX 模式下使用空格分隔，例如 01 03 00 00 00 02"
            : "输入要发送的内容：ASCII 模式下将按 UTF-8 编码发送";

        #endregion

        private readonly IDialogService _dialogService;
        private WsClientTransport _transport;
        private bool _isConnecting;
        private DateTime _sessionStartedAt;
        private DispatcherTimer _sessionTimer;
        private readonly WebSocketConfig _config;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new();
        private bool _isLoopSendInProgress;
        private IEnumerator<ExCommandConfig> _commandEnumerator;
        private bool _isScriptCommandInProgress;
        private readonly DispatcherTimer _scriptTimer = new();

        public WsClientViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _config = SettingsStore.Load<WebSocketConfig>(ConfigSections.FileName, ConfigSections.WebSocket);
            RemoteAddress = _config.Server.Ip;
            RemotePort = _config.Server.Port;
            WsPath = _config.Server.Path;
            ExCommandCollection = _config.ExCommands.ToObservableCollection();

            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseLogStatisticsChanged();
        }

        private void RaiseLogStatisticsChanged()
        {
            OnPropertyChanged(nameof(SendCount));
            OnPropertyChanged(nameof(SendByteSize));
            OnPropertyChanged(nameof(ReceiveCount));
            OnPropertyChanged(nameof(ReceiveByteSize));
        }

        [RelayCommand]
        private async Task ConnectServerAsync()
        {
            if (!int.TryParse(RemotePort, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
                port < 1 || port > 65535)
            {
                MessageBox.Show("端口必须是 1 到 65535 之间的正整数", "温馨提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            var transport = new WsClientTransport(RemoteAddress, port, WsPath);

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

        private string FormatLogContent(byte[] bytes)
        {
            return IsLogHexSelected ? BitConverter.ToString(bytes).Replace("-", " ") : Encoding.UTF8.GetString(bytes);
        }

        [RelayCommand]
        private void AddExtension()
        {
            var command = new ExCommandConfig();
            if (_dialogService.ShowDialog(typeof(ExCommandDialog), command) != true) return;

            _config.ExCommands.Add(command);
            PersistConfig();
            ExCommandCollection = _config.ExCommands.ToObservableCollection();
        }

        [RelayCommand]
        private void ExCommandItemSelected(string value)
        {
            UserInputText = value;
        }

        [RelayCommand]
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

        [RelayCommand]
        private void EditEx(ExCommandConfig command)
        {
            if (command == null) return;

            // 带参数
            if (_dialogService.ShowDialog(typeof(ExCommandDialog), command) != true) return;

            PersistConfig();
            ExCommandCollection = _config.ExCommands.ToObservableCollection();
        }

        private void PersistConfig()
        {
            _config.Server.Ip = RemoteAddress;
            _config.Server.Port = RemotePort;
            _config.Server.Path = WsPath;
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.WebSocket, _config);
        }

        [RelayCommand]
        private void DeleteEx(ExCommandConfig command)
        {
            if (command == null || !_config.ExCommands.Remove(command))
            {
                ShowToast("未找到要删除的扩展指令");
                return;
            }

            ExCommandCollection.Remove(command);
            PersistConfig();
            ShowToast("扩展指令已删除");
        }

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (_, _) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }

        [RelayCommand]
        private void CopyEx(string value)
        {
            Clipboard.SetText(value);
            ShowToast("已复制到剪贴板");
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            var value = UserInputText;
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("请输入要发送的内容", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await QuickSendAsync(new ExCommandConfig
            {
                Type = IsSendHexSelected ? "HEX" : "ASCII",
                Value = value,
                Memo = "用户输入"
            });
        }

        [RelayCommand]
        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
            ShowToast("已复制到剪贴板");
        }

        [RelayCommand]
        private void SaveLog()
        {
            if (!Logs.Any())
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
            using var writer = new StreamWriter(savePath);
            foreach (var log in Logs)
            {
                var text = log.IsSend == 1 ? $"{log.Time}【发送】{log.Content}" : $"{log.Time}【接收】{log.Content}";
                writer.WriteLine(text);
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            Logs.Clear();
        }

        [RelayCommand]
        private void DisplayRadioChanged(string value)
        {
            IsLogHexSelected = value switch
            {
                "0" => true,
                "1" => false,
                _ => IsLogHexSelected
            };
        }

        [RelayCommand]
        private void SendRadioChanged(string value)
        {
            IsSendHexSelected = value switch
            {
                "0" => true,
                "1" => false,
                _ => IsSendHexSelected
            };
        }

        [RelayCommand]
        private void LoopSendSwitch(object value)
        {
            if (value is not bool isChecked) return;

            if (isChecked)
            {
                StartLoopSending();
            }
            else
            {
                StopLoopSending();
            }
        }

        private void StartLoopSending()
        {
            if (!int.TryParse(Interval, NumberStyles.None, CultureInfo.InvariantCulture, out var ms) || ms <= 0)
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

            if (string.IsNullOrWhiteSpace(UserInputText))
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
                await SendMessageAsync();
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

        [RelayCommand]
        private void OpenScript()
        {
            // 带参数
            var context = new CommandScriptDialogContext(ExCommandCollection);
            if (_dialogService.ShowDialog(typeof(CommandScriptDialog), context) != true)
            {
                return;
            }

            var selectedCommands = context.SelectedCommands;
            if (selectedCommands.Count == 0)
            {
                ShowToast("请先添加脚本指令");
                return;
            }

            var interval = context.Interval;
            if (!int.TryParse(interval, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) ||
                milliseconds <= 0)
            {
                ShowToast("时间间隔必须是正整数");
                return;
            }

            if (_transport == null || ConnectionState != "已连接")
            {
                ShowToast("请先连接 TCP 服务器");
                return;
            }

            StopCommandScript();

            _commandEnumerator = selectedCommands.GetEnumerator();
            _scriptTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            _scriptTimer.Tick += ScriptTimerTickEvent_Handler;
            _scriptTimer.Start();
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

        public void OnDialogClosed()
        {
            PersistConfig();
            StopCommandScript();
            _ = _transport?.DisconnectAsync();
            _transport?.Dispose();
            _transport = null;
        }
    }
}
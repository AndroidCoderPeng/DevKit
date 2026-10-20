using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Transports.Enums;
using Microsoft.Win32;

namespace DevKit.ViewModels
{
    public partial class WsServerViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private string _stateOuterBackgroundColor = "#FFF7F9FC";
        [ObservableProperty] private string _stateOuterBorderColor = "#FFEEEEF0";
        [ObservableProperty] private string _stateInnerBackgroundColor = "#E7EBF0";
        [ObservableProperty] private string _stateInnerBorderColor = "#93A0AE";
        [ObservableProperty] private string _stateTextColor = "#5F6B7A";
        [ObservableProperty] private string _stateText = "未监听";
        [ObservableProperty] private string _selectedIp = string.Empty;
        [ObservableProperty] private List<string> _localHostSource = [];
        [ObservableProperty] private string _localPort = "24242";
        [ObservableProperty] private string _wsPath = string.Empty;
        [ObservableProperty] private string _listenButtonText = "开始监听";
        [ObservableProperty] private string _runningTime = "00:00:00";
        [ObservableProperty] private int _onlineClientCount;
        [ObservableProperty] private int _clientCount;
        [ObservableProperty] private int _unReadMessageCount;
        [ObservableProperty] private int _sendCount;
        [ObservableProperty] private int _sendByteSize;
        [ObservableProperty] private int _receiveCount;
        [ObservableProperty] private int _receiveByteSize;
        [ObservableProperty] private ObservableCollection<WsServerClient> _clientItems = [];
        [ObservableProperty] private ObservableCollection<SocketFrameMessage> _logs = [];
#nullable enable annotations
        [ObservableProperty] private WsServerClient? _selectedClient;
#nullable restore annotations
        [ObservableProperty] private string _userInputText = string.Empty;
        [ObservableProperty] private bool _isLoopSendEnabled;
        [ObservableProperty] private string _interval = "1000";
        [ObservableProperty] private bool _isMassSendEnabled;
        [ObservableProperty] private string _toastMessage;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private string _lastDataTime = string.Empty;

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
        private bool _isSendHexSelected = true;

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

        private WsServerTransport _server;
        private DispatcherTimer _runningTimer;
        private DateTime _listenStartedAt;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new();
        private bool _isLoopSendInProgress;

        public WsServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
            if (LocalHostSource.Count != 0)
            {
                SelectedIp = _localHostSource.First();
            }

            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        partial void OnIsLogHexSelectedChanged(bool value)
        {
            _ = value;
            foreach (var log in SelectedClient?.Logs ?? Enumerable.Empty<SocketMessage>())
            {
                if (log.RawData != null)
                {
                    log.Content = FormatLogContent(log.RawData);
                }
            }
        }

        private string FormatLogContent(byte[] bytes)
        {
            return IsLogHexSelected ? BitConverter.ToString(bytes).Replace("-", " ") : Encoding.UTF8.GetString(bytes);
        }

        partial void OnLogsChanging(ObservableCollection<SocketFrameMessage> oldValue,
            ObservableCollection<SocketFrameMessage> newValue)
        {
            ArgumentNullException.ThrowIfNull(newValue);
            oldValue.CollectionChanged -= Logs_CollectionChanged;
        }

        partial void OnLogsChanged(ObservableCollection<SocketFrameMessage> oldValue,
            ObservableCollection<SocketFrameMessage> newValue)
        {
            _ = oldValue;
            newValue.CollectionChanged += Logs_CollectionChanged;
            RaiseLogStatisticsChanged();
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
        private void CopyWsUrl()
        {
            var url = BuildWsUrl();
            Clipboard.SetText(url);
            ShowToast($"已复制：{url}");
        }

        private string BuildWsUrl()
        {
            return $"ws://{SelectedIp}:{LocalPort}{NormalizePath(WsPath)}";
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Trim();
            return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/" + normalized;
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
        private async Task StartListeningAsync()
        {
            if (_server == null)
            {
                _server = new WsServerTransport();
                _server.ClientConnected += OnClientConnected;
                _server.ClientClosed += OnClientClosed;
                _server.FrameReceived += OnClientFrameReceived;
            }

            if (_server.IsRunning)
            {
                await _server.StopAsync();
                ApplyListenState(ListenState.NotListening);
                StopRunningTimer();

                // 停止后清掉在线状态（StopAsync 会断开所有客户端）
                foreach (var item in ClientItems)
                {
                    item.IsConnected = false;
                }

                SelectedClient = null;
                RefreshUnreadMessageCount();
                OnlineClientCount = 0;
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedIp))
            {
                MessageBox.Show("请先选择监听地址", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(LocalPort, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
                || port < 1 || port > 65535)
            {
                MessageBox.Show("监听端口必须是 1-65535 的整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _server.StartAsync(SelectedIp, port, NormalizePath(WsPath), CancellationToken.None);
                ApplyListenState(ListenState.Listening);
                StartRunningTimer();
            }
            catch (Exception ex)
            {
                ApplyListenState(ListenState.Error);
                MessageBox.Show($"监听失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnClientConnected(ServerClient info)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Id 就是 "IP:Port"，重连时复用已有行，避免出现重复客户端
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client == null)
                {
                    client = new WsServerClient
                    {
                        Id = info.Id,
                        Ip = info.Ip,
                        Port = info.Port
                    };
                    ClientItems.Add(client);
                }

                client.WsPath = string.IsNullOrWhiteSpace(info.Path) ? "/" : info.Path;
                client.IsConnected = true;

                ClientCount = ClientItems.Count;
                OnlineClientCount = ClientItems.Count(x => x.IsConnected);
            }));
        }

        private void OnClientClosed(ServerClient info)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client != null)
                {
                    client.IsConnected = false;
                }

                OnlineClientCount = ClientItems.Count(x => x.IsConnected);
            }));
        }

        private void OnClientFrameReceived(ServerClient info, byte[] bytes, WsFrameKind kind)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client == null)
                {
                    return;
                }

                var isViewing = ReferenceEquals(SelectedClient, client);
                var time = DateTime.Now.ToString("HH:mm:ss.fff");

                client.Logs.Add(new SocketFrameMessage()
                {
                    RawData = bytes,
                    Content = FormatLogContent(bytes),
                    Time = time,
                    IsSend = 0,
                    IsRead = isViewing,
                    FrameType = kind == WsFrameKind.Text ? "文本帧" : "二进制帧"
                });
                client.MessageCount++;

                ReceiveCount++;
                ReceiveByteSize += bytes.Length;
                LastDataTime = time;

                RefreshUnreadMessageCount();
            }));
        }

        private void RefreshUnreadMessageCount()
        {
            // 面板上的“未读消息” = 所有客户端未读数之和
            UnReadMessageCount = ClientItems.Sum(x => x.UnreadLogCount);
        }

        private void ApplyListenState(ListenState state)
        {
            switch (state)
            {
                case ListenState.Listening:
                    StateText = "已监听";
                    ListenButtonText = "停止监听";
                    StateOuterBackgroundColor = "#5917A95C";
                    StateOuterBorderColor = "#1F17A95C";
                    StateInnerBackgroundColor = "#BEE7D1";
                    StateInnerBorderColor = "#16A34A";
                    StateTextColor = "#16A34A";
                    break;

                case ListenState.Error:
                    StateText = "监听失败";
                    ListenButtonText = "开始监听";
                    StateOuterBackgroundColor = "#66F2994A";
                    StateOuterBorderColor = "#1FF2994A";
                    StateInnerBackgroundColor = "#FBE1C9";
                    StateInnerBorderColor = "#F2994A";
                    StateTextColor = "#F2994A";
                    break;

                case ListenState.NotListening:
                default:
                    StateText = "未监听";
                    ListenButtonText = "开始监听";
                    StateOuterBackgroundColor = "#FFF7F9FC";
                    StateOuterBorderColor = "#FFEEEEF0";
                    StateInnerBackgroundColor = "#E7EBF0";
                    StateInnerBorderColor = "#93A0AE";
                    StateTextColor = "#5F6B7A";
                    break;
            }
        }

        private void StartRunningTimer()
        {
            StopRunningTimer();

            RunningTime = "00:00:00";
            _listenStartedAt = DateTime.Now;

            _runningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _runningTimer.Tick += RunningTimerTick;
            _runningTimer.Start();
        }

        private void StopRunningTimer()
        {
            if (_runningTimer == null)
            {
                return;
            }

            _runningTimer.Stop();
            _runningTimer.Tick -= RunningTimerTick;
            _runningTimer = null;
        }

        private void RunningTimerTick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _listenStartedAt;
            RunningTime = $"{(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        [RelayCommand]
        private async Task DisconnectAll()
        {
            if (_server is not { IsRunning: true })
            {
                MessageBox.Show("请先启动 WebSocket 服务", "无法断开", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _server.DisconnectAll();
        }

        [RelayCommand]
        private void DisconnectClient(WsServerClient client)
        {
            if (_server is not { IsRunning: true })
            {
                ShowToast("服务未启动");
                return;
            }

            if (client == null)
            {
                ShowToast("请先选择一个客户端");
                return;
            }

            _ = _server.DisconnectClient(client.Id);
        }

        [RelayCommand]
        private void ClientItemSelected(WsServerClient client)
        {
            if (client == null)
            {
                return;
            }

            if (!client.IsConnected)
            {
                ShowToast($"{client.Ip}:{client.Port} 已断开");
                return;
            }

            SelectedClient = client;

            // 切到该客户端 = 查看，未读清零
            client.MarkAllRead();
            Logs = client.Logs;
            RefreshUnreadMessageCount();
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
        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
            ShowToast("已复制到剪贴板");
        }

        [RelayCommand]
        private void SaveLog()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("请先选择一个客户端", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!SelectedClient.Logs.Any())
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

            using var writer = new StreamWriter(fileDialog.FileName);
            foreach (var log in SelectedClient.Logs)
            {
                var text = log.IsSend == 1 ? $"{log.Time}【发送】{log.Content}" : $"{log.Time}【接收】{log.Content}";
                writer.WriteLine(text);
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("请先选择一个客户端", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!SelectedClient.Logs.Any())
            {
                MessageBox.Show("没有需要清除的日志", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("确定要清除该客户端的所有日志吗？", "温馨提示", MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            SelectedClient.Logs.Clear();
            RefreshUnreadMessageCount();
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

            var server = _server;
            if (server == null || !server.IsRunning)
            {
                MessageBox.Show("请先启动 WebSocket 服务", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] payload;
            WsFrameKind kind;
            if (IsSendHexSelected)
            {
                var tokens = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var bytes = new List<byte>();

                foreach (var token in tokens)
                {
                    if (!byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsedByte))
                    {
                        MessageBox.Show("HEX 格式无效，请使用空格分隔字节，例如：AA 55 01 01", "无法发送", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    bytes.Add(parsedByte);
                }

                payload = [.. bytes];
                kind = WsFrameKind.Binary;
            }
            else
            {
                payload = Encoding.UTF8.GetBytes(value);
                kind = WsFrameKind.Text;
            }

            List<WsServerClient> targets;
            if (IsMassSendEnabled)
            {
                targets = ClientItems.Where(client => client.IsConnected).ToList();
                if (targets.Count == 0)
                {
                    MessageBox.Show("当前没有在线客户端", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                var selectedClient = SelectedClient;
                if (selectedClient is not { IsConnected: true })
                {
                    MessageBox.Show("请先选择一个在线客户端，或启用群发全部", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                targets = [selectedClient];
            }

            var failures = new List<string>();
            var successCount = 0;
            foreach (var client in targets)
            {
                try
                {
                    await server.SendAsync(client.Id, payload, kind);

                    var time = DateTime.Now.ToString("HH:mm:ss.fff");
                    client.Logs.Add(new SocketFrameMessage
                    {
                        RawData = payload,
                        Content = FormatLogContent(payload),
                        Time = time,
                        IsSend = 1,
                        IsRead = true,
                        FrameType = kind == WsFrameKind.Text ? "文本帧" : "二进制帧"
                    });

                    SendCount++;
                    SendByteSize += payload.Length;
                    LastDataTime = time;
                    successCount++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{client.Ip}:{client.Port}：{ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                var title = successCount == 0 ? "发送失败" : "部分发送失败";
                var message = string.Join(Environment.NewLine, failures);
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            if (_server == null || !_server.IsRunning)
            {
                MessageBox.Show("请先启动 WebSocket 服务", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            if (IsMassSendEnabled)
            {
                if (!ClientItems.Any(client => client.IsConnected))
                {
                    MessageBox.Show("当前没有在线客户端", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoopSendEnabled = false;
                    return;
                }
            }
            else if (SelectedClient is not { IsConnected: true })
            {
                MessageBox.Show("请先选择一个在线客户端，或启用群发全部", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            var value = UserInputText;
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("请输入要发送的内容", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            if (IsSendHexSelected)
            {
                var tokens = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) continue;

                    MessageBox.Show("HEX 格式无效，请使用空格分隔字节，例如：AA 55 01 01", "无法循环发送", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    IsLoopSendEnabled = false;
                    return;
                }
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

        public void OnDialogClosed()
        {
            StopLoopSending();
            StopRunningTimer();
            _toastTimer?.Stop();

            _server?.Dispose();
            _server = null;
        }
    }
}
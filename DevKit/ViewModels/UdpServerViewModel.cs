using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    public partial class UdpServerViewModel : ObservableObject
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
        [ObservableProperty] private string _localPort = "21212";
        [ObservableProperty] private string _idleTimeout = "60";
        [ObservableProperty] private string _listenButtonText = "开始监听";
        [ObservableProperty] private string _runningTime = "00:00:00";
        [ObservableProperty] private int _activeClientCount;
        [ObservableProperty] private int _clientCount;
        [ObservableProperty] private int _unReadMessageCount;
        [ObservableProperty] private int _sendCount;
        [ObservableProperty] private int _sendByteSize;
        [ObservableProperty] private int _receiveCount;
        [ObservableProperty] private int _receiveByteSize;
        [ObservableProperty] private ObservableCollection<UdpServerClient> _clientItems = [];
        [ObservableProperty] private ObservableCollection<SocketMessage> _logs = [];
#nullable enable annotations
        [ObservableProperty] private UdpServerClient? _selectedClient;
#nullable restore annotations
        [ObservableProperty] private string _userInputText = string.Empty;
        [ObservableProperty] private bool _isLoopSendEnabled;
        [ObservableProperty] private string _interval = "1000";
        [ObservableProperty] private bool _isBroadcastAllEnabled;
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

        private UdpServerTransport _server;
        private DispatcherTimer _runningTimer;
        private DateTime _listenStartedAt;
        private DispatcherTimer _toastTimer;
        private readonly DispatcherTimer _loopSendCommandTimer = new();
        private bool _isLoopSendInProgress;

        public UdpServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
            if (LocalHostSource.Count != 0)
            {
                SelectedIp = LocalHostSource.First();
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

        partial void OnLogsChanging(ObservableCollection<SocketMessage> oldValue,
            ObservableCollection<SocketMessage> newValue)
        {
            ArgumentNullException.ThrowIfNull(newValue);
            oldValue.CollectionChanged -= Logs_CollectionChanged;
        }

        partial void OnLogsChanged(ObservableCollection<SocketMessage> oldValue,
            ObservableCollection<SocketMessage> newValue)
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
        private async Task StartListeningAsync()
        {
            if (_server == null)
            {
                _server = new UdpServerTransport();
                _server.DataReceived += OnClientDataReceived;
            }

            if (!_server.IsRunning)
            {
                if (!int.TryParse(LocalPort, out var port) || port < 1 || port > 65535)
                {
                    ApplyListenState(ListenState.Error);
                    MessageBox.Show("端口必须是 1 到 65535 之间的整数", "端口无效", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    await _server.StartAsync(SelectedIp, port, CancellationToken.None);
                    ApplyListenState(ListenState.Listening);
                    StartRunningTimer();
                }
                catch (Exception ex)
                {
                    ApplyListenState(ListenState.Error);
                    MessageBox.Show($"监听失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await _server.StopAsync();
                ApplyListenState(ListenState.NotListening);
                StopRunningTimer();

                // UDP 没有「断开」事件：停止监听后列表里的来源不再有活跃可言
                var now = DateTime.Now;
                foreach (var client in ClientItems)
                {
                    client.IsActive = false;
                    client.LastActiveTime = (int)(now - client.LastReceivedAt).TotalMinutes;
                }

                ActiveClientCount = 0;
                UnReadMessageCount = 0;
                SelectedClient = null;
                Logs = [];
            }
        }

        private void OnClientDataReceived(ServerClient info, byte[] bytes)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 「新来源」= 收到过数据但列表里没有；增删都是 UI 侧的事
                var client = ClientItems.FirstOrDefault(x => x.Id == info.Id);
                if (client == null)
                {
                    client = new UdpServerClient { Id = info.Id, Ip = info.Ip, Port = info.Port };
                    ClientItems.Add(client);
                    ClientCount = ClientItems.Count;
                }

                client.LastReceivedAt = DateTime.Now;
                client.IsActive = true; // 收到的瞬间算活跃，静默由空闲超时 Timer 判定

                var isViewing = ReferenceEquals(SelectedClient, client);
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                client.Logs.Add(new SocketMessage
                {
                    RawData = bytes,
                    Content = FormatLogContent(bytes),
                    Time = time,
                    IsSend = 0,
                    IsRead = isViewing // 正在看 → 直接算已读；否则进未读
                });
                client.MessageCount++;

                ReceiveCount++;
                ReceiveByteSize += bytes.Length;
                LastDataTime = time;

                ActiveClientCount = ClientItems.Count(x => x.IsActive);
                RefreshUnreadMessageCount();
            }));
        }

        private void RefreshUnreadMessageCount()
        {
            // 统计面板上的"未读消息"= 所有客户端未读数之和
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
                    ListenButtonText = "开始监听"; // 保持可重试
                    StateOuterBackgroundColor = "#66F2994A";
                    StateOuterBorderColor = "#1FF2994A";
                    StateInnerBackgroundColor = "#FBE1C9";
                    StateInnerBorderColor = "#F2994A";
                    StateTextColor = "#F2994A";
                    break;

                case ListenState.NotListening:
                default: // NotListening
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

            // UDP 没有「断开」事件，来源的活跃/静默只能在这里按空闲超时推断
            var idleSeconds = GetIdleTimeoutSeconds();
            var now = DateTime.Now;
            foreach (var client in ClientItems)
            {
                var seconds = (now - client.LastReceivedAt).TotalSeconds;
                if (seconds <= idleSeconds)
                {
                    client.IsActive = true;
                    client.LastMessageTime = (int)seconds; // 活跃 · N 秒前收到的数据
                }
                else
                {
                    client.IsActive = false;
                    client.LastActiveTime = (int)(seconds / 60); // 静默 · N 分钟前收到的数据
                }
            }

            ActiveClientCount = ClientItems.Count(x => x.IsActive);
        }

        /// <summary>
        /// XAML 的空闲超时是 MaxLength=3，合法值 1~999；非法输入按 1 秒兜底
        /// </summary>
        private int GetIdleTimeoutSeconds()
        {
            if (!int.TryParse(IdleTimeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                || seconds < 1)
            {
                seconds = 1;
            }

            return seconds;
        }

        [RelayCommand]
        private void ClearAllClient()
        {
            // 清空来源列表是纯界面操作，不需要服务处于监听中
            ClientItems.Clear();
            ClientCount = 0;
            ActiveClientCount = 0;
            UnReadMessageCount = 0;
            SelectedClient = null;
            Logs = [];
        }

        [RelayCommand]
        private void CopyAddress(UdpServerClient client)
        {
            if (client == null)
            {
                return;
            }

            var address = $"{client.Ip}:{client.Port}";
            Clipboard.SetText(address);
            ShowToast($"已复制 {address}");
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
        private void RemoveClient(UdpServerClient client)
        {
            if (client == null)
            {
                return;
            }

            var wasSelected = ReferenceEquals(SelectedClient, client);
            if (!ClientItems.Remove(client))
            {
                return;
            }

            ClientCount = ClientItems.Count;
            ActiveClientCount = ClientItems.Count(x => x.IsActive);

            if (wasSelected)
            {
                // 已被移除的来源不能再当发送/查看目标
                SelectedClient = null;
                Logs = [];
            }

            RefreshUnreadMessageCount();
        }

        [RelayCommand]
        private void ClientItemSelected(UdpServerClient client)
        {
            if (client == null)
            {
                return;
            }

            if (!client.IsActive)
            {
                ShowToast($"{client.Ip}:{client.Port} 已静默");
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

            var savePath = fileDialog.FileName;
            using var writer = new StreamWriter(savePath);
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
        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
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

            var server = _server;
            if (server is not { IsRunning: true })
            {
                MessageBox.Show("请先启动 UDP 服务", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] payload;
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
            }
            else
            {
                payload = Encoding.UTF8.GetBytes(value);
            }

            List<UdpServerClient> targets;
            if (IsBroadcastAllEnabled)
            {
                targets = [.. ClientItems.Where(client => client.IsActive)];
                if (targets.Count == 0)
                {
                    MessageBox.Show("当前没有活跃客户端", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                var selectedClient = SelectedClient;
                if (selectedClient is not { IsActive: true })
                {
                    MessageBox.Show("请先选择一个活跃客户端，或启用广播全部", "无法发送", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    if (!IPAddress.TryParse(client.Ip, out var ipAddress) || ipAddress.AddressFamily != AddressFamily.InterNetwork)
                    {
                        failures.Add($"{client.Ip}:{client.Port}：IP 地址无效");
                        continue;
                    }

                    var remote = new IPEndPoint(ipAddress, client.Port);
                    await server.SendAsync(remote, payload);

                    var time = DateTime.Now.ToString("HH:mm:ss.fff");
                    client.Logs.Add(new SocketMessage
                    {
                        RawData = payload,
                        Content = FormatLogContent(payload),
                        Time = time,
                        IsSend = 1,
                        IsRead = true
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

            if (_server is not { IsRunning: true })
            {
                MessageBox.Show("请先启动 UDP 服务", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsLoopSendEnabled = false;
                return;
            }

            if (IsBroadcastAllEnabled)
            {
                if (!ClientItems.Any(client => client.IsActive))
                {
                    MessageBox.Show("当前没有活跃客户端", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoopSendEnabled = false;
                    return;
                }
            }
            else if (SelectedClient is not { IsActive: true })
            {
                MessageBox.Show("请先选择一个活跃客户端，或启用广播全部", "无法循环发送", MessageBoxButton.OK, MessageBoxImage.Warning);
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
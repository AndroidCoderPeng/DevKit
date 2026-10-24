using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Transports.Enums;
using DevKit.Utils;
using TouchSocket.SerialPorts;

namespace DevKit.ViewModels
{
    public partial class SerialPortViewModel : ObservableObject
    {
        #region VM

        [ObservableProperty] private string _stateOuterBackgroundColor = "#FFF7F9FC";
        [ObservableProperty] private string _stateOuterBorderColor = "#FFEEEEF0";
        [ObservableProperty] private string _stateInnerBackgroundColor = "#E7EBF0";
        [ObservableProperty] private string _stateInnerBorderColor = "#93A0AE";
        [ObservableProperty] private string _stateTextColor = "#5F6B7A";
        [ObservableProperty] private string _stateText = "未打开";
        [ObservableProperty] private List<string> _devicePortSource = [.. SerialPort.GetPortNames()];
        [ObservableProperty] private string _selectedPort = string.Empty;

        [ObservableProperty] private List<string> _baudRateSource =
            ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600", "自定义"];

        [ObservableProperty] private string _selectedBaudRate = string.Empty;

        [ObservableProperty] private List<string> _dataBitSource = ["5", "6", "7", "8"];
        [ObservableProperty] private string _selectedDataBit = string.Empty;
        [ObservableProperty] private List<string> _stopBitSource = ["1", "1.5", "2"];
        [ObservableProperty] private string _selectedStopBit = string.Empty;
        [ObservableProperty] private List<string> _parityBitSource = ["无", "奇", "偶", "1", "0"];
        [ObservableProperty] private string _selectedParityBit = string.Empty;
        [ObservableProperty] private List<string> _flowControlSource = ["无", "XON/XOFF", "RTS/CTS", "DSR/DTR"];
        [ObservableProperty] private string _selectedFlowControl = string.Empty;
        [ObservableProperty] private string _buttonStateText = "打开串口";
        [ObservableProperty] private string _sessionDuration = "00:00:00";
        [ObservableProperty] private ObservableCollection<ExCommandConfig> _exCommandCollection = [];
        [ObservableProperty] private ObservableCollection<SerialPortMessage> _logs = [];
        [ObservableProperty] private string _userInputText = string.Empty;
        [ObservableProperty] private List<string> _delimiterSource = ["无", "\\r\\n", "\\r", "\\n"];
        [ObservableProperty] private string _selectedDelimiter = string.Empty;
        [ObservableProperty] private bool _isLoopSendEnabled;
        [ObservableProperty] private string _interval = "1000";
        [ObservableProperty] private string _toastMessage;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private string _lastDataTime = string.Empty;

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

        private SerialPortTransport _transport;
        private bool _isConnecting;
        private DateTime _sessionStartedAt;
        private DispatcherTimer _sessionTimer;
        private readonly SerialPortConfig _config;
        private object _transportConnectionToken;

        public SerialPortViewModel()
        {
            if (DevicePortSource.Count != 0)
            {
                SelectedPort = DevicePortSource.First();
            }

            SelectedBaudRate = BaudRateSource[7]; // 默认选择 115200
            SelectedDataBit = DataBitSource[3]; // 默认选择 8
            SelectedStopBit = StopBitSource[0]; // 默认选择 1
            SelectedParityBit = ParityBitSource[0]; // 默认选择 无
            SelectedFlowControl = FlowControlSource[0]; // 默认选择 无
            SelectedDelimiter = DelimiterSource[0]; // 默认选择 无

            _config = SettingsStore.Load<SerialPortConfig>(ConfigSections.FileName, ConfigSections.SerialPort);
            ExCommandCollection = _config.ExCommands.ToObservableCollection();

            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        partial void OnLogsChanging(ObservableCollection<SerialPortMessage> oldValue,
            ObservableCollection<SerialPortMessage> newValue)
        {
            _ = oldValue;
            ArgumentNullException.ThrowIfNull(newValue);
            oldValue.CollectionChanged -= Logs_CollectionChanged;
        }

        partial void OnLogsChanged(ObservableCollection<SerialPortMessage> oldValue,
            ObservableCollection<SerialPortMessage> newValue)
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

        partial void OnIsLogHexSelectedChanged(bool value)
        {
            _ = value;
            foreach (var log in Logs)
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

        [RelayCommand]
        private async Task ConnectDeviceAsync()
        {
            if (_isConnecting)
            {
                return;
            }

            if (_transport != null)
            {
                var currentTransport = _transport;
                var connectionToken = _transportConnectionToken;

                await currentTransport.DisconnectAsync();

                if (ReferenceEquals(_transport, currentTransport) &&
                    ReferenceEquals(_transportConnectionToken, connectionToken))
                {
                    _transport = null;
                    _transportConnectionToken = null;
                }

                currentTransport.Dispose();
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPort))
            {
                MessageBox.Show("请先选择串口", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isConnecting = true;
            
            var transport = new SerialPortTransport();
            transport.SetRemoteEndPoint(SelectedPort, 0);
            transport.SetSerialPortOption(BuildSerialPortOption());
            var token = new object();
            _transport = transport;
            _transportConnectionToken = token;

            transport.StateChanged += delegate(TransportState state)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!ReferenceEquals(_transportConnectionToken, token))
                    {
                        return;
                    }

                    switch (state)
                    {
                        case TransportState.Connecting:
                            SetConnectionState("打开中");
                            break;
                        case TransportState.Connected:
                            SetConnectionState("已打开");
                            StartSessionTimer();
                            break;
                        case TransportState.Disconnected or TransportState.Error:
                            SetConnectionState("未打开");
                            StopSessionTimer();

                            var failedTransport = _transport;
                            _transport = null;
                            _transportConnectionToken = null;
                            failedTransport?.Dispose();

                            // StopLoopSending();
                            // IsLoopSendEnabled = false;
                            // StopCommandScript();
                            break;
                    }
                }));
            };

            transport.DataReceived += delegate(byte[] bytes)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => { UpdateTransportMessage(bytes); }));
            };

            try
            {
                await transport.ConnectAsync();
            }
            catch (Exception e)
            {
                transport.Dispose();

                if (ReferenceEquals(_transportConnectionToken, token))
                {
                    _transport = null;
                    _transportConnectionToken = null;
                    SetConnectionState("未打开");
                    StopSessionTimer();
                    
                    // StopLoopSending();
                    // IsLoopSendEnabled = false;
                    // StopCommandScript();
                }

                MessageBox.Show(e.Message, "串口打开失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private SerialPortOption BuildSerialPortOption()
        {
            var parity = SelectedParityBit switch
            {
                "奇" => Parity.Odd,
                "偶" => Parity.Even,
                "1" => Parity.Mark,
                "0" => Parity.Space,
                _ => Parity.None
            };

            var stopBits = SelectedStopBit switch
            {
                "1.5" => StopBits.OnePointFive,
                "2" => StopBits.Two,
                _ => StopBits.One
            };

            var handshake = SelectedFlowControl switch
            {
                "XON/XOFF" => Handshake.XOnXOff,
                "RTS/CTS" => Handshake.RequestToSend,
                _ => Handshake.None // DSR/DTR 在 System.IO.Ports.Handshake 中不存在，按无流控处理
            };

            return new SerialPortOption
            {
                BaudRate = int.TryParse(SelectedBaudRate, out var baud) ? baud : 115200,
                DataBits = int.TryParse(SelectedDataBit, out var bits) ? bits : 8,
                Parity = parity,
                StopBits = stopBits,
                Handshake = handshake
            };
        }

        private void SetConnectionState(string state)
        {
            StateText = state;
            ButtonStateText = state == "已打开" ? "关闭串口" : "打开串口";
            
            switch (state)
            {
                case "打开中":
                    StateOuterBackgroundColor = "#66F2994A";
                    StateOuterBorderColor = "#1FF2994A";
                    StateInnerBackgroundColor = "#FBE1C9";
                    StateInnerBorderColor = "#F2994A";
                    StateTextColor = "#F2994A";
                    break;
                case "已打开":
                    StateOuterBackgroundColor = "#5917A95C";
                    StateOuterBorderColor = "#1F17A95C";
                    StateInnerBackgroundColor = "#BEE7D1";
                    StateInnerBorderColor = "#16A34A";
                    StateTextColor = "#16A34A";
                    break;
                default:
                    StateOuterBackgroundColor = "#FFF7F9FC";
                    StateOuterBorderColor = "#FFEEEEF0";
                    StateInnerBackgroundColor = "#E7EBF0";
                    StateInnerBorderColor = "#93A0AE";
                    StateTextColor = "#5F6B7A";
                    break;
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
        
        private void UpdateTransportMessage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            var rawData = (byte[])bytes.Clone();
            var time = DateTime.Now.ToString("HH:mm:ss.fff");

            Logs.Add(new SerialPortMessage
            {
                RawData = rawData,
                Content = FormatLogContent(rawData),
                Time = time,
                IsSend = 0
            });

            LastDataTime = time;
        }

        public void OnDialogClosed()
        {
        }
    }
}
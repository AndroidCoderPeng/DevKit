using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Timers;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class SerialPortCommunicateViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<MessageModel> _messageCollection = new ObservableCollection<MessageModel>();

        public ObservableCollection<MessageModel> MessageCollection
        {
            set
            {
                _messageCollection = value;
                RaisePropertyChanged();
            }
            get => _messageCollection;
        }

        private string[] _portArray;

        public string[] PortArray
        {
            set
            {
                _portArray = value;
                RaisePropertyChanged();
            }
            get => _portArray;
        }

        private List<int> _baudRateArray;

        public List<int> BaudRateArray
        {
            set
            {
                _baudRateArray = value;
                RaisePropertyChanged();
            }
            get => _baudRateArray;
        }

        private List<int> _dataBitArray;

        public List<int> DataBitArray
        {
            set
            {
                _dataBitArray = value;
                RaisePropertyChanged();
            }
            get => _dataBitArray;
        }

        private List<Parity> _parityArray;

        public List<Parity> ParityArray
        {
            set
            {
                _parityArray = value;
                RaisePropertyChanged();
            }
            get => _parityArray;
        }

        private List<StopBits> _stopBitArray;

        public List<StopBits> StopBitArray
        {
            set
            {
                _stopBitArray = value;
                RaisePropertyChanged();
            }
            get => _stopBitArray;
        }

        private string _buttonState = "打开串口";

        public string ButtonState
        {
            set
            {
                _buttonState = value;
                RaisePropertyChanged();
            }
            get => _buttonState;
        }

        private bool _showHex = true;

        public bool ShowHex
        {
            set
            {
                _showHex = value;
                RaisePropertyChanged();
            }
            get => _showHex;
        }

        private bool _loopSend;

        public bool LoopSend
        {
            set
            {
                _loopSend = value;
                RaisePropertyChanged();
            }
            get => _loopSend;
        }

        private long _commandInterval = 1000;

        public long CommandInterval
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

        private bool _sendHex = true;

        public bool SendHex
        {
            set
            {
                _sendHex = value;
                RaisePropertyChanged();
            }
            get => _sendHex;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> PortItemSelectedCommand { set; get; }
        public DelegateCommand<object> BaudRateItemSelectedCommand { set; get; }
        public DelegateCommand<object> DataBitItemSelectedCommand { set; get; }
        public DelegateCommand<object> ParityItemSelectedCommand { set; get; }
        public DelegateCommand<object> StopBitItemSelectedCommand { set; get; }
        public DelegateCommand OpenSerialPortCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand LoopUncheckedCommand { set; get; }
        public DelegateCommand SendHexCheckedCommand { set; get; }
        public DelegateCommand SendHexUncheckedCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly Timer _loopSendMessageTimer = new Timer();
        private readonly SerialPortKit _serialPortKit = new SerialPortKit();
        private ClientConfigCache _clientCache;
        private string _portName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;

        public SerialPortCommunicateViewModel(IAppDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            PortArray = SerialPort.GetPortNames();
            BaudRateArray = new List<int> { 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 230400 };
            DataBitArray = new List<int> { 5, 6, 7, 8 };
            ParityArray = new List<Parity> { Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space };
            StopBitArray = new List<StopBits> { StopBits.None, StopBits.One, StopBits.Two, StopBits.OnePointFive };

            InitDefaultConfig();

            PortItemSelectedCommand = new DelegateCommand<string>(PortItemSelected);
            BaudRateItemSelectedCommand = new DelegateCommand<object>(BaudRateItemSelected);
            DataBitItemSelectedCommand = new DelegateCommand<object>(DataBitItemSelected);
            ParityItemSelectedCommand = new DelegateCommand<object>(ParityItemSelected);
            StopBitItemSelectedCommand = new DelegateCommand<object>(StopBitItemSelected);
            OpenSerialPortCommand = new DelegateCommand(OpenSerialPort);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            LoopUncheckedCommand = new DelegateCommand(LoopUnchecked);
            SendHexCheckedCommand = new DelegateCommand(SendHexChecked);
            SendHexUncheckedCommand = new DelegateCommand(SendHexUnchecked);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void InitDefaultConfig()
        {
            _clientCache = _dataService.LoadClientConfigCache(ConnectionType.SerialPort);
            ShowHex = _clientCache.ShowHex == 1;
            SendHex = _clientCache.SendHex == 1;

            //默认值
            _baudRate = _baudRateArray.First();
            _parity = _parityArray.First();
            _dataBits = _dataBitArray.First();
            _stopBits = _stopBitArray.First();

            _serialPortKit.DataReceivedEvent += delegate(byte[] bytes) { };

            _loopSendMessageTimer.Elapsed += TimerElapsedEvent_Handler;
        }

        private void PortItemSelected(string portName)
        {
            _portName = portName;
        }

        private void BaudRateItemSelected(object baudRate)
        {
            _baudRate = (int)baudRate;
        }

        private void DataBitItemSelected(object dataBits)
        {
            _dataBits = (int)dataBits;
        }

        private void ParityItemSelected(object parity)
        {
            _parity = (Parity)parity;
        }

        private void StopBitItemSelected(object stopBits)
        {
            _stopBits = (StopBits)stopBits;
        }

        private void OpenSerialPort()
        {
            if (string.IsNullOrEmpty(_portName))
            {
                MessageBox.Show("串口名称异常，无法打开", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _serialPortKit.Open(_portName, _baudRate, _parity, _dataBits, _stopBits);
        }

        private void AddExtensionCommand()
        {
            var dialogParameters = new DialogParameters
            {
                { "ParentId", _clientCache.Id },
                { "ConnectionType", ConnectionType.SerialPort }
            };
            _dialogService.ShowDialog("ExCommandDialog", dialogParameters, delegate { }, "ExCommandWindow");
        }

        private void ShowHexChecked()
        {
            var boxResult = MessageBox.Show(
                "切换到HEX显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            );
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为string
                    var hex = model.Content.StringToHex();
                    var msg = new MessageModel
                    {
                        Content = hex.Replace("-", " "),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;

                _clientCache.ShowHex = 1;
            }
        }

        private void ShowHexUnchecked()
        {
            var boxResult = MessageBox.Show("确定切换到字符串显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为Hex，先转bytes[]，再转string
                    var bytes = model.Content.HexToBytes();
                    var msg = new MessageModel
                    {
                        Content = bytes.ByteArrayToString(),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;

                _clientCache.ShowHex = 0;
            }
        }

        private void TimerElapsedEvent_Handler(object sender, ElapsedEventArgs e)
        {
            if (_buttonState.Equals("打开串口"))
            {
                Console.WriteLine(@"串口未打开");
                return;
            }

            var message = new MessageModel();
            if (_clientCache.SendHex == 1)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // _tcpClient.SendAsync(_userInputText);
            message.Content = _userInputText;
            message.Time = DateTime.Now.ToString("HH:mm:ss.fff");
            message.IsSend = true;
            Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(message); });
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void SendHexChecked()
        {
            _clientCache.SendHex = 1;
        }

        private void SendHexUnchecked()
        {
            _clientCache.SendHex = 0;
        }

        private void LoopUnchecked()
        {
            Console.WriteLine(@"取消循环发送指令");
            _loopSendMessageTimer.Enabled = false;
        }

        private void SendMessage()
        {
            _clientCache.Type = ConnectionType.SerialPort;
            _dataService.SaveCacheConfig(_clientCache);
        }
    }
}
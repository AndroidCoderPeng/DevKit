using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevKit.Cache;
using DevKit.Models;

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
        }


        public void OnDialogClosed()
        {
        }
    }
}
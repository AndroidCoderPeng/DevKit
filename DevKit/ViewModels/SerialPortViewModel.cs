using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

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
        [ObservableProperty] private List<string> _devicePortSource = [];
        [ObservableProperty] private string _selectedPort = string.Empty;

        [ObservableProperty] private List<string> _baudRateSource =
            ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600", "自定义"];

        [ObservableProperty] private string _selectedBaudRate = string.Empty;

        #endregion

        public SerialPortViewModel()
        {
            DevicePortSource = [.. SerialPort.GetPortNames()];
            if (DevicePortSource.Count != 0)
            {
                SelectedPort = DevicePortSource.First();
            }

            SelectedBaudRate = BaudRateSource[7]; // 默认选择 115200
        }


        public void OnDialogClosed()
        {
        }
    }
}
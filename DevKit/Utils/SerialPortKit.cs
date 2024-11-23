using System;
using System.IO.Ports;

namespace DevKit.Utils
{
    public class SerialPortKit
    {
        private readonly SerialPort _serialPort = new SerialPort();


        public event Action<byte[]> DataReceivedEvent;

        public bool IsOpen => _serialPort.IsOpen;

        public void Open(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.Parity = parity;
                _serialPort.DataBits = dataBits;
                _serialPort.StopBits = stopBits;
                _serialPort.Open();
                _serialPort.DataReceived += SerialPort_DataReceived;
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.DataReceived -= SerialPort_DataReceived;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
        }

        #region 写入数据

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (!_serialPort.IsOpen)
            {
                return;
            }

            _serialPort.Write(buffer, offset, count);
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer">写入端口的字节数组</param>
        public void Write(byte[] buffer)
        {
            if (!_serialPort.IsOpen)
            {
                return;
            }

            _serialPort.Write(buffer, 0, buffer.Length);
        }

        #endregion
    }
}
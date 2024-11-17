namespace DevKit.Utils
{
    public enum ConnectionType : ushort
    {
        TcpClient = 1,
        TcpServer = 2,
        UdpClient = 3,
        UdpServer = 4,
        WebSocketClient = 5,
        WebSocketServer = 6,
        SerialPort = 7
    }
}
namespace DevKit.Utils
{
    public interface ITcpClient
    {
        ConnectedEventHandler OnConnected { get; set; }
        DisconnectedEventHandler OnDisconnected { get; set; }
        DataReceivedEventHandler OnDataReceived { get; set; }
        ConnectFailedEventHandler OnConnectFailed { get; set; }
    }
}
using System.Windows.Controls;

namespace DevKit.Views
{
    public partial class TcpCommunicateView : UserControl
    {
        public TcpCommunicateView()
        {
            InitializeComponent();
            // var tcpClient = new TcpClient();
            // tcpClient.OnConnected += delegate(object sender, IChannelHandlerContext context)
            // {
            //     Console.WriteLine("Connected");
            // };
            // tcpClient.OnDisconnected += delegate(object sender, IChannelHandlerContext context)
            // {
            //     Console.WriteLine("Disconnected");
            // };
            // tcpClient.OnConnectFailed += delegate(object sender, Exception exception)
            // {
            //     Console.WriteLine("OnConnectFailed");
            // };
            // tcpClient.OnDataReceived += delegate(object sender, byte[] message)
            // {
            //     Console.WriteLine(BitConverter.ToString(message));
            // };
            // tcpClient.Start("192.168.161.208", 3000);
        }
    }
}
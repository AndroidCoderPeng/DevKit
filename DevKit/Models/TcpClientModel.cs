namespace DevKit.Models
{
    public class TcpClientModel
    {
        public string Id { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public int State { get; set; } // 1 - 在线，0 - 离线
    }
}
namespace DevKit.Models
{
    public class WsServerClient : TcpServerClient
    {
        /// <summary>
        /// 该连接握手时使用的 URL 路径，例如 /ws。
        /// </summary>
        public string WsPath { get; set; }
    }
}
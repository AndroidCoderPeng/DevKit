namespace DevKit.Models
{
    /// <summary>
    /// 客户端信息，避免 transport 依赖 VM 的 Model
    /// </summary>
    public sealed class ServerClient
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="path">仅 WebSocket 这类基于 URL 的传输有意义，TCP/UDP 下为 null。</param>
        public ServerClient(string id, string ip, int port, string path = null)
        {
            Id = id;
            Ip = ip;
            Port = port;
            Path = path;
        }

        public string Id { get; }
        public string Ip { get; }
        public int Port { get; }
        public string Path { get; }
    }
}
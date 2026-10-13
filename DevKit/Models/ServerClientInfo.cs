namespace DevKit.Models
{
    /// <summary>
    /// 客户端信息，避免 transport 依赖 VM 的 Model
    /// </summary>
    public sealed class ServerClientInfo
    {
        public ServerClientInfo(string id, string ip, int port)
        {
            Id = id;
            Ip = ip;
            Port = port;
        }

        public string Id { get; }
        public string Ip { get; }
        public int Port { get; }
    }
}
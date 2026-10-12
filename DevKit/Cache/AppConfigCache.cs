using System.Collections.Generic;

namespace DevKit.Cache
{
    // 分组名 + 文件名常量
    public static class ConfigSections
    {
        public const string FileName = "app-config.json";
        public const string Jni = "Jni";
        public const string Apk = "Apk";
        public const string Tcp = "Tcp";
        public const string Udp = "Udp";
        public const string WebSocket = "WebSocket";
        public const string History = "History";
    }

    // 安装包归档分组
    public class AppPackageConfig
    {
        public string JdkPath { get; set; }
        public string KeyPath { get; set; }
        public string Alias { get; set; }
        public string Password { get; set; }
        public string ApkRootFolder { get; set; }
    }

    // JNI逆向分组
    public class JniReverseConfig
    {
        public string NdkPath { get; set; }
        public string SharedLibPath { get; set; }
    }

    // TCP分组
    public class TcpConfig
    {
        public EndpointConfig Server { get; set; } = new EndpointConfig();
        public List<ExCommandConfig> ExCommands { get; set; } = new List<ExCommandConfig>();
        public List<EndpointConfig> Clients { get; set; } = new List<EndpointConfig>();
    }

    public class UdpConfig
    {
        public EndpointConfig Server { get; set; } = new EndpointConfig();
        public List<ExCommandConfig> ExCommands { get; set; } = new List<ExCommandConfig>();
        public List<EndpointConfig> Clients { get; set; } = new List<EndpointConfig>();
    }

    public class WebSocketConfig
    {
        public WsEndpointConfig Server { get; set; } = new WsEndpointConfig();
        public List<ExCommandConfig> ExCommands { get; set; } = new List<ExCommandConfig>();
        public List<WsEndpointConfig> Clients { get; set; } = new List<WsEndpointConfig>();
    }

    // TCP/UDP端点
    public class EndpointConfig
    {
        public string Ip { get; set; }
        public string Port { get; set; }
    }

    public class WsEndpointConfig : EndpointConfig
    {
        public string Path { get; set; }
    }

    // 扩展指令分组
    public class ExCommandConfig
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Memo { get; set; }
    }

    // 最近处理颜色分组
    public class RecentlyColorConfig
    {
        public List<string> Colors { get; set; } = new List<string>();
    }
}
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
        public TcpEndpointConfig Servers { get; set; } = new TcpEndpointConfig();

        public List<TcpEndpointConfig> Clients { get; set; } = new List<TcpEndpointConfig>();
    }

    // TCP端点
    public class TcpEndpointConfig
    {
        public string Ip { get; set; }
        public string Port { get; set; }
    }

    // 最近处理颜色分组
    public class RecentlyColorConfig
    {
        public List<string> Colors { get; set; } = new List<string>();
    }
}
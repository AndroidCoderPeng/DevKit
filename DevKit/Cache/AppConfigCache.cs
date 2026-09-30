using System.Collections.Generic;

namespace DevKit.Cache
{
    // 分组名 + 文件名常量
    public static class ConfigSections
    {
        public const string FileName = "app-config.json";
        public const string Jni = "jni";
        public const string Apk = "apk";
        public const string RecentlyColor = "color";
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

    // 最近处理颜色分组
    public class RecentlyColorConfig
    {
        public List<string> Colors { get; set; } = new List<string>();
    }
}
namespace DevKit.Cache
{
    public class AppConfigCache
    {
        public const string FileName = "app-config.json";
        
        public string JdkPath { get; set; }
        public string KeyPath { get; set; }
        public string Alias { get; set; }
        public string Password { get; set; }
        public string ApkRootFolder { get; set; }
        
        public string NdkPath { get; set; }
    }
}
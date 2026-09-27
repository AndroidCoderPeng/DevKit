namespace DevKit.Cache
{
    public class ApkConfigCache
    {
        public const string FileName = "apk-config.json";
        
        public string JdkPath { get; set; }
        public string KeyPath { get; set; }
        public string Alias { get; set; }
        public string Password { get; set; }
        public string ApkRootFolder { get; set; }
    }
}
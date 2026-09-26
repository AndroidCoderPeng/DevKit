namespace DevKit.Models
{
    /// <summary>
    /// 单条 Android logcat 日志（仅界面绑定所需字段，逻辑后续补充）
    /// </summary>
    public class LogcatModel
    {
        /// <summary>时间，如 14:21:08.123</summary>
        public string Time { get; set; }

        /// <summary>级别：V / D / I / W / E / F</summary>
        public string Level { get; set; }

        /// <summary>进程与线程，如 1234-5678</summary>
        public string Pid { get; set; }

        /// <summary>标签（Tag）</summary>
        public string Tag { get; set; }

        /// <summary>日志内容</summary>
        public string Message { get; set; }
    }
}
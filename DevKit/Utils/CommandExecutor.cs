using System.Diagnostics;
using System.Text;

namespace DevKit.Utils
{
    public class CommandExecutor
    {
        public delegate void CommandResultDelegate(string output);

        // 事件，用于在输出时触发  
        public event CommandResultDelegate OnStandardOutput;
        public event CommandResultDelegate OnErrorOutput;

        /// <summary>
        /// 命令行参数
        /// </summary>
        private readonly string _arguments;

        public CommandExecutor(string arguments)
        {
            _arguments = arguments;
        }

        public void Execute(string executor)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executor, // 如果不在环境变量中，需要改为完整路径  
                    Arguments = _arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8, // 确保输出编码正确  
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    OnStandardOutput?.Invoke(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    OnErrorOutput?.Invoke(e.Data);
                }
            };

            // 启动进程并开始异步读取输出  
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 等待进程完成  
            process.WaitForExit();
            process.Close();
        }
    }
}
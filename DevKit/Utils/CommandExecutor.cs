using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DevKit.Utils
{
    /// <summary>
    /// 命令执行器：负责启动进程、重定向并逐行转发标准输出与标准错误。
    /// </summary>
    public sealed class CommandExecutor
    {
        /// <summary>
        /// 收到一行标准输出时触发。
        /// </summary>
        public event Action<string> OnStandardOutput;

        /// <summary>
        /// 收到一行标准错误时触发。
        /// </summary>
        public event Action<string> OnStandardError;

        private readonly string _arguments;

        public CommandExecutor(string arguments)
        {
            _arguments = arguments ?? string.Empty;
        }

        /// <summary>
        /// 同步执行命令并阻塞直到退出，返回进程退出码（0 通常表示成功）。
        /// </summary>
        public int Execute(string executor)
        {
            using (var process = Start(executor))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        /// <summary>
        /// 在后台线程执行命令，返回一个在进程退出时完成的 Task（含退出码）。
        /// </summary>
        public Task<int> ExecuteAsync(string fileName)
        {
            return Task.Run(() => Execute(fileName));
        }

        /// <summary>
        /// 非阻塞启动进程，返回 Process 供调用方自行轮询进度。
        /// 调用方需在结束后自行释放（Dispose / Close）。
        /// </summary>
        public Process StartNonBlocking(string executor)
        {
            return Start(executor);
        }

        private Process Start(string executor)
        {
            if (string.IsNullOrWhiteSpace(executor))
            {
                throw new ArgumentException(@"可执行文件路径不能为空。", nameof(executor));
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executor,
                    Arguments = _arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
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
                    OnStandardError?.Invoke(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
    }
}
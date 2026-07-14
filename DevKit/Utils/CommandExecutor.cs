using System.Diagnostics;

namespace DevKit.Utils
{
    public class CommandExecutor
    {
        public delegate void CommandResultDelegate(string output);

        public event CommandResultDelegate OnStandardOutput;
        public event CommandResultDelegate OnStandardError;

        private readonly string _arguments;

        public CommandExecutor(string arguments)
        {
            _arguments = arguments;
        }

        /// <summary>
        /// 同步执行命令，阻塞直到进程退出
        /// </summary>
        public void Execute(string executor)
        {
            var process = BuildAndStartProcess(executor);
            process.WaitForExit();
            process.Close();
        }

        /// <summary>
        /// 非阻塞启动进程，返回 Process 对象供调用方轮询状态
        /// </summary>
        public Process StartNonBlocking(string executor)
        {
            return BuildAndStartProcess(executor);
        }

        private Process BuildAndStartProcess(string executor)
        {
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
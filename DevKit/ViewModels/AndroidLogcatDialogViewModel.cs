using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class AndroidLogcatDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "Logcat";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        #region DelegateCommand

        public DelegateCommand ClearCommand { get; }
        public DelegateCommand PauseCommand { get; }
        public DelegateCommand<string> ToggleLevelCommand { get; }

        #endregion

        #region VM

        private ObservableCollection<LogcatModel> _logs = new ObservableCollection<LogcatModel>();

        public ObservableCollection<LogcatModel> Logs
        {
            get => _logs;
            set
            {
                _logs = value;
                RaisePropertyChanged();
            }
        }

        private string _keyword = string.Empty;

        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoScroll = true;

        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                _autoScroll = value;
                RaisePropertyChanged();
            }
        }

        public ICollectionView LogsView => _logsView;

        #endregion

        private string _device;
        private const int MaxLogCount = 1000;

        private static readonly Regex LogcatLineRegex = new Regex(
            @"^(?:\d{4}-)?\d{2}-\d{2}\s+(\d{2}:\d{2}:\d{2}\.\d{3})\s+(\d+)\s+(\d+)\s+([VDIWEF])\s+(\S+?):\s*(.*)$",
            RegexOptions.Compiled);

        private Process _logcatProcess;
        private bool _isPaused;
        private readonly HashSet<string> _enabledLevels = new HashSet<string> { "D", "I", "W", "E" };
        private ICollectionView _logsView;

        public AndroidLogcatDialogViewModel()
        {
            _logsView = CollectionViewSource.GetDefaultView(Logs);
            _logsView.Filter = o => o is LogcatModel m && _enabledLevels.Contains(m.Level);

            ClearCommand = new DelegateCommand(() => { Logs.Clear(); });

            PauseCommand = new DelegateCommand(() => { _isPaused = !_isPaused; });

            ToggleLevelCommand = new DelegateCommand<string>(level =>
            {
                if (string.IsNullOrEmpty(level)) return;

                if (!_enabledLevels.Add(level))
                {
                    _enabledLevels.Remove(level);
                }

                _logsView.Refresh();
            });
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            StopLogcatCapture();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            _device = parameters.GetValue<string>("device");

            // 启动 adb logcat 抓取（Debug 及以上级别）
            StartLogcatCapture();
        }

        private void StartLogcatCapture()
        {
            if (string.IsNullOrEmpty(_device)) return;

            // adb -s <device> logcat -v threadtime *:D
            var argument = new ArgumentCreator();
            var cmdStr = argument
                .Append("-s").Append(_device)
                .Append("logcat")
                .Append("-v").Append("threadtime")
                .Append("*:V")
                .ToCommandLine();

            var executor = new CommandExecutor(cmdStr);
            executor.OnStandardOutput += ParseLogcatLine;
            _logcatProcess = executor.StartNonBlocking("adb");
        }

        private void ParseLogcatLine(string line)
        {
            if (_isPaused) return;
            if (string.IsNullOrEmpty(line)) return;
            
            if (string.IsNullOrEmpty(line)) return;

            var match = LogcatLineRegex.Match(line);
            if (!match.Success) return;

            var model = new LogcatModel
            {
                Time = match.Groups[1].Value,
                Pid = $"{match.Groups[2].Value}-{match.Groups[3].Value}",
                Level = match.Groups[4].Value,
                Tag = match.Groups[5].Value,
                Message = match.Groups[6].Value
            };

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Add(model);
                while (Logs.Count > MaxLogCount)
                {
                    Logs.RemoveAt(0);
                }
            }));
        }

        private void StopLogcatCapture()
        {
            if (_logcatProcess == null) return;

            try
            {
                if (!_logcatProcess.HasExited)
                {
                    _logcatProcess.Kill();
                }
            }
            catch
            {
                // 关闭弹窗时忽略异常
            }
            finally
            {
                _logcatProcess.Dispose();
                _logcatProcess = null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Models;
using DevKit.Utils;

namespace DevKit.ViewModels
{
    public partial class AndroidLogcatDialogViewModel : ObservableObject, IDialogParameterReceiver
    {
        #region VM

        [ObservableProperty] private ObservableCollection<AndroidLog> _logs = [];
        [ObservableProperty] private string _keyword = string.Empty;
        [ObservableProperty] private bool _autoScroll = true;

        public ICollectionView LogsView { get; }

        partial void OnKeywordChanged(string value)
        {
            LogsView.Refresh();
        }

        #endregion

        private const int MaxLogCount = 1000;

        private static readonly Regex LogcatLineRegex = new(
            @"^(?:\d{4}-)?\d{2}-\d{2}\s+(\d{2}:\d{2}:\d{2}\.\d{3})\s+(\d+)\s+(\d+)\s+([VDIWEF])\s+(\S+?):\s*(.*)$",
            RegexOptions.Compiled);

        private Process _logcatProcess;
        private bool _isPaused;
        private readonly HashSet<string> _enabledLevels = ["D"];

        public AndroidLogcatDialogViewModel()
        {
            LogsView = CollectionViewSource.GetDefaultView(Logs);
            LogsView.Filter = o => o is AndroidLog log
                                   && _enabledLevels.Contains(log.Level)
                                   && (string.IsNullOrEmpty(_keyword)
                                       || log.Tag.Contains(_keyword, StringComparison.OrdinalIgnoreCase)
                                       || log.Message.Contains(_keyword, StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand]
        private void Clear()
        {
            Logs.Clear();
        }

        [RelayCommand]
        private void Pause()
        {
            _isPaused = !_isPaused;
        }

        [RelayCommand]
        private void ToggleLevel(string level)
        {
            if (string.IsNullOrEmpty(level))
            {
                return;
            }

            if (!_enabledLevels.Add(level))
            {
                _enabledLevels.Remove(level);
            }

            LogsView.Refresh();
        }

        public void OnDialogOpened(object parameter)
        {
            if (parameter is string device)
            {
                // 启动 adb logcat 抓取（Debug 及以上级别）
                StartLogcatCapture(device);
            }
        }

        // public void OnDialogClosed()
        // {
        //     StopLogcatCapture();
        // }

        private void StartLogcatCapture(string device)
        {
            if (string.IsNullOrEmpty(device)) return;

            // adb -s <device> logcat -v threadtime *:D
            var argument = new ArgumentCreator();
            var cmdStr = argument
                .Append("-s").Append(device)
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

            var log = new AndroidLog
            {
                Time = match.Groups[1].Value,
                Pid = $"{match.Groups[2].Value}-{match.Groups[3].Value}",
                Level = match.Groups[4].Value,
                Tag = match.Groups[5].Value,
                Message = match.Groups[6].Value
            };

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Add(log);
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
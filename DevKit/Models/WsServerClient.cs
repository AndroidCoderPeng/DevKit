using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.Models
{
    public partial class WsServerClient : ObservableObject
    {
        public WsServerClient()
        {
            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        public required string Id { get; init; }
        public required string Ip { get; init; }
        public int Port { get; init; }

        /// <summary>
        /// 该连接握手时使用的 URL 路径，例如 /ws。
        /// </summary>
        [ObservableProperty] private string _wsPath;

        [ObservableProperty] private bool _isConnected;

        [ObservableProperty] private ObservableCollection<SocketFrameMessage> _logs = [];

        private readonly HashSet<SocketFrameMessage> _subscribedLogs = [];

        partial void OnLogsChanging(
            ObservableCollection<SocketFrameMessage> oldValue,
            ObservableCollection<SocketFrameMessage> newValue)
        {
            _ = newValue;
            oldValue.CollectionChanged -= Logs_CollectionChanged;
            UnsubscribeFromLogs();
        }

        partial void OnLogsChanged(
            ObservableCollection<SocketFrameMessage> oldValue,
            ObservableCollection<SocketFrameMessage> newValue)
        {
            _ = oldValue;
            newValue.CollectionChanged += Logs_CollectionChanged;
            SubscribeToLogs(newValue);
            OnPropertyChanged(nameof(UnreadLogCount));
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                UnsubscribeFromLogs();
            }
            else
            {
                UnsubscribeFromChangedLogs(e.OldItems);
                SubscribeToChangedLogs(e.NewItems);
            }

            // 增删消息后，未读数要跟着刷新（XAML 里的红色角标）
            OnPropertyChanged(nameof(UnreadLogCount));
        }

        private void SubscribeToLogs(IEnumerable<SocketFrameMessage> logs)
        {
            foreach (var log in logs)
            {
                if (_subscribedLogs.Add(log))
                {
                    log.PropertyChanged += Log_PropertyChanged;
                }
            }
        }

        private void SubscribeToChangedLogs(IList logs)
        {
            foreach (SocketFrameMessage log in logs)
            {
                SubscribeToLogs([log]);
            }
        }

        private void UnsubscribeFromLogs()
        {
            foreach (var log in _subscribedLogs)
            {
                log.PropertyChanged -= Log_PropertyChanged;
            }

            _subscribedLogs.Clear();
        }

        private void UnsubscribeFromChangedLogs(IList logs)
        {
            foreach (SocketFrameMessage log in logs)
            {
                if (_subscribedLogs.Remove(log))
                {
                    log.PropertyChanged -= Log_PropertyChanged;
                }
            }
        }

        private void Log_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SocketMessage.IsRead) ||
                e.PropertyName == nameof(SocketMessage.IsSend))
            {
                OnPropertyChanged(nameof(UnreadLogCount));
            }
        }

        [ObservableProperty] private int _messageCount;

        public int UnreadLogCount
        {
            get
            {
                var count = 0;
                foreach (var log in Logs)
                {
                    // 只统计“接收到的、且未读”的消息
                    if (log.IsSend == 0 && !log.IsRead)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 把该客户端所有消息标记为已读，返回本次清零的条数
        /// </summary>
        public void MarkAllRead()
        {
            foreach (var log in Logs)
            {
                if (log.IsRead) continue;
                log.IsRead = true;
            }
        }
    }
}
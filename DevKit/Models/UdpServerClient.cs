using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.Models
{
    /// <summary>
    /// UDP 服务端的「来源端点」。因 UDP 无连接，用活跃/静默状态表达会话。
    /// </summary>
    public partial class UdpServerClient : ObservableObject
    {
        public UdpServerClient()
        {
            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        public required string Id { get; init; }
        public required string Ip { get; init; }
        public int Port { get; init; }

        /// <summary>
        /// 最后一次收到数据的时间，供 VM 的空闲扫描计算
        /// </summary>
        public DateTime LastReceivedAt { get; set; }

        [ObservableProperty] private bool _isActive;

        // 静默 · N 分钟前
        [ObservableProperty] private int _lastActiveTime;

        // 活跃 · N 秒前
        [ObservableProperty] private int _lastMessageTime;

        [ObservableProperty] private ObservableCollection<SocketMessage> _logs = [];

        private readonly HashSet<SocketMessage> _subscribedLogs = [];

        partial void OnLogsChanging(
            ObservableCollection<SocketMessage> oldValue,
            ObservableCollection<SocketMessage> newValue)
        {
            _ = newValue;
            oldValue.CollectionChanged -= Logs_CollectionChanged;
            UnsubscribeFromLogs();
        }

        partial void OnLogsChanged(
            ObservableCollection<SocketMessage> oldValue,
            ObservableCollection<SocketMessage> newValue)
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

        private void SubscribeToLogs(IEnumerable<SocketMessage> logs)
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
            foreach (SocketMessage log in logs)
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
            foreach (SocketMessage log in logs)
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
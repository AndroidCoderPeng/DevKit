using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.Models
{
    public partial class TcpServerClient : ObservableObject
    {
        /// <summary>
        /// 生命周期内不变，用 init 属性
        /// </summary>
        public required string Id { get; init; }

        public required string Ip { get; init; }
        public required int Port { get; init; }

        [ObservableProperty] private bool _isConnected;

        [ObservableProperty] private ObservableCollection<SocketMessage> _logs = [];

        private readonly HashSet<SocketMessage> _subscribedLogs = [];

        public TcpServerClient()
        {
            _logs.CollectionChanged += Logs_CollectionChanged;
        }

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
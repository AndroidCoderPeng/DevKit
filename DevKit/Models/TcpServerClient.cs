using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DevKit.Models
{
    public class TcpServerClient : INotifyPropertyChanged
    {
        public TcpServerClient()
        {
            _logs.CollectionChanged += Logs_CollectionChanged;
        }

        public string Id { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private ObservableCollection<SocketMessage> _logs = new ObservableCollection<SocketMessage>();

        public ObservableCollection<SocketMessage> Logs
        {
            get => _logs;
            set
            {
                if (ReferenceEquals(_logs, value))
                {
                    return;
                }

                _logs.CollectionChanged -= Logs_CollectionChanged;
                _logs = value;
                _logs.CollectionChanged += Logs_CollectionChanged;

                OnPropertyChanged(nameof(Logs));
                OnPropertyChanged(nameof(UnreadLogCount));
            }
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 增删消息后，未读数要跟着刷新（XAML 里的红色角标）
            OnPropertyChanged(nameof(UnreadLogCount));
        }

        private int _messageCount;

        public int MessageCount
        {
            get => _messageCount;
            set
            {
                _messageCount = value;
                OnPropertyChanged(nameof(MessageCount));
            }
        }

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
        public int MarkAllRead()
        {
            var changed = 0;
            foreach (var log in Logs)
            {
                if (!log.IsRead)
                {
                    log.IsRead = true;
                    changed++;
                }
            }

            if (changed > 0)
            {
                OnPropertyChanged(nameof(UnreadLogCount));
            }

            return changed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;

namespace DevKit.Models
{
    public class UdpClientModel : INotifyPropertyChanged
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public EndPoint TargetEndPoint { get; set; }

        private ObservableCollection<LogModel> _logs = new ObservableCollection<LogModel>();

        public ObservableCollection<LogModel> Logs
        {
            get => _logs;
            set
            {
                _logs = value;
                OnPropertyChanged(nameof(Logs));
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
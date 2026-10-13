using System.Collections.ObjectModel;
using System.ComponentModel;
using TouchSocket.Http.WebSockets;

namespace DevKit.Models
{
    public class WebSocketClientModel : INotifyPropertyChanged
    {
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

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private IWebSocket _connectedWebSocket;

        public IWebSocket WebSocket
        {
            get => _connectedWebSocket;
            set
            {
                _connectedWebSocket = value;
                OnPropertyChanged(nameof(WebSocket));
            }
        }
    }
}
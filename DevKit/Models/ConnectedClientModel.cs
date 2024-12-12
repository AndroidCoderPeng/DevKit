using System.Collections.ObjectModel;
using System.ComponentModel;
using TouchSocket.Http.WebSockets;

namespace DevKit.Models
{
    public class ConnectedClientModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

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

        private ObservableCollection<byte[]> _messageCollection = new ObservableCollection<byte[]>();

        public ObservableCollection<byte[]> MessageCollection
        {
            set
            {
                _messageCollection = value;
                OnPropertyChanged(nameof(MessageCollection));
            }
            get => _messageCollection;
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
using TouchSocket.Http.WebSockets;

namespace DevKit.Models
{
    public class WebSocketClientModel : SocketClientModel
    {
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
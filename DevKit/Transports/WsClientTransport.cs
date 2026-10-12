using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;

namespace DevKit.Transports
{
    public class WsClientTransport : ITransport
    {
        public event Action<byte[]> DataReceived;
        
        public void Dispose()
        {
            
        }

        public void SetRemoteEndPoint(string address, int port, string path = null, bool secure = false)
        {
            
        }

        public Task SendAsync(byte[] payload, CancellationToken token)
        {
            return Task.CompletedTask;
        }
        
        public event Action<TransportState> StateChanged;
        
        public Task ConnectAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
        
        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }
    }
}
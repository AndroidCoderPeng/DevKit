using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;

namespace DevKit.Transports
{
    public class TcpClientTransport: ITransport
    {
        public void Dispose()
        {
            
        }

        public Task ConnectAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] payload, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public event Action<byte[]> DataReceived;
        
        public event Action<TransportState> StateChanged;
        
        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }
    }
}
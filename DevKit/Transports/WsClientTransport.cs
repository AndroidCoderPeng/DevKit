using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DevKit.Transports
{
    public class WsClientTransport : ITransport
    {
        private const int MaxMessageBytes = 4 * 1024 * 1024;

        public event Action<byte[]> DataReceived;

        public WsClientTransport(string remoteAddress, int remotePort, string remotePath = null)
        {
            SetRemoteEndPoint(remoteAddress, remotePort, remotePath);
        }

        public void Dispose()
        {
        }

        public void SetRemoteEndPoint(string address, int port, string path = null)
        {
        }

        public async Task SendAsync(byte[] payload, CancellationToken token)
        {
        }

        public async Task ConnectAsync(CancellationToken token)
        {
        }

        public async Task DisconnectAsync()
        {
        }

        private async Task ReceiveLoopAsync(ClientWebSocket client, CancellationTokenSource sessionCts,
            CancellationTokenSource receiveCts)
        {
            
        }
    }
}
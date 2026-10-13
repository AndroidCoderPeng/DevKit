using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Models;

namespace DevKit.Transports
{
    public interface ITcpServerTransport
    {
        event Action<TcpClientInfo> ClientConnected;
        event Action<TcpClientInfo> ClientClosed;
        event Action<TcpClientInfo, byte[]> DataReceived;

        bool IsRunning { get; }

        Task StartAsync(string address, int port, CancellationToken token);
        Task StopAsync();
        Task SendAsync(string clientId, byte[] payload);
        Task CloseClientAsync(string clientId);
    }
}
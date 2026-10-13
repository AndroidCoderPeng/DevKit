using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Models;

namespace DevKit.Transports
{
    public interface IServerTransport : IDisposable
    {
        event Action<ServerClient, byte[]> DataReceived;

        bool IsRunning { get; }

        Task StartAsync(string address, int port, bool reuseAddress, CancellationToken token);
        Task StopAsync();
        Task SendAsync(string clientId, byte[] payload);
        Task DisconnectAll();
        Task DisconnectClient(string clientId);
    }
}
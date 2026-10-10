using System;
using System.Threading;
using System.Threading.Tasks;
using DevKit.Utils;

namespace DevKit.Transports
{
    public interface ITransport: IDisposable
    {
        Task ConnectAsync(CancellationToken token);
        
        Task SendAsync(byte[] payload, CancellationToken token);
        
        event Action<byte[]> DataReceived; 
        
        event Action<TransportState> StateChanged;
        
        Task DisconnectAsync();
    }
}
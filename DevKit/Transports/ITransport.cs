using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevKit.Transports
{
    public interface ITransport: IDisposable
    {
        event Action<byte[]> DataReceived; 
        
        void SetRemoteEndPoint(string ip, int port);
        
        Task SendAsync(byte[] payload, CancellationToken token);
    }
}
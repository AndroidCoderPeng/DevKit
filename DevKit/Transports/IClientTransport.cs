using System;
using System.Threading.Tasks;

namespace DevKit.Transports
{
    public interface IClientTransport : IDisposable
    {
        /// <summary>
        /// Gets the current state of the transport.
        /// </summary>
        event Action<byte[]> DataReceived;

        /// <summary>
        /// Sets the remote endpoint for the transport.
        /// path 仅对 WebSocket 这类基于 URL 的传输有意义
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="path"></param>
        void SetRemoteEndPoint(string address, int port, string path = null);

        /// <summary>
        /// Sends data to the remote endpoint.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task SendAsync(byte[] payload);
    }
}
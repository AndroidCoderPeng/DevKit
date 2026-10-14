namespace DevKit.Models
{
    /// <summary>
    /// WebSocket frame message model.
    /// </summary>
    public class SocketFrameMessage : SocketMessage
    {
        public string FrameType { get; set; }
    }
}
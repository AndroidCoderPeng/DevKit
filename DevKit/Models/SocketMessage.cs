using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.Models
{
    public partial class SocketMessage : ObservableObject
    {
        public byte[] RawData { get; set; }

        [ObservableProperty] private string _content;

        public string Time { get; set; }

        /// <summary>
        /// Indicates whether the message was sent or received. 1 for sent messages, 0 for received messages.
        /// </summary>
        public int IsSend { get; set; }

        [ObservableProperty] private bool _isRead;
    }
}
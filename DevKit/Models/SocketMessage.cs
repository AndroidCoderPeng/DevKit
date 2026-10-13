using System.ComponentModel;

namespace DevKit.Models
{
    public class SocketMessage : INotifyPropertyChanged
    {
        public byte[] RawData { get; set; }

        private string _content;

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public string Time { get; set; }

        /// <summary>
        /// Indicates whether the message was sent or received. 1 for sent messages, 0 for received messages.
        /// </summary>
        public int IsSend { get; set; }

        /// <summary>
        /// Indicates whether the message has been read. True if the message has been read; otherwise, false.
        /// </summary>
        public bool IsRead { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
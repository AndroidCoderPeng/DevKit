using System.ComponentModel;
using SQLite;

namespace DevKit.Cache
{
    [Table("LogCache")]
    public class LogCache : INotifyPropertyChanged
    {
        [PrimaryKey, Unique, NotNull, AutoIncrement]
        public int Id { get; set; }

        public string ClientType { get; set; }
        public string HostAddress { get; set; }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Time { get; set; }
        public int IsSend { get; set; }
    }
}
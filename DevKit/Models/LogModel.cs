using System.ComponentModel;

namespace DevKit.Models
{
    public class LogModel : INotifyPropertyChanged
    {
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
        public bool IsSend { get; set; }
    }
}
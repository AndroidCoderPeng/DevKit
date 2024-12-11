using System.Windows;
using System.Windows.Controls;
using DevKit.Models;

namespace DevKit.Views
{
    public partial class TcpServerView : UserControl
    {
        public TcpServerView()
        {
            InitializeComponent();
        }

        private void MessageItem_RightClick(object sender, RoutedEventArgs e)
        {
            if (!(MessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }
    }
}
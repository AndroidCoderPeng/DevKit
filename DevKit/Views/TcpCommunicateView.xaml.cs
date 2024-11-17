using System.Windows;
using System.Windows.Controls;
using DevKit.Models;

namespace DevKit.Views
{
    public partial class TcpCommunicateView : UserControl
    {
        public TcpCommunicateView()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(ClientMessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ModifyMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
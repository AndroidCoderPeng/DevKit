using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Models;
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class TcpServerView : UserControl
    {
        public TcpServerView()
        {
            InitializeComponent();
        }

        private void ClientListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is ConnectedClientModel model)
            {
                var vm = DataContext as TcpServerViewModel;
                vm?.ClientItemClickedCommand.Execute(model);
            }
        }
        
        private void MessageItem_RightClick(object sender, RoutedEventArgs e)
        {
            if (!(MessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }
    }
}
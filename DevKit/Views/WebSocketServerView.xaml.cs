using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Models;
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class WebSocketServerView : UserControl
    {
        public WebSocketServerView()
        {
            InitializeComponent();
        }

        private void ClientListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is UdpClientModel model)
            {
                var vm = DataContext as WebSocketServerViewModel;
                // vm?.ClientItemClickedCommand.Execute(model);
            }
        }
    }
}
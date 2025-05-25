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
            if (sender is ListBoxItem item && item.Content is SocketClientModel model)
            {
                var vm = DataContext as TcpServerViewModel;
                vm?.ClientItemClickedCommand.Execute(model);
            }
        }
    }
}
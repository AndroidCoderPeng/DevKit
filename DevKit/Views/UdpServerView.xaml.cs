using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Models;
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class UdpServerView : UserControl
    {
        public UdpServerView()
        {
            InitializeComponent();
        }

        private void ClientListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is UdpClientModel model)
            {
                var vm = DataContext as UdpServerViewModel;
                vm?.ClientItemClickedCommand.Execute(model);
            }
        }
    }
}
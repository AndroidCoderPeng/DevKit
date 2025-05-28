using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Cache;
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class ColorResourceView : UserControl
    {
        public ColorResourceView()
        {
            InitializeComponent();
        }

        private void ColorListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is ColorResourceCache cache)
            {
                var vm = DataContext as ColorResourceViewModel;
                vm?.ColorItemClickedCommand.Execute(cache);
            }
        }
    }
}
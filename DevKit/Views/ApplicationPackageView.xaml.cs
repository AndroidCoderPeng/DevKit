using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Models;
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class ApplicationPackageView : UserControl
    {
        public ApplicationPackageView()
        {
            InitializeComponent();
        }

        private void ListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is ApkFileModel model)
            {
                var vm = DataContext as ApplicationPackageViewModel;
                vm?.OpenFileFolderCommand.Execute(model.FullName);
            }
        }
    }
}
using System;
using System.Windows.Controls;
using System.Windows.Input;
using DevKit.ViewModels;
using Newtonsoft.Json;

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
            if (sender is ListBoxItem item && item.Content is ColorResourceViewModel model)
            {
                var vm = DataContext as ColorResourceViewModel;
                // vm?.ClientItemClickedCommand.Execute(model);
                Console.WriteLine(JsonConvert.SerializeObject(model));
            }
        }
    }
}
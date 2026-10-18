using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class NetConfigurationView
    {
        public NetConfigurationView()
        {
            InitializeComponent();
            OutputTextBox.TextChanged += (sender, e) =>
            {
                if (DataContext is NetConfigurationViewModel vm && vm.IsAutoScrollBoxChecked)
                {
                    OutputTextBox.ScrollToEnd();
                }
            };
        }
    }
}
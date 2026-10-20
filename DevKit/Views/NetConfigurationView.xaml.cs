using System.Runtime.Versioning;
using DevKit.ViewModels;

namespace DevKit.Views
{
    [SupportedOSPlatform("windows6.1")]
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
using System.Runtime.Versioning;
using DevKit.ViewModels;

namespace DevKit.Views
{
    [SupportedOSPlatform("windows6.1")]
    public partial class JniReverseView
    {
        public JniReverseView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is JniReverseViewModel viewModel)
                {
                    viewModel.SaveConfiguration();
                }
            };
        }
    }
}
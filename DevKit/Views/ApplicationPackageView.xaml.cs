using System.Runtime.Versioning;
using DevKit.ViewModels;

namespace DevKit.Views
{
    [SupportedOSPlatform("windows6.1")]
    public partial class ApplicationPackageView
    {
        public ApplicationPackageView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is ApplicationPackageViewModel viewModel)
                {
                    viewModel.SaveConfiguration();
                }
            };
        }
    }
}
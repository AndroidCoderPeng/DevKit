using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class ColorResourceView
    {
        public ColorResourceView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is ColorResourceViewModel viewModel)
                {
                    viewModel.SaveConfiguration();
                }
            };
        }
    }
}
using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class UdpServerView
    {
        public UdpServerView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is UdpServerViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
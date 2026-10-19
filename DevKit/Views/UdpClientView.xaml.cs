using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class UdpClientView
    {
        public UdpClientView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is UdpClientViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
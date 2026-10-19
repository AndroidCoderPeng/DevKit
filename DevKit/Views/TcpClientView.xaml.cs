using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class TcpClientView
    {
        public TcpClientView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is TcpClientViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
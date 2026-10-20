using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class TcpServerView
    {
        public TcpServerView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is TcpServerViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
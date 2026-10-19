using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class WsClientView
    {
        public WsClientView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is WsClientViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
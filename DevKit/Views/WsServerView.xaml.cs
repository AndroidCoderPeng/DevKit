using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class WsServerView
    {
        public WsServerView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is WsServerViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
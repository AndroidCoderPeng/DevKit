using DevKit.ViewModels;

namespace DevKit.Views
{
    public partial class SerialPortView
    {
        public SerialPortView()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is SerialPortViewModel viewModel)
                {
                    viewModel.OnDialogClosed();
                }
            };
        }
    }
}
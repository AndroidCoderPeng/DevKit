using DevKit.ViewModels;

namespace DevKit.Dialogs
{
    public partial class AndroidLogcatDialog
    {
        public AndroidLogcatDialog()
        {
            InitializeComponent();
            Closed += delegate
            {
                if (DataContext is AndroidLogcatDialogViewModel viewModel)
                {
                    viewModel.StopLogcatCapture();
                }
            };
        }
    }
}
using System.Windows;
using DevKit.Views;
using Prism.Commands;

namespace DevKit.ViewModels
{
    public class TrayIconViewModel
    {
        public DelegateCommand ShowWindowCommand { set; get; }
        public DelegateCommand ExitApplicationCommand { set; get; }

        public TrayIconViewModel(MainWindow mainWindow)
        {
            ShowWindowCommand = new DelegateCommand(delegate { ShowWindow(mainWindow); });
            ExitApplicationCommand = new DelegateCommand(ExitApplication);
        }

        private void ShowWindow(MainWindow mainWindow)
        {
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.ShowInTaskbar = true;
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
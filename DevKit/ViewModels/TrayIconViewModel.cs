using System.ComponentModel;
using System.Windows;
using DevKit.Views;
using Prism.Commands;

namespace DevKit.ViewModels
{
    public class TrayIconViewModel
    {
        public DelegateCommand ShowWindowCommand { set; get; }
        public DelegateCommand ExitApplicationCommand { set; get; }

        private bool _isClickTrayIconExit;

        public TrayIconViewModel(MainWindow mainWindow)
        {
            mainWindow.Closing += delegate(object sender, CancelEventArgs e)
            {
                if (!_isClickTrayIconExit)
                {
                    var result = MessageBox.Show(
                        "确定要退出吗？", "退出确认", MessageBoxButton.OKCancel, MessageBoxImage.Question
                    );
                    if (result == MessageBoxResult.OK)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        e.Cancel = true;
                        //最小化且不显示在任务栏
                        mainWindow.WindowState = WindowState.Minimized;
                        mainWindow.ShowInTaskbar = false;
                    }
                }
            };

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
            _isClickTrayIconExit = true;
            Application.Current.Shutdown();
        }
    }
}
using System.ComponentModel;
using System.Windows;

namespace DevKit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += delegate(object sender, CancelEventArgs e)
            {
                {
                    var result = MessageBox.Show(
                        "确定要退出吗？", "退出确认", MessageBoxButton.OKCancel, MessageBoxImage.Question
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        e.Cancel = true;
                        //最小化且不显示在任务栏
                        WindowState = WindowState.Minimized;
                        ShowInTaskbar = false;
                    }
                }
            };
        }
    }
}
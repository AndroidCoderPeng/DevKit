using System.Timers;
using DevKit.Utils;

namespace DevKit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Timer _updateWindowLocationTimer = new Timer(1000);

        public MainWindow()
        {
            InitializeComponent();
            _updateWindowLocationTimer.Elapsed += delegate
            {
                Dispatcher.Invoke(delegate
                {
                    RuntimeCache.X = Left + Width;
                    RuntimeCache.Y = Top;
                });
            };
            Loaded += delegate { _updateWindowLocationTimer.Enabled = true; };
        }
    }
}
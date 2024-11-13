using System.Collections.Generic;

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

            var elements = new List<string>
            {
                "ADB", "APK", "TCP", "UDP", "WebSocket", "串口通信", "图标", "微软翻译", "转码", "颜色"
            };
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using DevKit.DataService;
using DevKit.Models;

namespace DevKit.Views
{
    public partial class TcpCommunicateView : UserControl
    {
        public TcpCommunicateView(IAppDataService dataService)
        {
            InitializeComponent();
            var clientCache = dataService.LoadTcpClientConfigCache();
            ShowHexCheckBox.Checked += delegate
            {
                clientCache.ShowHex = 1;
                dataService.SaveCacheConfig(clientCache);
            };
            ShowHexCheckBox.Unchecked += delegate
            {
                clientCache.ShowHex = 0;
                dataService.SaveCacheConfig(clientCache);
            };

            SendHexCheckBox.Checked += delegate
            {
                clientCache.SendHex = 1;
                dataService.SaveCacheConfig(clientCache);
            };
            SendHexCheckBox.Unchecked += delegate
            {
                clientCache.SendHex = 0;
                dataService.SaveCacheConfig(clientCache);
            };
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(ClientMessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }
    }
}
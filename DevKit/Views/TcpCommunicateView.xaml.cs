using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using DevKit.Utils;

namespace DevKit.Views
{
    public partial class TcpCommunicateView : UserControl
    {
        private readonly IAppDataService _dataService;

        public TcpCommunicateView(IAppDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(ClientMessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }

        private void RightDrawer_OnOpened(object sender, RoutedEventArgs e)
        {
            //加载已经预设的指令
            var commandCache = _dataService.LoadCommandExtensionCaches(1, ConnectionType.TcpClient);
            CommandListBox.ItemsSource = commandCache.ToObservableCollection();
        }

        private void SaveExtensionCommandButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserCommandTextBox.Text))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var annotation = string.IsNullOrWhiteSpace(CommandAnnotationTextBox.Text)
                ? $"指令{CommandListBox.Items.Count + 1}"
                : CommandAnnotationTextBox.Text;
            var cache = new CommandExtensionCache
            {
                ParentId = 1,
                ParentType = ConnectionType.TcpClient,
                Command = UserCommandTextBox.Text,
                Annotation = annotation
            };

            if (IsHexCheckBox.IsChecked == true)
            {
                //检查是否是正确的Hex指令
                if (!UserCommandTextBox.Text.IsHex())
                {
                    MessageBox.Show("预设的指令不是正确的Hex", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                cache.IsHex = 1;
            }
            else
            {
                cache.IsHex = 0;
            }

            _dataService.SaveCacheConfig(cache);
            //更新列表
            var commandCache = _dataService.LoadCommandExtensionCaches(1, ConnectionType.TcpClient);
            CommandListBox.ItemsSource = commandCache.ToObservableCollection();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ModifyMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
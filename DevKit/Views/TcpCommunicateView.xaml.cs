using System;
using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using Prism.Events;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.Views
{
    public partial class TcpCommunicateView : UserControl
    {
        private readonly IAppDataService _dataService;
        private readonly IEventAggregator _eventAggregator;

        public TcpCommunicateView(IAppDataService dataService, IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _dataService = dataService;
            _eventAggregator = eventAggregator;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(ClientMessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }

        private void RightDrawer_OnOpened(object sender, RoutedEventArgs e)
        {
            UpdateListBoxItemsSource();
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
            UpdateListBoxItemsSource();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var cache = CommandListBox.SelectedItem as CommandExtensionCache;
            if (cache == null)
            {
                return;
            }

            _dataService.DeleteExtensionCommandCache(cache.Id);
            UpdateListBoxItemsSource();
        }

        private void UpdateListBoxItemsSource()
        {
            var commandCache = _dataService.LoadCommandExtensionCaches(1, ConnectionType.TcpClient);
            CommandListBox.ItemsSource = commandCache.ToObservableCollection();
        }

        private void ExecuteCommandButton_OnClick(object sender, RoutedEventArgs e)
        {
            var cache = CommandListBox.SelectedItem as CommandExtensionCache;
            if (cache == null)
            {
                Console.WriteLine(@"cache == null");
                return;
            }

            _eventAggregator.GetEvent<DrawerExecuteCommandEvent>().Publish(cache);
        }
    }
}
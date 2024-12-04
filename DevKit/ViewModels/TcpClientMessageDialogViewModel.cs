using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class TcpClientMessageDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { set; get; }

        #region VM

        private ObservableCollection<MessageModel> _messageCollection = new ObservableCollection<MessageModel>();

        public ObservableCollection<MessageModel> MessageCollection
        {
            set
            {
                _messageCollection = value;
                RaisePropertyChanged();
            }
            get => _messageCollection;
        }

        private ObservableCollection<ExCommandCache> _exCommandCollection =
            new ObservableCollection<ExCommandCache>();

        public ObservableCollection<ExCommandCache> ExCommandCollection
        {
            set
            {
                _exCommandCollection = value;
                RaisePropertyChanged();
            }
            get => _exCommandCollection;
        }

        private bool _showHex = true;

        public bool ShowHex
        {
            set
            {
                _showHex = value;
                RaisePropertyChanged();
            }
            get => _showHex;
        }

        private string _userInputText = string.Empty;

        public string UserInputText
        {
            set
            {
                _userInputText = value;
                RaisePropertyChanged();
            }
            get => _userInputText;
        }

        private bool _sendHex = true;

        public bool SendHex
        {
            set
            {
                _sendHex = value;
                RaisePropertyChanged();
            }
            get => _sendHex;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand DropDownOpenedCommand { set; get; }
        public DelegateCommand<object> DeleteExCmdCommand { set; get; }
        public DelegateCommand<ComboBox> DropDownClosedCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand ShowHexCheckedCommand { set; get; }
        public DelegateCommand ShowHexUncheckedCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly IDialogService _dialogService;
        
        public TcpClientMessageDialogViewModel(IAppDataService dataService, IEventAggregator eventAggregator,
            IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpServer)
                .ToObservableCollection();

            eventAggregator.GetEvent<TcpClientMessageEvent>().Subscribe(delegate(byte[] bytes)
            {
                var messageModel = new MessageModel
                {
                    Content = _showHex
                        ? BitConverter.ToString(bytes).Replace("-", " ")
                        : Encoding.UTF8.GetString(bytes),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
            });

            DropDownOpenedCommand = new DelegateCommand(DropDownOpened);
            DeleteExCmdCommand = new DelegateCommand<object>(DeleteExCmd);
            DropDownClosedCommand = new DelegateCommand<ComboBox>(DropDownClosed);
            ExtensionCommand = new DelegateCommand(AddExtensionCommand);
            ShowHexCheckedCommand = new DelegateCommand(ShowHexChecked);
            ShowHexUncheckedCommand = new DelegateCommand(ShowHexUnchecked);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);
        }

        private void DropDownOpened()
        {
            ExCommandCollection = _dataService.LoadCommandExtensionCaches(ConnectionType.TcpServer)
                .ToObservableCollection();
        }

        private void DeleteExCmd(object obj)
        {
            var result = MessageBox.Show(
                "确定删除此条扩展指令？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question
            );
            if (result == MessageBoxResult.OK)
            {
                _dataService.DeleteExtensionCommandCache((int)obj);
            }
        }

        private void DropDownClosed(ComboBox box)
        {
            if (box.SelectedIndex == -1)
            {
                box.SelectedIndex = 0;
            }

            var commandCache = _exCommandCollection[box.SelectedIndex];
            UserInputText = commandCache.CommandValue;
        }

        private void AddExtensionCommand()
        {
            var dialogParameters = new DialogParameters
            {
                { "ConnectionType", ConnectionType.TcpServer }
            };
            _dialogService.Show("ExCommandDialog", dialogParameters, delegate { });
        }

        private void ShowHexChecked()
        {
            var boxResult = MessageBox.Show(
                "切换到HEX显示，可能会显示乱码，确定执行吗？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
            );
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为string
                    var hex = model.Content.StringToHex();
                    var msg = new MessageModel
                    {
                        Content = hex.Replace("-", " "),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;
            }
        }

        private void ShowHexUnchecked()
        {
            var boxResult = MessageBox.Show("确定切换到字符串显示？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (boxResult == MessageBoxResult.OK)
            {
                var collection = new ObservableCollection<MessageModel>();
                foreach (var model in MessageCollection)
                {
                    //将model.Content视为Hex，先转bytes[]，再转string
                    var bytes = model.Content.HexToBytes();
                    var msg = new MessageModel
                    {
                        Content = bytes.ByteArrayToString(),
                        Time = model.Time,
                        IsSend = model.IsSend
                    };
                    collection.Add(msg);
                }

                MessageCollection = collection;
            }
        }

        private void ClearMessage()
        {
            MessageCollection?.Clear();
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = new MessageModel();
            if (_sendHex)
            {
                if (!_userInputText.IsHex())
                {
                    MessageBox.Show("错误的16进制数据，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // _tcpClient.SendAsync(_userInputText);
            //
            // message.Content = _userInputText;
            // message.Time = DateTime.Now.ToString("HH:mm:ss.fff");
            // message.IsSend = true;
            // MessageCollection.Add(message);
        }

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            RuntimeCache.IsClientViewShowing = false;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var client = parameters.GetValue<TcpClientModel>("TcpClientModel");
            Title = $"{client.Ip}:{client.Port}";

            foreach (var bytes in client.MessageCollection)
            {
                var messageModel = new MessageModel
                {
                    Content = _showHex
                        ? BitConverter.ToString(bytes).Replace("-", " ")
                        : Encoding.UTF8.GetString(bytes),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                MessageCollection.Add(messageModel);
            }
        }
    }
}
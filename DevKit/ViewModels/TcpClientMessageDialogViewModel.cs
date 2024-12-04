using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using DevKit.Events;
using DevKit.Models;
using DevKit.Utils;
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

        #endregion

        #region DelegateCommand

        

        #endregion
        
        public TcpClientMessageDialogViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<TcpClientMessageEvent>().Subscribe(delegate(byte[] bytes)
            {
                var messageModel = new MessageModel
                {
                    Content = true
                        ? BitConverter.ToString(bytes).Replace("-", " ")
                        : Encoding.UTF8.GetString(bytes),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };

                Application.Current.Dispatcher.Invoke(() => { MessageCollection.Add(messageModel); });
            });
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
        }
    }
}
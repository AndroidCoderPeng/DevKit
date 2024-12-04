using System;
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

        public TcpClientMessageDialogViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<TcpClientMessageEvent>().Subscribe(delegate(byte[] bytes)
            {
                Console.WriteLine(BitConverter.ToString(bytes));
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
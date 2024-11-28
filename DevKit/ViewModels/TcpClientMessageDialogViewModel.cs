using System;
using DevKit.Models;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class TcpClientMessageDialogViewModel: BindableBase, IDialogAware
    {
        public string Title => string.Empty;
        
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
        }
        
        public void OnDialogOpened(IDialogParameters parameters)
        {
            var clientModel = parameters.GetValue<TcpClientModel>("TcpClientModel");
        }
    }
}
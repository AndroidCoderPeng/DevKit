using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevKit.ViewModels
{
    public class SerialPortViewModel: ObservableObject
    {
        public string Title => "串口通信";

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
        }
    }
}
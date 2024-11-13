using System;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class LoadingDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => string.Empty;

        private string _loadingMessage;

        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                _loadingMessage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 空实现
        /// </summary>
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
            LoadingMessage = parameters.GetValue<string>("LoadingMessage");
        }
    }
}
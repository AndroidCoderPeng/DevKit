using System;
using DevKit.Events;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class LoadingDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "DevKit";

        public event Action<IDialogResult> RequestClose;
        
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

        public LoadingDialogViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<CloseLoadingDialogEvent>().Subscribe(delegate
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            });
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
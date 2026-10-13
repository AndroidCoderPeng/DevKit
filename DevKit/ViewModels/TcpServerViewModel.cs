using System;
using System.Collections.Generic;
using DevKit.DataService;
using DevKit.Transports;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class TcpServerViewModel : BindableBase, IDialogAware
    {
        public string Title => "TCP服务端";

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

        #region VM

        private List<string> _localHostSource;

        public List<string> LocalHostSource
        {
            set
            {
                _localHostSource = value;
                RaisePropertyChanged();
            }
            get => _localHostSource;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand ServerListenCommand { set; get; }

        #endregion

        private TcpServerTransport _server;
        
        public TcpServerViewModel(IAppDataService dataService)
        {
            LocalHostSource = dataService.GetIPv4Address();
        }
    }
}
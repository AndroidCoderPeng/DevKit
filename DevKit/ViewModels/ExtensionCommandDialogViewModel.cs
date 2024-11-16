using System;
using System.Collections.ObjectModel;
using DevKit.Cache;
using DevKit.DataService;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class ExtensionCommandDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "扩展指令";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        #region VM

        private string _userCommandValue = string.Empty;

        public string UserCommandValue
        {
            set
            {
                _userCommandValue = value;
                RaisePropertyChanged();
            }
            get => _userCommandValue;
        }

        private string _commandAnnotation = string.Empty;

        public string CommandAnnotation
        {
            set
            {
                _commandAnnotation = value;
                RaisePropertyChanged();
            }
            get => _commandAnnotation;
        }

        private bool _isHex;

        public bool IsHex
        {
            set
            {
                _isHex = value;
                RaisePropertyChanged();
            }
            get => _isHex;
        }

        private ObservableCollection<string> _commandCollection = new ObservableCollection<string>();

        public ObservableCollection<string> CommandCollection
        {
            set
            {
                _commandCollection = value;
                RaisePropertyChanged();
            }
            get => _commandCollection;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand SaveExtensionCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private CommandExtensionCache _commandCache;

        public ExtensionCommandDialogViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            SaveExtensionCommand = new DelegateCommand(SaveExtension);
        }

        private void SaveExtension()
        {
            Console.WriteLine(_isHex);
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
            _commandCache = _dataService.LoadCommandExtensionCache();
            UserCommandValue = _commandCache.Command;
            CommandAnnotation = _commandCache.Annotation;
            IsHex = _commandCache.IsHex == 1;
        }
    }
}
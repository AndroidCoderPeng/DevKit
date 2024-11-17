using System;
using System.Collections.ObjectModel;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Utils;
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
        private int _parentId;
        private int _parentType;

        public ExtensionCommandDialogViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            SaveExtensionCommand = new DelegateCommand(SaveExtension);
        }

        private void SaveExtension()
        {
            if (string.IsNullOrWhiteSpace(_userCommandValue))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var cache = new CommandExtensionCache
            {
                ParentId = _parentId,
                ParentType = _parentType,
                Command = _userCommandValue,
                Annotation = _commandAnnotation
            };

            if (_isHex)
            {
                //检查是否是正确的Hex指令
                if (!_userCommandValue.IsHex())
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
            _parentId = parameters.GetValue<int>("ParentId");
            _parentType = parameters.GetValue<int>("ParentType");

            _commandCache = _dataService.LoadCommandExtensionCache(_parentId, _parentType);

            UserCommandValue = _commandCache.Command;
            CommandAnnotation = _commandCache.Annotation;
            IsHex = _commandCache.IsHex == 1;
        }
    }
}
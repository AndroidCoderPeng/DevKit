using System;
using System.Collections.ObjectModel;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Events;
using DevKit.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class ExCommandDialogViewModel : BindableBase, IDialogAware
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

        private bool _isHexChecked;

        public bool IsHexChecked
        {
            set
            {
                _isHexChecked = value;
                RaisePropertyChanged();
            }
            get => _isHexChecked;
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

        private ObservableCollection<ExCommandCache> _userCommandCollection =
            new ObservableCollection<ExCommandCache>();

        public ObservableCollection<ExCommandCache> UserCommandCollection
        {
            set
            {
                _userCommandCollection = value;
                RaisePropertyChanged();
            }
            get => _userCommandCollection;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ExtensionCommandSaveCommand { set; get; }
        public DelegateCommand<string> ExecuteCommand { set; get; }

        #endregion

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            _type = parameters.GetValue<int>("ConnectionType");
            UserCommandCollection = _dataService.LoadCommandExtensionCaches(_type).ToObservableCollection();
        }

        private readonly IAppDataService _dataService;
        private int _type;

        public ExCommandDialogViewModel(IAppDataService dataService, IEventAggregator eventAggregator)
        {
            _dataService = dataService;

            ExtensionCommandSaveCommand = new DelegateCommand(ExtensionCommandSave);
            ExecuteCommand = new DelegateCommand<string>(delegate(string command)
            {
                eventAggregator.GetEvent<ExecuteExCommandEvent>().Publish(command);
            });

            eventAggregator.GetEvent<DeleteExCommandEvent>().Subscribe(delegate(ExCommandCache cache)
            {
                _dataService.DeleteExtensionCommandCache(cache.Id);
                UserCommandCollection = _dataService.LoadCommandExtensionCaches(_type).ToObservableCollection();
            });
        }

        private void ExtensionCommandSave()
        {
            if (string.IsNullOrWhiteSpace(_userCommandValue))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var annotation = string.IsNullOrWhiteSpace(_commandAnnotation)
                ? $"指令{UserCommandCollection.Count + 1}"
                : _userCommandValue;
            var cache = new ExCommandCache
            {
                ParentType = _type,
                CommandValue = _userCommandValue,
                Annotation = annotation
            };

            if (_isHexChecked)
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
            UserCommandCollection = _dataService.LoadCommandExtensionCaches(_type).ToObservableCollection();
        }
    }
}
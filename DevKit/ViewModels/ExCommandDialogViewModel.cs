using System;
using System.Windows;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class ExCommandDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "添加扩展指令";

        public event Action<IDialogResult> RequestClose;

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

        #endregion

        #region DelegateCommand

        public DelegateCommand CommandSaveCommand { set; get; }
        public DelegateCommand CancelDialogCommand { set; get; }

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
        }

        private readonly IAppDataService _dataService;
        private int _type;

        public ExCommandDialogViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            CommandSaveCommand = new DelegateCommand(ExtensionCommandSave);
            CancelDialogCommand = new DelegateCommand(CancelDialog);
        }

        private void ExtensionCommandSave()
        {
            if (string.IsNullOrWhiteSpace(_userCommandValue))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var cache = new ExCommandCache
            {
                ParentType = _type,
                CommandValue = _userCommandValue,
                Annotation = _commandAnnotation
            };

            if (_isHexChecked)
            {
                //检查是否是正确的Hex指令
                if (!_userCommandValue.IsHex())
                {
                    MessageBox.Show("预设的指令不是正确的HEX", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                cache.IsHex = 1;
            }
            else
            {
                cache.IsHex = 0;
            }

            _dataService.SaveCacheConfig(cache);
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        private void CancelDialog()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }
    }
}
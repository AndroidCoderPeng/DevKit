using System;
using System.Linq;
using System.Windows;
using DevKit.Cache;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class ExCommandDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { private set; get; }
        
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
            if (parameters.Keys.Any())
            {
                Title = "编辑扩展指令";
                var cache = parameters.GetValue<ExCommandCache>("ExCommandCache");
                UserCommandValue = cache.CommandValue;
                CommandAnnotation = cache.Annotation;
            }
            else
            {
                Title = "添加扩展指令";
            }
        }

        public ExCommandDialogViewModel()
        {
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

            if (!_userCommandValue.IsHex())
            {
                MessageBox.Show("预设的指令不是正确的16进制", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "CommandValue", _userCommandValue },
                { "Annotation", _commandAnnotation }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
        }

        private void CancelDialog()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }
    }
}
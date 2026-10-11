using System;
using System.Linq;
using System.Windows;
using DevKit.Cache;
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

        private string _exCommandValue = string.Empty;

        public string ExCommandValue
        {
            get => _exCommandValue;
            set => SetProperty(ref _exCommandValue, value);
        }

        private string _commandMemo = string.Empty;

        public string CommandMemo
        {
            get => _commandMemo;
            set => SetProperty(ref _commandMemo, value);
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
                ExCommandValue = cache.CommandValue;
                CommandMemo = cache.Annotation;
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
            if (string.IsNullOrWhiteSpace(_exCommandValue))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "CommandValue", _exCommandValue },
                { "Annotation", _commandMemo }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
        }

        private void CancelDialog()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }
    }
}
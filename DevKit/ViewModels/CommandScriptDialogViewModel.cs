using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevKit.Cache;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class CommandScriptDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "指令执行脚本（鼠标左键选择需要执行的指令）";

        public event Action<IDialogResult> RequestClose;

        #region VM

        private ObservableCollection<string> _exCommandCaches = new ObservableCollection<string>();

        public ObservableCollection<string> ExCommandCaches
        {
            set
            {
                _exCommandCaches = value;
                RaisePropertyChanged();
            }
            get => _exCommandCaches;
        }

        private ObservableCollection<string> _selectedCommands = new ObservableCollection<string>();

        public ObservableCollection<string> SelectedCommands
        {
            set
            {
                _selectedCommands = value;
                RaisePropertyChanged();
            }
            get => _selectedCommands;
        }

        private string _interval = "1000";

        public string Interval
        {
            set
            {
                _interval = value;
                RaisePropertyChanged();
            }
            get => _interval;
        }
        
        #endregion

        #region DelegateCommand

        public DelegateCommand<object> ListBoxItemSelectedCommand { set; get; }
        public DelegateCommand<string> DeleteSelectedItemCommand { set; get; }
        public DelegateCommand SaveScriptCommand { set; get; }
        public DelegateCommand CancelDialogCommand { set; get; }

        #endregion

        private List<ExCommandCache> _commandCache;

        public CommandScriptDialogViewModel()
        {
            ListBoxItemSelectedCommand = new DelegateCommand<object>(OnListBoxItemSelected);
            DeleteSelectedItemCommand = new DelegateCommand<string>(OnSelectedItemDeleted);
            SaveScriptCommand = new DelegateCommand(SaveScript);
            CancelDialogCommand = new DelegateCommand(CancelDialog);
        }

        private void OnListBoxItemSelected(object index)
        {
            if (index == null)
            {
                return;
            }

            try
            {
                var command = _commandCache[(int)index];
                var annotation = command.Annotation;
                if (!SelectedCommands.Contains(annotation))
                {
                    SelectedCommands.Add(annotation);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void OnSelectedItemDeleted(string annotation)
        {
            var find = _commandCache.Find(x => x.Annotation == annotation);
            SelectedCommands.Remove(find.Annotation);
        }
        
        private void SaveScript()
        {
            var result = new List<string>();
            foreach (var str in _selectedCommands)
            {
                var command = _commandCache.Find(x => x.Annotation.Equals(str));
                result.Add(command.CommandValue);
            }
            var dialogParameters = new DialogParameters
            {
                { "SelectedCommands", result },
                { "Interval", _interval }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
        }

        private void CancelDialog()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
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
            _commandCache = parameters.GetValue<List<ExCommandCache>>("ExCommandCache");
            foreach (var cache in _commandCache)
            {
                var command = $"【{cache.Annotation}】{cache.CommandValue}";
                ExCommandCaches.Add(command);
            }
        }
    }
}
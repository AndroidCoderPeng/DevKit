using System;
using System.Collections.ObjectModel;
using DevKit.Cache;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class CommandScriptDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "指令脚本";

        public event Action<IDialogResult> RequestClose;

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
            ExCommandCollection = parameters.GetValue<ObservableCollection<ExCommandConfig>>("ExCommands");
        }
        
        #region VM

        private ObservableCollection<ExCommandConfig> _exCommandCollection = new ObservableCollection<ExCommandConfig>();

        public ObservableCollection<ExCommandConfig> ExCommandCollection
        {
            get => _exCommandCollection;
            set => SetProperty(ref _exCommandCollection, value);
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

        public CommandScriptDialogViewModel()
        {
            ListBoxItemSelectedCommand = new DelegateCommand<object>(OnListBoxItemSelected);
            DeleteSelectedItemCommand = new DelegateCommand<string>(OnSelectedItemDeleted);
            SaveScriptCommand = new DelegateCommand(SaveScript);
            CancelDialogCommand = new DelegateCommand(CancelDialog);
        }

        private void OnListBoxItemSelected(object index)
        {
            // if (index == null)
            // {
            //     return;
            // }
            //
            // try
            // {
            //     var command = _commands[(int)index];
            //     var annotation = command.Annotation;
            //     if (!SelectedCommands.Contains(annotation))
            //     {
            //         SelectedCommands.Add(annotation);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e);
            // }
        }

        private void OnSelectedItemDeleted(string annotation)
        {
            // var find = _commands.Find(x => x.Annotation == annotation);
            // SelectedCommands.Remove(find.Annotation);
        }
        
        private void SaveScript()
        {
            // var result = new List<string>();
            // foreach (var str in _selectedCommands)
            // {
            //     var command = _commands.Find(x => x.Annotation.Equals(str));
            //     result.Add(command.CommandValue);
            // }
            // var dialogParameters = new DialogParameters
            // {
            //     { "SelectedCommands", result },
            //     { "Interval", _interval }
            // };
            // RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
        }
    }
}
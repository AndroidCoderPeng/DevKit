using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Utils;

namespace DevKit.ViewModels
{
    public partial class CommandScriptDialogViewModel : ObservableObject, IDialogParameterReceiver,
        IDialogCloseRequester
    {
        #region VM

        [ObservableProperty] private ObservableCollection<ExCommandConfig> _exCommandCollection = [];
        [ObservableProperty] private ObservableCollection<ExCommandConfig> _selectedCommands = [];

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(TotalTime))]
        private string _interval = "1000";

        public string TotalTime
        {
            get
            {
                if (int.TryParse(Interval, NumberStyles.Integer, CultureInfo.CurrentCulture, out var interval) &&
                    interval > 0)
                {
                    var totalSeconds = interval * (double)SelectedCommands.Count / 1000;
                    return totalSeconds.ToString("0.##", CultureInfo.CurrentCulture);
                }

                return "0";
            }
        }

        #endregion

        public event Action<bool> CloseRequested;
        private CommandScriptDialogContext _dialogContext;

        public void OnDialogOpened(object parameter)
        {
            _dialogContext = parameter as CommandScriptDialogContext ?? throw new ArgumentException(
                @"指令脚本对话框需要 CommandScriptDialogContext 参数。", nameof(parameter));

            ExCommandCollection = _dialogContext.ExCommands;
            SelectedCommands = _dialogContext.SelectedCommands;
            Interval = _dialogContext.Interval;
        }

        partial void OnSelectedCommandsChanging(
            ObservableCollection<ExCommandConfig> oldValue,
            ObservableCollection<ExCommandConfig> newValue)
        {
            ArgumentNullException.ThrowIfNull(newValue);
            oldValue.CollectionChanged -= SelectedCommands_CollectionChanged;
        }

        partial void OnSelectedCommandsChanged(
            ObservableCollection<ExCommandConfig> oldValue,
            ObservableCollection<ExCommandConfig> newValue)
        {
            _ = oldValue;
            newValue.CollectionChanged += SelectedCommands_CollectionChanged;
            OnPropertyChanged(nameof(TotalTime));
        }

        public CommandScriptDialogViewModel()
        {
            _selectedCommands.CollectionChanged += SelectedCommands_CollectionChanged;
        }

        private void SelectedCommands_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalTime));
        }

        [RelayCommand]
        private void ExCommandDoubleClick(ExCommandConfig command)
        {
            if (command == null || SelectedCommands.Contains(command))
            {
                return;
            }

            SelectedCommands.Add(command);
        }

        [RelayCommand]
        private void ClearSelected()
        {
            SelectedCommands.Clear();
        }

        [RelayCommand]
        private void DeleteSelected(ExCommandConfig command)
        {
            if (command == null)
            {
                return;
            }

            SelectedCommands.Remove(command);
        }

        [RelayCommand]
        private void CancelDialog()
        {
            SelectedCommands.CollectionChanged -= SelectedCommands_CollectionChanged;
            CloseRequested?.Invoke(false);
        }

        [RelayCommand]
        private void ConfirmDialog()
        {
            var selectedCommands = SelectedCommands.ToList();

            _dialogContext.SelectedCommands.Clear();
            foreach (var command in selectedCommands)
            {
                _dialogContext.SelectedCommands.Add(command);
            }

            _dialogContext.Interval = Interval;
            SelectedCommands.CollectionChanged -= SelectedCommands_CollectionChanged;
            CloseRequested?.Invoke(true);
        }
    }
}
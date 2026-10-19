using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DevKit.Cache;
using DevKit.Utils;

namespace DevKit.ViewModels
{
    public partial class CommandScriptDialogViewModel : ObservableObject, IDialogParameterReceiver,
        IDialogCloseRequester
    {
        #region VM

        [ObservableProperty] private ObservableCollection<ExCommandConfig> _exCommandCollection = [];

        private ObservableCollection<ExCommandConfig> _selectedCommands = new ObservableCollection<ExCommandConfig>();

        public ObservableCollection<ExCommandConfig> SelectedCommands
        {
            get => _selectedCommands;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var previous = _selectedCommands;
                if (!SetProperty(ref _selectedCommands, value))
                {
                    return;
                }

                previous.CollectionChanged -= SelectedCommands_CollectionChanged;
                _selectedCommands.CollectionChanged += SelectedCommands_CollectionChanged;
                OnPropertyChanged(nameof(TotalTime));
            }
        }

        private string _interval = "1000";

        public string Interval
        {
            get => _interval;
            set
            {
                if (SetProperty(ref _interval, value))
                {
                    OnPropertyChanged(nameof(TotalTime));
                }
            }
        }

        public string TotalTime
        {
            get
            {
                if (int.TryParse(_interval, NumberStyles.Integer, CultureInfo.CurrentCulture, out var interval) &&
                    interval > 0)
                {
                    var totalSeconds = interval * (double)_selectedCommands.Count / 1000;
                    return totalSeconds.ToString("0.##", CultureInfo.CurrentCulture);
                }

                return "0";
            }
        }

        #endregion

        public event Action<bool> CloseRequested;

        public void OnDialogOpened(object parameter)
        {
            
        }

        public CommandScriptDialogViewModel()
        {
            _selectedCommands.CollectionChanged += SelectedCommands_CollectionChanged;

            ExCommandDoubleClickCommand = new RelayCommand<ExCommandConfig>(item =>
            {
                if (item == null || SelectedCommands.Contains(item))
                {
                    return;
                }

                SelectedCommands.Add(item);
            });

            ClearSelectedCommand = new RelayCommand(() => { SelectedCommands.Clear(); });

            DeleteSelectedCommand = new RelayCommand<ExCommandConfig>(item =>
            {
                if (item == null)
                {
                    return;
                }

                SelectedCommands.Remove(item);
            });

            CancelDialogCommand = new RelayCommand(() => { CloseRequested?.Invoke(false); });

            ConfirmDialogCommand = new RelayCommand(() =>
            {
                // _dialogContext.Commands = new ObservableCollection<ExCommandConfig>(_selectedCommands);
                // _dialogContext.Interval = _interval;
                CloseRequested?.Invoke(true);
            });
        }
        
        private void SelectedCommands_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalTime));
        }
    }
}
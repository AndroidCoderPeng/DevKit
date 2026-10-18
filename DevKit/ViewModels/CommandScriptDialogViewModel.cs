using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using DevKit.Cache;

namespace DevKit.ViewModels
{
    public class CommandScriptDialogViewModel : ObservableObject
    {
        public string Title => "指令脚本";

        public event Action<IDialogResult> RequestClose;

        private void CancelDialog()
        {
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

        private ObservableCollection<ExCommandConfig>
            _exCommandCollection = new ObservableCollection<ExCommandConfig>();

        public ObservableCollection<ExCommandConfig> ExCommandCollection
        {
            get => _exCommandCollection;
            set => SetProperty(ref _exCommandCollection, value);
        }

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

        #region DelegateCommand

        public DelegateCommand<ExCommandConfig> ExCommandDoubleClickCommand { set; get; }
        public DelegateCommand ClearSelectedCommand { set; get; }
        public DelegateCommand<ExCommandConfig> DeleteSelectedCommand { set; get; }
        public DelegateCommand CancelDialogCommand { set; get; }
        public DelegateCommand ConfirmDialogCommand { set; get; }

        #endregion

        public CommandScriptDialogViewModel()
        {
            _selectedCommands.CollectionChanged += SelectedCommands_CollectionChanged;

            ExCommandDoubleClickCommand = new DelegateCommand<ExCommandConfig>(item =>
            {
                if (item == null || SelectedCommands.Contains(item))
                {
                    return;
                }

                SelectedCommands.Add(item);
            });

            ClearSelectedCommand = new DelegateCommand(() => { SelectedCommands.Clear(); });

            DeleteSelectedCommand = new DelegateCommand<ExCommandConfig>(item =>
            {
                if (item == null)
                {
                    return;
                }

                SelectedCommands.Remove(item);
            });
            
            CancelDialogCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            });
            
            ConfirmDialogCommand = new DelegateCommand(() =>
            {
                var dialogParameters = new DialogParameters
                {
                    { "Commands", _selectedCommands },
                    { "Interval", _interval }
                };
                
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
            });
        }

        private void SelectedCommands_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalTime));
        }
    }
}
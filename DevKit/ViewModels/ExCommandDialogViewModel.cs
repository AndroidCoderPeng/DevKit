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

        private string _commandType = "HEX";

        public string CommandType
        {
            get => _commandType;
            set => SetProperty(ref _commandType, value);
        }

        private string _tips = "HEX 字节，空格分隔";

        public string Tips
        {
            get => _tips;
            set => SetProperty(ref _tips, value);
        }

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

        public DelegateCommand<string> ComboBoxItemSelectedCommand { set; get; }
        public DelegateCommand CommandSaveCommand { set; get; }
        public DelegateCommand CancelDialogCommand { set; get; }

        #endregion

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            var config = SettingsStore.Load<TcpConfig>(ConfigSections.FileName, ConfigSections.Tcp);
            config.ExCommands.Add(new ExCommandConfig
            {
                Type = CommandType,
                Value = ExCommandValue,
                Memo = CommandMemo
            });
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Tcp, config);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.Keys.Any())
            {
                Title = "编辑扩展指令";
                // var cache = parameters.GetValue<ExCommandCache>("ExCommandCache");
                // ExCommandValue = cache.CommandValue;
                // CommandMemo = cache.Annotation;
            }
            else
            {
                Title = "添加扩展指令";
            }
        }

        public ExCommandDialogViewModel()
        {
            ComboBoxItemSelectedCommand = new DelegateCommand<string>(type =>
            {
                if (type == "0")
                {
                    CommandType = "HEX";
                    Tips = "HEX 字节，空格分隔";
                }
                else if (type == "1")
                {
                    CommandType = "ASCII";
                    Tips = "ASCII 文本，原样发送";
                }
            });

            CommandSaveCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(_exCommandValue))
                {
                    MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var config = SettingsStore.Load<TcpConfig>(ConfigSections.FileName, ConfigSections.Tcp);
                if (config.ExCommands.Any(x => x.Value == _exCommandValue))
                {
                    MessageBox.Show("该指令已存在", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            });

            CancelDialogCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            });
        }
    }
}
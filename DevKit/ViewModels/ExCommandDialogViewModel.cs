using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private string _commandType = "HEX";

        public string CommandType
        {
            get => _commandType;
            set
            {
                if (SetProperty(ref _commandType, value))
                {
                    RaisePropertyChanged(nameof(IsHexSelected));
                    RaisePropertyChanged(nameof(IsAsciiSelected));
                    RaisePropertyChanged(nameof(Tips));
                    RaisePropertyChanged(nameof(PlaceHolderText));
                }
            }
        }

        public bool IsHexSelected
        {
            get => CommandType == "HEX";
            set
            {
                if (value) CommandType = "HEX";
            }
        }

        public bool IsAsciiSelected
        {
            get => CommandType == "ASCII";
            set
            {
                if (value) CommandType = "ASCII";
            }
        }

        public string Tips => CommandType == "ASCII" ? "ASCII 文本，原样发送" : "HEX 字节，空格分隔";

        public string PlaceHolderText => CommandType == "ASCII" ? "例如：AT + RST" : "例如：aa550101，点格式化 → AA 55 01 01";

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
        public DelegateCommand FormatStyleCommand { set; get; }
        public DelegateCommand CommandSaveCommand { set; get; }
        public DelegateCommand CancelDialogCommand { set; get; }

        #endregion

        private static readonly Regex HexTokenRegex = new Regex("^[0-9a-fA-F]{1,2}$", RegexOptions.Compiled);

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
                var command = parameters.GetValue<ExCommandConfig>("ExCommand");
                CommandType = command.Type;
                ExCommandValue = command.Value;
                CommandMemo = command.Memo;
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
                }
                else if (type == "1")
                {
                    CommandType = "ASCII";
                }
            });

            // 格式化（仅 HEX 有意义）：去空白 → 大写 → 奇数长度补零 → 每两位一组空格分隔
            FormatStyleCommand = new DelegateCommand(() =>
            {
                if (CommandType != "HEX" || string.IsNullOrWhiteSpace(_exCommandValue))
                {
                    return;
                }

                var compact = string.Concat(_exCommandValue.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
                if (compact.Length % 2 == 1)
                {
                    compact = "0" + compact;
                }

                var builder = new StringBuilder();
                for (var i = 0; i < compact.Length; i += 2)
                {
                    builder.Append(compact, i, 2);
                    if (i + 2 < compact.Length)
                    {
                        builder.Append(' ');
                    }
                }

                ExCommandValue = builder.ToString();
            });

            CommandSaveCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(_exCommandValue))
                {
                    MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // HEX 模式下校验字节格式
                if (_commandType == "HEX")
                {
                    var invalid = _exCommandValue
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(token => !HexTokenRegex.IsMatch(token));
                    if (invalid)
                    {
                        MessageBox.Show(
                            "指令值格式不正确：仅支持十六进制字节（00 - FF），以空格分隔",
                            "温馨提示", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }

                var dialogParameters = new DialogParameters
                {
                    { "Type", _commandType },
                    { "Value", _exCommandValue },
                    { "Memo", _commandMemo }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, dialogParameters));
            });

            CancelDialogCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            });
        }
    }
}
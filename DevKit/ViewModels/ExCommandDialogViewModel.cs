using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Cache;
using DevKit.Utils;
using MessageBox = System.Windows.MessageBox;

namespace DevKit.ViewModels
{
    public partial class ExCommandDialogViewModel : ObservableObject, IDialogParameterReceiver, IDialogCloseRequester
    {
        #region VM

        [ObservableProperty] private string _title = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHexSelected))]
        [NotifyPropertyChangedFor(nameof(IsAsciiSelected))]
        [NotifyPropertyChangedFor(nameof(Tips))]
        [NotifyPropertyChangedFor(nameof(PlaceHolderText))]
        private string _commandType = "HEX";

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

        [ObservableProperty] private string _exCommandValue = string.Empty;
        [ObservableProperty] private string _commandMemo = string.Empty;

        #endregion

        public event Action<bool> CloseRequested;
        private ExCommandConfig _configContext;
        
        [GeneratedRegex("^[0-9a-fA-F]{1,2}$", RegexOptions.Compiled)]
        private partial Regex HexTokenRegex();

        public void OnDialogOpened(object parameter)
        {
            if (parameter is not ExCommandConfig context)
            {
                throw new ArgumentException(@"扩展指令对话框需要 ExCommandConfig 参数。", nameof(parameter));
            }
            
            _configContext = context;
            var isEdit = !string.IsNullOrWhiteSpace(context.Value);

            Title = isEdit ? "编辑扩展指令" : "添加扩展指令";
            CommandType = string.IsNullOrWhiteSpace(context.Type) ? "HEX" : context.Type;
            ExCommandValue = context.Value ?? string.Empty;
            CommandMemo = context.Memo ?? string.Empty;
        }

        [RelayCommand]
        private void ComboBoxItemSelected(string type)
        {
            CommandType = type switch
            {
                "0" => "HEX",
                "1" => "ASCII",
                _ => CommandType
            };
        }

        /// <summary>
        /// 格式化（仅 HEX 有意义）：去空白 → 大写 → 奇数长度补零 → 每两位一组空格分隔
        /// </summary>
        [RelayCommand]
        private void FormatStyle()
        {
            if (CommandType != "HEX" || string.IsNullOrWhiteSpace(ExCommandValue))
            {
                return;
            }

            var compact = string.Concat(ExCommandValue.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
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
        }

        [RelayCommand]
        private void CommandSave()
        {
            if (string.IsNullOrWhiteSpace(ExCommandValue))
            {
                MessageBox.Show("需要预设的指令为空", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // HEX 模式下校验字节格式
            if (CommandType == "HEX")
            {
                var invalid = ExCommandValue
                    .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                    .Any(token => !HexTokenRegex().IsMatch(token));
                if (invalid)
                {
                    MessageBox.Show("指令值格式不正确：仅支持十六进制字节（00 - FF），以空格分隔",
                        "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            _configContext.Type = CommandType;
            _configContext.Value = ExCommandValue;
            _configContext.Memo = CommandMemo;
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void CancelDialog()
        {
            CloseRequested?.Invoke(false);
        }
    }
}
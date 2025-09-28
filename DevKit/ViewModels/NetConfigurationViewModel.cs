using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Utils;
using HandyControl.Tools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class NetConfigurationViewModel : BindableBase, IDialogAware
    {
        public string Title => "网络信息";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
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
        }

        #region VM

        private ObservableCollection<string> _commandItems = new ObservableCollection<string>();

        public ObservableCollection<string> CommandItems
        {
            get => _commandItems;
            set
            {
                _commandItems = value;
                RaisePropertyChanged();
            }
        }

        private string _targetAddress = string.Empty;

        public string TargetAddress
        {
            get => _targetAddress;
            set
            {
                _targetAddress = value;
                RaisePropertyChanged();
            }
        }

        private string _outputResult = string.Empty;

        public string OutputResult
        {
            get => _outputResult;
            set
            {
                _outputResult = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> ItemSelectedCommand { set; get; }
        public DelegateCommand TestNetCommand { set; get; }

        #endregion

        public NetConfigurationViewModel()
        {
            CommandItems = new ObservableCollection<string>
            {
                "ipconfig", "ping"
            };
            ItemSelectedCommand = new DelegateCommand<string>(ItemSelected);
            TestNetCommand = new DelegateCommand(TestNet);
        }

        private void ItemSelected(string commandValue)
        {
            if (commandValue.Equals("ping"))
            {
                // ping 命令会单独执行
                return;
            }

            Task.Run(() =>
            {
                ExecuteCommand(commandValue);
            });
        }

        private void TestNet()
        {
            if (_targetAddress.IsIp())
            {
                Task.Run(() =>
                {
                    var argument = new ArgumentCreator();
                    argument.Append("ping").Append(_targetAddress);
                    ExecuteCommand(argument.ToCommandLine());
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    var addresses = await Dns.GetHostAddressesAsync(_targetAddress);
                    var ip = addresses.FirstOrDefault()?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(ip))
                    {
                        MessageBox.Show("请输入正确的目标地址", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        var argument = new ArgumentCreator();
                        argument.Append("ping").Append(ip);
                        ExecuteCommand(argument.ToCommandLine());
                    }
                });
            }
        }

        private void ExecuteCommand(string command)
        {
            var executor = new CommandExecutor($"/c {command}");
            var result = new StringBuilder();
            executor.OnStandardOutput += delegate(string value)
            {
                result.AppendLine(value);
                OutputResult = result.ToString();
            };
            executor.Execute("cmd");
        }
    }
}
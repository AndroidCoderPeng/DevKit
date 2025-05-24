using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UdpClient = TouchSocket.Sockets.UdpSession;

namespace DevKit.ViewModels
{
    public class UdpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "UDP客户端";

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

        private string _remoteAddress = string.Empty;

        public string RemoteAddress
        {
            set
            {
                _remoteAddress = value;
                RaisePropertyChanged();
            }
            get => _remoteAddress;
        }

        private string _remotePort = "5000";

        public string RemotePort
        {
            set
            {
                _remotePort = value;
                RaisePropertyChanged();
            }
            get => _remotePort;
        }

        private string _buttonState = "连接";

        public string ButtonState
        {
            set
            {
                _buttonState = value;
                RaisePropertyChanged();
            }
            get => _buttonState;
        }

        private string _connectionStateColor = "red";

        public string ConnectionStateColor
        {
            set
            {
                _connectionStateColor = value;
                RaisePropertyChanged();
            }
            get => _connectionStateColor;
        }

        private ObservableCollection<ExCommandCache> _exCommandCollection = new ObservableCollection<ExCommandCache>();

        public ObservableCollection<ExCommandCache> ExCommandCollection
        {
            set
            {
                _exCommandCollection = value;
                RaisePropertyChanged();
            }
            get => _exCommandCollection;
        }

        private ObservableCollection<LogModel> _logs = new ObservableCollection<LogModel>();

        public ObservableCollection<LogModel> Logs
        {
            set
            {
                _logs = value;
                RaisePropertyChanged();
            }
            get => _logs;
        }

        private string _commandInterval = "1000";

        public string CommandInterval
        {
            set
            {
                _commandInterval = value;
                RaisePropertyChanged();
            }
            get => _commandInterval;
        }

        private string _userInputText = string.Empty;

        public string UserInputText
        {
            set
            {
                _userInputText = value;
                RaisePropertyChanged();
            }
            get => _userInputText;
        }

        private bool _isHexSelected = true;

        public bool IsHexSelected
        {
            set
            {
                _isHexSelected = value;
                RaisePropertyChanged();
            }
            get => _isHexSelected;
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand SaveCommunicationCommand { set; get; }
        public DelegateCommand ClearCommunicationCommand { set; get; }
        public DelegateCommand AddExtensionCommand { set; get; }
        public DelegateCommand<string> DataGridItemSelectedCommand { set; get; }
        public DelegateCommand<string> CopyLogCommand { set; get; }
        public DelegateCommand SendCommand { set; get; }
        public DelegateCommand<string> CopyCommand { set; get; }
        public DelegateCommand<object> EditCommand { set; get; }
        public DelegateCommand<object> DeleteCommand { set; get; }
        public DelegateCommand OpenScriptCommand { set; get; }
        public DelegateCommand TimeCheckedCommand { set; get; }
        public DelegateCommand TimeUncheckedCommand { set; get; }
        public DelegateCommand<object> ComboBoxItemSelectedCommand { set; get; }

        #endregion

        private const string ClientType = "UDP";
        private readonly IDialogService _dialogService;
        private readonly UdpClient _udpClient = new UdpClient();
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();
        private IEnumerator<string> _commandEnumerator;

        public UdpClientViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            using (var dataBase = new DataBaseConnection())
            {
                //加载连接配置缓存
                var queryResult = dataBase.Table<ClientConfigCache>()
                    .Where(x => x.ClientType == ClientType)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();
                if (queryResult != null)
                {
                    RemoteAddress = queryResult.RemoteAddress;
                    RemotePort = queryResult.RemotePort.ToString();
                }

                //加载扩展指令缓存
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == ClientType)
                    .ToList();
                ExCommandCollection = commandCache.ToObservableCollection();
            }

            _udpClient.Received += (client, e) =>
            {
                UpdateCommunicationLog("", e.ByteBlock.ToArray());
                return EasyTask.CompletedTask;
            };

            SaveCommunicationCommand = new DelegateCommand(SaveCommunicationLog);
            ClearCommunicationCommand = new DelegateCommand(ClearCommunicationLog);
            AddExtensionCommand = new DelegateCommand(AddExtension);
            DataGridItemSelectedCommand = new DelegateCommand<string>(OnDataGridItemSelected);
            CopyLogCommand = new DelegateCommand<string>(CopyLog);
            SendCommand = new DelegateCommand(OnMessageSend);
            CopyCommand = new DelegateCommand<string>(OnCopy);
            EditCommand = new DelegateCommand<object>(OnEdit);
            DeleteCommand = new DelegateCommand<object>(OnDelete);
            OpenScriptCommand = new DelegateCommand(OpenScriptDialog);
            TimeCheckedCommand = new DelegateCommand(OnTimeChecked);
            TimeUncheckedCommand = new DelegateCommand(OnTimeUnchecked);
            ComboBoxItemSelectedCommand = new DelegateCommand<object>(OnComboBoxItemSelected);
        }

        private async void SaveCommunicationLog()
        {
            if (!_logs.Any())
            {
                MessageBox.Show("没有需要保存的日志", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fileDialog = new SaveFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Log文件(*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (fileDialog.ShowDialog() == true)
            {
                var savePath = fileDialog.FileName;
                try
                {
                    using (var writer = new StreamWriter(savePath))
                    {
                        foreach (var log in _logs)
                        {
                            var logText = log.IsSend
                                ? $"{log.Time}【发送】{log.Content}"
                                : $"{log.Time}【接收】{log.Content}";
                            await writer.WriteLineAsync(logText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存日志时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearCommunicationLog()
        {
            Logs.Clear();
        }

        private void AddExtension()
        {
            _dialogService.Show("ExCommandDialog", null, delegate(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                var commandValue = result.Parameters.GetValue<string>("CommandValue");
                var annotation = result.Parameters.GetValue<string>("Annotation");
                using (var dataBase = new DataBaseConnection())
                {
                    var exCommand = new ExCommandCache
                    {
                        ClientType = ClientType,
                        CommandValue = commandValue,
                        Annotation = annotation
                    };
                    dataBase.Insert(exCommand);
                    //刷新列表
                    ExCommandCollection.Clear();
                    var commandCache = dataBase.Table<ExCommandCache>()
                        .Where(x => x.ClientType == ClientType)
                        .ToList();
                    ExCommandCollection = commandCache.ToObservableCollection();
                }
            });
        }

        private void OnDataGridItemSelected(string command)
        {
            UserInputText = command;
        }

        private void CopyLog(string log)
        {
            Clipboard.SetText(log);
        }

        private void OnMessageSend()
        {
            SendMessage(_userInputText);
        }

        private void OnCopy(string command)
        {
            Clipboard.SetText(command);
        }

        private void OnEdit(object id)
        {
            var dialogParameters = new DialogParameters();
            ExCommandCache exCommand;

            using (var dataBase = new DataBaseConnection())
            {
                exCommand = dataBase.Table<ExCommandCache>().First(x => x.Id == (int)id);
                dialogParameters.Add("ExCommandCache", exCommand);
            }

            _dialogService.Show("ExCommandDialog", dialogParameters, delegate(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                var commandValue = result.Parameters.GetValue<string>("CommandValue");
                var annotation = result.Parameters.GetValue<string>("Annotation");
                exCommand.CommandValue = commandValue;
                exCommand.Annotation = annotation;
                using (var dataBase = new DataBaseConnection())
                {
                    dataBase.Update(exCommand);
                    //刷新列表
                    ExCommandCollection.Clear();
                    var commandCache = dataBase.Table<ExCommandCache>()
                        .Where(x => x.ClientType == ClientType)
                        .ToList();
                    ExCommandCollection = commandCache.ToObservableCollection();
                }
            });
        }

        private void OnDelete(object id)
        {
            using (var dataBase = new DataBaseConnection())
            {
                var itemToDelete = dataBase.Table<ExCommandCache>()
                    .First(x => x.Id == (int)id);
                dataBase.Delete(itemToDelete);
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == ClientType)
                    .ToList();
                ExCommandCollection = commandCache.ToObservableCollection();
            }
        }

        private void SendMessage(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_udpClient.RemoteIPHost == null)
            {
                if (string.IsNullOrWhiteSpace(_remoteAddress) || string.IsNullOrWhiteSpace(_remotePort))
                {
                    MessageBox.Show("IP或者端口未填写", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _udpClient.Setup(new TouchSocketConfig().UseUdpReceive().SetRemoteIPHost(
                    new IPHost($"{_remoteAddress}:{_remotePort}"))
                );
                _udpClient.Start();
                using (var dataBase = new DataBaseConnection())
                {
                    var queryResult = dataBase.Table<ClientConfigCache>()
                        .Where(x => x.ClientType == ClientType)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();
                    if (queryResult != null)
                    {
                        queryResult.RemoteAddress = _remoteAddress;
                        queryResult.RemotePort = Convert.ToInt32(_remotePort);
                        dataBase.Update(queryResult);
                    }
                    else
                    {
                        var config = new ClientConfigCache
                        {
                            ClientType = ClientType,
                            RemoteAddress = _remoteAddress,
                            RemotePort = Convert.ToInt32(_remotePort)
                        };
                        dataBase.Insert(config);
                    }
                }
            }
            else
            {
                Console.WriteLine($@"UDP服务端已配置，可直接发送消息【{_udpClient.RemoteIPHost}】");
            }

            if (_isHexSelected)
            {
                if (!command.IsHex())
                {
                    MessageBox.Show("16进制格式数据错误，请确认发送数据的模式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var bytes = command.Replace(" ", "").ByHexStringToBytes();
                _udpClient.Send(bytes);
                UpdateCommunicationLog(command, bytes);
            }
            else
            {
                var bytes = command.ToUTF8Bytes();
                _udpClient.Send(bytes);
                UpdateCommunicationLog(command, bytes);
            }
        }

        private void UpdateCommunicationLog(string command, byte[] bytes)
        {
            if (command.Equals(""))
            {
                //默认显示为UTF8编码
                var log = new LogModel
                {
                    Content = bytes.ByBytesToHexString(" "),
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = false
                };
                Application.Current.Dispatcher.Invoke(() => { Logs.Add(log); });
            }
            else
            {
                var log = new LogModel
                {
                    Content = command,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = true
                };
                Logs.Add(log);
            }
        }

        private void OpenScriptDialog()
        {
            var dialogParameters = new DialogParameters();

            using (var dataBase = new DataBaseConnection())
            {
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == ClientType)
                    .ToList();
                dialogParameters.Add("ExCommandCache", commandCache);
            }

            _dialogService.Show("CommandScriptDialog", dialogParameters, delegate(IDialogResult result)
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                var commands = result.Parameters.GetValue<List<string>>("SelectedCommands");
                var interval = result.Parameters.GetValue<string>("Interval");
                _commandEnumerator = commands.GetEnumerator();
                _scriptTimer.Tick += ScriptTimerTickEvent_Handler;
                _scriptTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(interval));
                _scriptTimer.Start();
            });
        }

        private void ScriptTimerTickEvent_Handler(object sender, EventArgs e)
        {
            if (_commandEnumerator.MoveNext())
            {
                SendMessage(_commandEnumerator.Current);
            }
            else
            {
                _scriptTimer.Stop();
                _scriptTimer.Tick -= ScriptTimerTickEvent_Handler;
            }
        }

        private void OnTimeChecked()
        {
            if (!_commandInterval.IsNumber())
            {
                MessageBox.Show("时间间隔仅支持正整数", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _loopSendCommandTimer.Tick += TimerTickEvent_Handler;
            _loopSendCommandTimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(_commandInterval));
            _loopSendCommandTimer.Start();
        }

        private void OnTimeUnchecked()
        {
            _loopSendCommandTimer.Tick -= TimerTickEvent_Handler;
            _loopSendCommandTimer.Stop();
        }

        private void TimerTickEvent_Handler(object sender, EventArgs e)
        {
            SendMessage(_userInputText);
        }

        private void OnComboBoxItemSelected(object index)
        {
            if (index == null)
            {
                return;
            }

            if (index.ToString().Equals("0"))
            {
                //转为16进制显示
                foreach (var log in _logs)
                {
                    var bytes = log.Content.ToUTF8Bytes();
                    log.Content = bytes.ByBytesToHexString(" ");
                }
            }
            else if (index.ToString().Equals("1"))
            {
                //转为ASCII显示
                foreach (var log in _logs)
                {
                    var bytes = log.Content.Replace(" ", "").ByHexStringToBytes();
                    log.Content = Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}
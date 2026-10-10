using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevKit.Cache;
using DevKit.Models;
using DevKit.Transports;
using DevKit.Utils;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DevKit.ViewModels
{
    public class TcpClientViewModel : BindableBase, IDialogAware
    {
        public string Title => "TCP客户端";

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
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.Tcp, new TcpConfig
            {
                Servers = new TcpEndpointConfig
                {
                    Ip = _remoteAddress,
                    Port = _remotePort
                }
            });
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        private string _stateOuterBackgroundColor = "#FFF7F9FC";

        public string StateOuterBackgroundColor
        {
            get => _stateOuterBackgroundColor;
            set => SetProperty(ref _stateOuterBackgroundColor, value);
        }

        private string _stateOuterBorderColor = "#FFEEEEF0";

        public string StateOuterBorderColor
        {
            get => _stateOuterBorderColor;
            set => SetProperty(ref _stateOuterBorderColor, value);
        }

        private string _stateInnerBackgroundColor = "#E7EBF0";

        public string StateInnerBackgroundColor
        {
            get => _stateInnerBackgroundColor;
            set => SetProperty(ref _stateInnerBackgroundColor, value);
        }

        private string _stateInnerBorderColor = "#93A0AE";

        public string StateInnerBorderColor
        {
            get => _stateInnerBorderColor;
            set => SetProperty(ref _stateInnerBorderColor, value);
        }

        private string _stateTextColor = "#5F6B7A";

        public string StateTextColor
        {
            get => _stateTextColor;
            set => SetProperty(ref _stateTextColor, value);
        }

        private string _connectionState = "未连接";

        public string ConnectionState
        {
            get => _connectionState;
            set => SetProperty(ref _connectionState, value);
        }

        private string _remoteAddress = string.Empty;

        public string RemoteAddress
        {
            get => _remoteAddress;
            set => SetProperty(ref _remoteAddress, value);
        }

        private string _remotePort = "9000";

        public string RemotePort
        {
            get => _remotePort;
            set => SetProperty(ref _remotePort, value);
        }

        private string _buttonStateText = "连接";

        public string ButtonStateText
        {
            get => _buttonStateText;
            set => SetProperty(ref _buttonStateText, value);
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
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

        private ObservableCollection<SocketMessage> _logs = new ObservableCollection<SocketMessage>();

        public ObservableCollection<SocketMessage> Logs
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

        public DelegateCommand ConnectServerCommand { set; get; }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
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

        private const string ClientType = "TCP";
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _loopSendCommandTimer = new DispatcherTimer();
        private readonly DispatcherTimer _scriptTimer = new DispatcherTimer();

        private ITransport _transport;
        private CancellationTokenSource _transportCts;
        private bool _isConnecting;

        private IEnumerator<string> _commandEnumerator;

        public TcpClientViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            //加载连接配置缓存
            var config = SettingsStore.Load<TcpConfig>(ConfigSections.FileName, ConfigSections.Tcp);
            RemoteAddress = config.Servers.Ip;
            RemotePort = config.Servers.Port;

            ConnectServerCommand = new DelegateCommand(() => _ = ConnectServerAsync());

            /////////////////////////////////////////////////////////////////////////////////

            using (var dataBase = new DataBaseConnection())
            {
                //加载扩展指令缓存
                var commandCache = dataBase.Table<ExCommandCache>()
                    .Where(x => x.ClientType == ClientType)
                    .ToList();
                ExCommandCollection = commandCache.ToObservableCollection();
            }

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

        private async Task ConnectServerAsync()
        {
            if (_isConnecting)
            {
                return;
            }

            if (_transport != null)
            {
                await _transport.DisconnectAsync();
                _transport.Dispose();
                _transport = null;
                return;
            }

            _isConnecting = true;
            _transportCts = new CancellationTokenSource();

            var transport = new TcpClientTransport(_remoteAddress, Convert.ToInt32(_remotePort));
            transport.StateChanged += delegate(TransportState state)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (state == TransportState.Connecting)
                    {
                        SetConnectionState("连接中");
                    }
                    else if (state == TransportState.Connected)
                    {
                        SetConnectionState("已连接");
                    }
                    else if (state == TransportState.Disconnected)
                    {
                        SetConnectionState("未连接");
                    }
                }));
            };
            transport.DataReceived += delegate(byte[] bytes)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTransportMessage("", bytes);
                }));
            };

            _transport = transport;

            try
            {
                await transport.ConnectAsync(_transportCts.Token);
            }
            catch (Exception e)
            {
                transport.Dispose();
                _transport = null;
                SetConnectionState("未连接");
                MessageBox.Show(e.Message, "连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private void UpdateTransportMessage(string command, byte[] bytes)
        {
            if (command.Equals(""))
            {
                //默认显示为UTF8编码
                // var log = new SocketMessage
                // {
                //     Content = bytes.ByBytesToHexString(" "),
                //     Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                //     IsSend = 0
                // };
                // Application.Current.Dispatcher.BeginInvoke(new Action(() => { Logs.Add(log); }));
            }
            else
            {
                var log = new SocketMessage
                {
                    Content = command,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    IsSend = 1
                };
                Logs.Add(log);
            }
        }

        private void SetConnectionState(string state)
        {
            ConnectionState = state;
            ButtonStateText = state == "已连接" ? "断开" : "连接";

            if (state == "连接中")
            {
                StateOuterBackgroundColor = "#66F2994A";
                StateOuterBorderColor = "#1FF2994A";
                StateInnerBackgroundColor = "#FBE1C9";
                StateInnerBorderColor = "#F2994A";
                StateTextColor = "#F2994A";
            }
            else if (state == "已连接")
            {
                StateOuterBackgroundColor = "#5917A95C";
                StateOuterBorderColor = "#1F17A95C";
                StateInnerBackgroundColor = "#BEE7D1";
                StateInnerBorderColor = "#16A34A";
                StateTextColor = "#16A34A";
            }
            else
            {
                StateOuterBackgroundColor = "#FFF7F9FC";
                StateOuterBorderColor = "#FFEEEEF0";
                StateInnerBackgroundColor = "#E7EBF0";
                StateInnerBorderColor = "#93A0AE";
                StateTextColor = "#5F6B7A";
            }
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
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
                            var logText = log.IsSend == 1
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
            // if (!_tcpClient.Connected)
            // {
            //     MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // SendMessage(_userInputText);
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
                // foreach (var log in _logs)
                // {
                //     var bytes = log.Content.ToUtf8Bytes();
                //     log.Content = bytes.ByBytesToHexString(" ");
                // }
            }
            else if (index.ToString().Equals("1"))
            {
                //转为ASCII显示
                // foreach (var log in _logs)
                // {
                //     var bytes = log.Content.Replace(" ", "").ByHexStringToBytes();
                //     log.Content = Encoding.UTF8.GetString(bytes);
                // }
            }
        }
    }
}
﻿using System.Collections.ObjectModel;
using DevKit.Cache;
using DevKit.DataService;
using DevKit.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace DevKit.ViewModels
{
    public class TcpCommunicateViewModel : BindableBase
    {
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

        private string _remotePort = string.Empty;

        public string RemotePort
        {
            set
            {
                _remotePort = value;
                RaisePropertyChanged();
            }
            get => _remotePort;
        }

        private ObservableCollection<MessageModel> _messageCollection = new ObservableCollection<MessageModel>();

        public ObservableCollection<MessageModel> MessageCollection
        {
            set
            {
                _messageCollection = value;
                RaisePropertyChanged();
            }
            get => _messageCollection;
        }

        private string _connectionStateColor = "DarkGray";

        public string ConnectionStateColor
        {
            set
            {
                _connectionStateColor = value;
                RaisePropertyChanged();
            }
            get => _connectionStateColor;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand ConnectRemoteCommand { set; get; }
        public DelegateCommand ExtensionCommand { set; get; }
        public DelegateCommand ClearMessageCommand { set; get; }
        public DelegateCommand SendMessageCommand { set; get; }

        #endregion

        private readonly IAppDataService _dataService;
        private readonly ApkConfigCache _apkConfigCache;
        private readonly TcpClientConfigCache _clientConfigCache = new TcpClientConfigCache();

        public TcpCommunicateViewModel(IAppDataService dataService)
        {
            _dataService = dataService;

            // _apkConfigCache = dataService.LoadCacheConfig();
            // if (_apkConfigCache.TcpClientCache != null)
            // {
            //     var clientCache = _apkConfigCache.TcpClientCache;
            //     // KeyFilePath = tcpClient.RemoteAddress;
            //     // KeyAlias = tcpClient.RemotePort.ToString();
            //     // KeyPassword = tcpClient.ShowHex;
            //     // ApkRootFolderPath = tcpClient.SendHex;
            //     // var extensions = tcpClient.Extension;
            // }

            // ConnectRemoteCommand = new DelegateCommand();
        }
    }
}
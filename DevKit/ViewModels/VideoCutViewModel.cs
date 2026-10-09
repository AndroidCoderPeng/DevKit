using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevKit.ViewModels
{
    public class VideoCutViewModel : BindableBase, IDialogAware
    {
        public string Title => "视频裁剪";

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

        private string _selectedVideoPath= string.Empty;

        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set => SetProperty(ref _selectedVideoPath, value);
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand SelectVideoCommand { set; get; }

        #endregion
        
        public VideoCutViewModel()
        {
            SelectVideoCommand = new DelegateCommand(() =>
            {
                var fileDialog = new OpenFileDialog
                {
                    DefaultExt = ".mp4",
                    Filter = "视频文件(*.mp4)|*.mp4"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    SelectedVideoPath = fileDialog.FileName;
                }
            });
        }
    }
}
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Utils;

namespace DevKit.ViewModels;

[SupportedOSPlatform("windows6.1")]
public partial class TransferFileDialogViewModel : ObservableObject, IDialogParameterReceiver, IDialogCloseRequester
{
    #region VM

    /// <summary>
    /// 传输方向："1" = 导出（设备 → 电脑），"0" = 导入（电脑 → 设备）
    /// </summary>
    [ObservableProperty] private string _transferMode = "1";

    [ObservableProperty] private string _deviceExportFilePath = string.Empty;

    [ObservableProperty]
    private string _computerImportFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty] private bool _isDropZoneActive;
    [ObservableProperty] private string _selectedFileName = string.Empty;
    [ObservableProperty] private string _selectedFilePath = string.Empty;
    [ObservableProperty] private string _deviceImportFilePath = "/sdcard/Download/";

    public bool HasSelectedFile => !string.IsNullOrEmpty(SelectedFileName);

    #endregion

    private string _deviceSerial = string.Empty;
    public event Action<bool> CloseRequested;

    public void OnDialogOpened(object parameter)
    {
        _deviceSerial = parameter as string ?? string.Empty;
    }

    [RelayCommand]
    private void TransferSateChanged(string value)
    {
        if (value is "1" or "0")
        {
            TransferMode = value;
        }
    }

    [RelayCommand]
    private void SelectImportFolder()
    {
        using var dialog = new FolderBrowserDialog();
        dialog.SelectedPath = ComputerImportFilePath;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ComputerImportFilePath = dialog.SelectedPath;
        }
    }

    partial void OnSelectedFileNameChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(HasSelectedFile));
    }

    [RelayCommand]
    private void SelectImportFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = @"选择要推送到设备的文件"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        SetImportFile(dialog.FileName);
    }

    public void SetImportFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        SelectedFilePath = path;

        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            SelectedFileName = $"{info.Name} · {info.Length.ToFileSize()}";
        }
        else if (Directory.Exists(path))
        {
            SelectedFileName = new DirectoryInfo(path).Name;
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        CloseRequested?.Invoke(false);
    }

    [RelayCommand]
    private async Task TransferFile(string mode)
    {
        string[] args = [];
        if (mode is "0")
        {
            // 导入文件（电脑 → 设备）
            if (string.IsNullOrEmpty(DeviceImportFilePath))
            {
                MessageBox.Show(@"请输入要导入的设备路径。", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DeviceImportFilePath))
            {
                MessageBox.Show(@"请输入设备目标路径。", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            args = ["push", SelectedFilePath, DeviceImportFilePath];
        }
        else if (mode is "1")
        {
            // 导出文件（设备 → 电脑）
            if (string.IsNullOrEmpty(DeviceExportFilePath))
            {
                MessageBox.Show(@"请输入要导出的文件路径。", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            args = ["pull", DeviceExportFilePath, ComputerImportFilePath];
        }

        if (!string.IsNullOrEmpty(_deviceSerial))
        {
            args = ["-s", _deviceSerial, .. args];
        }

        var exitCode = await RunCommandAsync(args);
        if (exitCode == 0)
        {
            var result = MessageBox.Show(@"传输成功，是否继续传输？", @"提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result != DialogResult.Yes)
            {
                CloseRequested?.Invoke(true);
            }
        }
        else
        {
            MessageBox.Show($@"传输失败：{exitCode}。", @"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 执行一条 adb 命令，逐行输出通过回调在 UI 线程处理。
    /// </summary>
    private async Task<int> RunCommandAsync(string[] args)
    {
        var argument = new ArgumentCreator();
        foreach (var arg in args)
        {
            argument.Append(Quote(arg));
        }

        var executor = new CommandExecutor(argument.ToCommandLine());
        return await executor.ExecuteAsync("adb");
    }

    /// <summary>
    /// 路径含空格时加引号，避免命令行参数被拆散
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;
}
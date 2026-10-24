using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevKit.Utils;
using Application = System.Windows.Application;

namespace DevKit.ViewModels;

[SupportedOSPlatform("windows6.1")]
public partial class TransferFileDialogViewModel : ObservableObject, IDialogCloseRequester
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
    [ObservableProperty] private bool _hasSelectedFile;
    [ObservableProperty] private string _selectedFileName = string.Empty;
    [ObservableProperty] private string _deviceImportFilePath = string.Empty;

    #endregion

    public event Action<bool> CloseRequested;

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

    [RelayCommand]
    private void CancelDialog()
    {
        CloseRequested?.Invoke(false);
    }

    [RelayCommand]
    private void TransferFile(string mode)
    {
        if (mode is "0")
        {
            // 导入文件（电脑 → 设备）
            if (string.IsNullOrEmpty(DeviceImportFilePath))
            {
                MessageBox.Show(@"请输入要导入的设备路径。", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunCommand(["push", ComputerImportFilePath, DeviceImportFilePath], line =>
            {
                Console.WriteLine(line);
            });
        }
        else if (mode is "1")
        {
            // 导出文件（设备 → 电脑）
            if (string.IsNullOrEmpty(DeviceExportFilePath))
            {
                MessageBox.Show(@"请输入要导出的文件路径。", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunCommand(["pull", DeviceExportFilePath, ComputerImportFilePath], _ => { CloseRequested?.Invoke(true); });
        }
    }

    /// <summary>
    /// 执行一条 adb 命令，逐行输出通过回调在 UI 线程处理。
    /// </summary>
    private void RunCommand(string[] args, Action<string> onLine)
    {
        var argument = new ArgumentCreator();
        foreach (var arg in args)
        {
            argument.Append(arg);
        }

        var cmd = argument.ToCommandLine();
        var executor = new CommandExecutor(cmd);
        executor.OnStandardOutput += line =>
        {
            if (string.IsNullOrEmpty(line)) return;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => onLine(line)));
        };
        executor.Execute("adb");
    }
}
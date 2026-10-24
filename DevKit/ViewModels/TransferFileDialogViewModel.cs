using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevKit.ViewModels;

public partial class TransferFileDialogViewModel : ObservableObject
{
    #region VM

    /// <summary>
    /// 传输方向："1" = 导出（设备 → 电脑），"0" = 导入（电脑 → 设备）
    /// </summary>
    [ObservableProperty] private string _transferMode = "1";

    #endregion

    public TransferFileDialogViewModel()
    {
    }

    [RelayCommand]
    private void TransferSateChanged(string value)
    {
        if (value is "1" or "0")
        {
            TransferMode = value;
        }
    }
}
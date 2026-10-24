using System.Runtime.Versioning;
using System.Windows;
using DevKit.ViewModels;

namespace DevKit.Dialogs;

[SupportedOSPlatform("windows6.1")]
public partial class TransferFileDialog : Window
{
    public TransferFileDialog()
    {
        InitializeComponent();

        FileSelectZone.PreviewMouseLeftButtonDown += (_, _) =>
            ((TransferFileDialogViewModel)DataContext).SelectImportFileCommand.Execute(null);
    }
}
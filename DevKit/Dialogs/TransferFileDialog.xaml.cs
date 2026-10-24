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

        FileDropZone.PreviewMouseLeftButtonDown += (_, _) =>
            ((TransferFileDialogViewModel)DataContext).SelectImportFileCommand.Execute(null);
        FileDropZone.DragOver += FileDropZone_DragOver;
        FileDropZone.DragLeave += (_, _) => ((TransferFileDialogViewModel)DataContext).IsDropZoneActive = false;
        FileDropZone.Drop += FileDropZone_Drop;
    }

    private void FileDropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
        ((TransferFileDialogViewModel)DataContext).IsDropZoneActive = e.Effects != DragDropEffects.None;
    }

    private void FileDropZone_Drop(object sender, DragEventArgs e)
    {
        var vm = (TransferFileDialogViewModel)DataContext;
        vm.IsDropZoneActive = false;

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            vm.SetImportFile(files[0]);
        }
    }
}
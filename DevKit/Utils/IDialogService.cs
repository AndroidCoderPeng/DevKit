namespace DevKit.Utils;

public interface IDialogService
{
    void Show(string key, object parameter = null);

    bool? ShowDialog(string key, object parameter = null);
}
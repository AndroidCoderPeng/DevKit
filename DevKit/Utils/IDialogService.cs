using System;

namespace DevKit.Utils;

public interface IDialogService
{
    void Show(Type viewType, object parameter = null);

    bool? ShowDialog(Type viewType, object parameter = null);
}
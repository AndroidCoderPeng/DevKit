using System.Windows;

namespace DevKit.Utils;

public interface IDialogService
{
    bool? ShowDialog<TView, TViewModel>(object parameter = null)
        where TView : FrameworkElement
        where TViewModel : class;
}
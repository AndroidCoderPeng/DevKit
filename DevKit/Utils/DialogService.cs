using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit.Utils;

public sealed class DialogService(IServiceProvider services) : IDialogService
{
    public bool? ShowDialog<TView, TViewModel>(object parameter = null)
        where TView : FrameworkElement
        where TViewModel : class
    {
        var view = services.GetRequiredService<TView>();
        var viewModel = services.GetRequiredService<TViewModel>();
        
        view.DataContext = viewModel;
        
        var window = new Window
        {
            Owner = Application.Current.MainWindow,
            Content = view,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        return window.ShowDialog();
    }
}
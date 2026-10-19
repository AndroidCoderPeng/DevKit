using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit.Utils;

public sealed record ViewModelRegistration(Type ViewType, Type ViewModelType);

public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, Type> _registrations;

    public DialogService(IServiceProvider services, IEnumerable<ViewModelRegistration> registrations)
    {
        _services = services;
        _registrations = registrations.ToDictionary(register => register.ViewType, register => register.ViewModelType);
    }

    public void Show(Type viewType, object parameter = null)
    {
        CreateWindow(viewType, parameter).Show();
    }

    public bool? ShowDialog(Type viewType, object parameter = null)
    {
        return CreateWindow(viewType, parameter).ShowDialog();
    }

    private Window CreateWindow(Type viewType, object parameter)
    {
        if (!_registrations.TryGetValue(viewType, out var viewModelType))
        {
            throw new ArgumentException($@"未注册 View 类型“{viewType.FullName}”。", nameof(viewType));
        }

        var view = (FrameworkElement)_services.GetRequiredService(viewType);
        var viewModel = _services.GetRequiredService(viewModelType);
        view.DataContext = viewModel;

        if (viewModel is IDialogParameterReceiver receiver)
        {
            receiver.OnDialogOpened(parameter);
        }

        return new Window
        {
            Owner = Application.Current.MainWindow,
            Content = view
        };
    }
}
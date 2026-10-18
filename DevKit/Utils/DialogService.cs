using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DevKit.Utils;

public sealed record DialogRegistration(string Key, Type ViewType, Type ViewModelType);

public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyDictionary<string, DialogRegistration> _registrations;

    public DialogService(IServiceProvider services, IEnumerable<DialogRegistration> registrations)
    {
        _services = services;
        _registrations = registrations.ToDictionary(registration => registration.Key, StringComparer.Ordinal);
    }

    public void Show(string key, object parameter = null)
    {
        CreateWindow(key).Show();
    }

    public bool? ShowDialog(string key, object parameter = null)
    {
        return CreateWindow(key).ShowDialog();
    }

    private Window CreateWindow(string key)
    {
        if (!_registrations.TryGetValue(key, out var registration))
        {
            throw new ArgumentException($@"未注册名为“{key}”的对话框。", nameof(key));
        }

        var view = (FrameworkElement)_services.GetRequiredService(registration.ViewType);
        view.DataContext = _services.GetRequiredService(registration.ViewModelType);

        return new Window
        {
            Owner = Application.Current.MainWindow,
            Content = view,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };
    }
}
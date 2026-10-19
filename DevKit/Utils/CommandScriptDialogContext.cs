using System;
using System.Collections.ObjectModel;
using DevKit.Cache;

namespace DevKit.Utils;

public sealed class CommandScriptDialogContext
{
    public CommandScriptDialogContext(ObservableCollection<ExCommandConfig> exCommands)
    {
        ExCommands = exCommands ?? throw new ArgumentNullException(nameof(exCommands));
    }

    public ObservableCollection<ExCommandConfig> ExCommands { get; }

    public ObservableCollection<ExCommandConfig> SelectedCommands { get; } = [];

    public string Interval { get; set; } = "1000";
}
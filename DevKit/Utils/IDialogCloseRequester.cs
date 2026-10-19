using System;

namespace DevKit.Utils;

public interface IDialogCloseRequester
{
    event Action<bool> CloseRequested;
}
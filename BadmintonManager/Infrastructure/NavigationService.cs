using System;
using System.Collections.Generic;

namespace BadmintonManager.Infrastructure;

public interface INavigationService
{
    string CurrentPageKey { get; }
    object? CurrentParameter { get; }
    event Action<string, object?>? Navigated;
    void NavigateTo(string pageKey, object? parameter = null);
}

public class NavigationService : INavigationService
{
    public string CurrentPageKey { get; private set; } = string.Empty;
    public object? CurrentParameter { get; private set; }

    public event Action<string, object?>? Navigated;

    public void NavigateTo(string pageKey, object? parameter = null)
    {
        CurrentPageKey = pageKey;
        CurrentParameter = parameter;
        Navigated?.Invoke(pageKey, parameter);
    }
}

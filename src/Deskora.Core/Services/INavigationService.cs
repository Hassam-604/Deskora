using Deskora.Core.Models;
using Deskora.Core.ViewModels;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for application page navigation.
/// </summary>
public interface INavigationService
{
    event EventHandler<NavigationPageKey>? CurrentPageChanged;

    NavigationPageKey CurrentPageKey { get; }
    ViewModelBase CurrentViewModel { get; }

    void RegisterPage(NavigationPageKey key, Func<ViewModelBase> factory);
    void NavigateTo(NavigationPageKey key);
    bool CanNavigateTo(NavigationPageKey key);
}

/// <summary>
/// Manages application-wide navigation state and view model transitions.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Dictionary<NavigationPageKey, Func<ViewModelBase>> _factories = new();
    private readonly Dictionary<NavigationPageKey, ViewModelBase> _cache = new();
    private NavigationPageKey _currentPageKey = NavigationPageKey.Home;
    private ViewModelBase? _currentViewModel;

    public event EventHandler<NavigationPageKey>? CurrentPageChanged;

    public NavigationPageKey CurrentPageKey => _currentPageKey;

    public ViewModelBase CurrentViewModel
    {
        get
        {
            if (_currentViewModel == null)
            {
                _currentViewModel = ResolveViewModel(_currentPageKey);
            }
            return _currentViewModel;
        }
    }

    public void RegisterPage(NavigationPageKey key, Func<ViewModelBase> factory)
    {
        _factories[key] = factory;
    }

    public bool CanNavigateTo(NavigationPageKey key)
    {
        return _factories.ContainsKey(key);
    }

    public void NavigateTo(NavigationPageKey key)
    {
        if (!_factories.ContainsKey(key))
        {
            throw new InvalidOperationException($"No ViewModel factory registered for navigation key: {key}");
        }

        if (_currentPageKey == key)
        {
            _currentViewModel ??= ResolveViewModel(key);
            return;
        }

        _currentPageKey = key;
        _currentViewModel = ResolveViewModel(key);
        CurrentPageChanged?.Invoke(this, _currentPageKey);
    }

    private ViewModelBase ResolveViewModel(NavigationPageKey key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (_factories.TryGetValue(key, out var factory))
        {
            var vm = factory();
            _cache[key] = vm;
            return vm;
        }

        throw new KeyNotFoundException($"Page key '{key}' is not registered in the navigation service.");
    }
}

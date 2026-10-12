using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private AppNotification? _currentNotification;

    [ObservableProperty]
    private string _currentThemeStatus = "Theme: System";

    [ObservableProperty]
    private string _currentStatus = "Ready";

    public ObservableCollection<NavigationItem> PrimaryNavItems { get; } = new();
    public ObservableCollection<NavigationItem> SecondaryNavItems { get; } = new();

    public string AppTitle => "Deskora Studio";
    public string AppVersion => "0.3.0";

    public MainWindowViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        IErrorService errorService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _errorService = errorService;

        Title = "Deskora";
        _currentPage = _navigationService.CurrentViewModel;
        _currentNotification = _errorService.CurrentNotification;

        InitializeNavigationItems();

        _navigationService.CurrentPageChanged += OnCurrentPageChanged;
        _themeService.ThemeChanged += OnThemeChanged;
        _errorService.NotificationChanged += OnNotificationChanged;

        UpdateThemeStatus(_themeService.CurrentTheme);
        UpdateSelectionState(_navigationService.CurrentPageKey);
    }

    private void InitializeNavigationItems()
    {
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Home, "Home", "IconHome"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Wallpapers, "Wallpapers", "IconWallpaper"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Gallery, "Gallery", "IconGallery"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.LiveWallpapers, "Live Wallpapers", "IconVideo", "0.6.0"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Icons, "Icons", "IconGrid", "0.4.0"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Layouts, "Layouts", "IconLayout", "0.5.0"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Themes, "Themes", "IconPalette", "0.8.0"));
        PrimaryNavItems.Add(new NavigationItem(NavigationPageKey.Widgets, "Widgets", "IconWidget", "0.7.0"));

        SecondaryNavItems.Add(new NavigationItem(NavigationPageKey.Settings, "Settings", "IconSettings"));
        SecondaryNavItems.Add(new NavigationItem(NavigationPageKey.About, "About", "IconAbout"));
    }

    private void OnCurrentPageChanged(object? sender, NavigationPageKey key)
    {
        CurrentPage = _navigationService.CurrentViewModel;
        UpdateSelectionState(key);
    }

    private void OnThemeChanged(object? sender, ThemePreference theme)
    {
        UpdateThemeStatus(theme);
    }

    private void OnNotificationChanged(object? sender, AppNotification? notification)
    {
        CurrentNotification = notification;
    }

    private void UpdateThemeStatus(ThemePreference theme)
    {
        CurrentThemeStatus = theme switch
        {
            ThemePreference.Light => "Theme: Light",
            ThemePreference.Dark => "Theme: Dark",
            _ => "Theme: System Default"
        };
    }

    private void UpdateSelectionState(NavigationPageKey activeKey)
    {
        foreach (var item in PrimaryNavItems)
        {
            item.IsSelected = item.Key == activeKey;
        }

        foreach (var item in SecondaryNavItems)
        {
            item.IsSelected = item.Key == activeKey;
        }
    }

    [RelayCommand]
    public void Navigate(NavigationPageKey key)
    {
        _navigationService.NavigateTo(key);
    }

    [RelayCommand]
    public void DismissNotification()
    {
        _errorService.ClearNotification();
    }
}

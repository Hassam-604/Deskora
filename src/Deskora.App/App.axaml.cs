using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Deskora.App.Views;
using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;

namespace Deskora.App;

public partial class App : Application
{
    private IThemeService? _themeService;
    private IErrorService? _errorService;
    private INavigationService? _navigationService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize Core services
            _themeService = new ThemeService();
            _errorService = new ErrorService();
            _navigationService = new NavigationService();

            // Wire theme changes to Avalonia's RequestedThemeVariant
            _themeService.ThemeChanged += (sender, preference) =>
            {
                RequestedThemeVariant = preference switch
                {
                    ThemePreference.Light => ThemeVariant.Light,
                    ThemePreference.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            };

            // Register navigation pages
            RegisterNavigationPages(_navigationService, _themeService, _errorService);

            // Construct main ViewModel
            var mainViewModel = new MainWindowViewModel(
                _navigationService,
                _themeService,
                _errorService
            );

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterNavigationPages(
        INavigationService navigationService,
        IThemeService themeService,
        IErrorService errorService)
    {
        navigationService.RegisterPage(NavigationPageKey.Home, () => new HomeViewModel(navigationService));
        navigationService.RegisterPage(NavigationPageKey.Wallpapers, () => new WallpapersViewModel());
        navigationService.RegisterPage(NavigationPageKey.LiveWallpapers, () => new LiveWallpapersViewModel());
        navigationService.RegisterPage(NavigationPageKey.Icons, () => new IconsViewModel());
        navigationService.RegisterPage(NavigationPageKey.Layouts, () => new LayoutsViewModel());
        navigationService.RegisterPage(NavigationPageKey.Themes, () => new ThemesViewModel());
        navigationService.RegisterPage(NavigationPageKey.Widgets, () => new WidgetsViewModel());
        navigationService.RegisterPage(NavigationPageKey.Settings, () => new SettingsViewModel(themeService, errorService));
        navigationService.RegisterPage(NavigationPageKey.About, () => new AboutViewModel());
    }
}
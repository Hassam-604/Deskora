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
    private IWallpaperService? _wallpaperService;
    private IWallpaperPreferencesService? _wallpaperPreferencesService;
    private IFilePickerService? _filePickerService;
    private IGalleryProvider? _galleryProvider;
    private IDownloadService? _downloadService;

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

            // Initialize Wallpaper & Storage services
            _wallpaperService = new Services.WindowsWallpaperService();
            _wallpaperPreferencesService = new JsonWallpaperPreferencesService();
            _filePickerService = new Services.AvaloniaFilePickerService();
            _galleryProvider = new OnlineGalleryProvider(new JsonCatalogProvider());
            _downloadService = new HttpDownloadService();

            // Initialize Icon services
            var iconPreferencesService = new JsonIconPreferencesService();
            var iconService = new Services.WindowsIconService(iconPreferencesService);
            IIconLibraryService iconLibraryService = OperatingSystem.IsWindows() ? new Services.WindowsIconLibraryService() : null!;

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
            RegisterNavigationPages(
                _navigationService,
                _themeService,
                _errorService,
                _wallpaperService,
                _wallpaperPreferencesService,
                _filePickerService,
                _galleryProvider,
                _downloadService,
                iconService,
                iconPreferencesService,
                iconLibraryService
            );

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
        IErrorService errorService,
        IWallpaperService wallpaperService,
        IWallpaperPreferencesService wallpaperPreferencesService,
        IFilePickerService filePickerService,
        IGalleryProvider galleryProvider,
        IDownloadService downloadService,
        IIconService iconService,
        IIconPreferencesService iconPreferencesService,
        IIconLibraryService iconLibraryService)
    {
        navigationService.RegisterPage(NavigationPageKey.Home, () => new HomeViewModel(navigationService));
        navigationService.RegisterPage(NavigationPageKey.Wallpapers, () => new WallpapersViewModel(
            wallpaperService,
            wallpaperPreferencesService,
            filePickerService,
            errorService
        ));
        navigationService.RegisterPage(NavigationPageKey.Gallery, () =>
        {
            var vm = new GalleryViewModel(galleryProvider, downloadService, errorService, wallpaperService);
            _ = vm.InitializeAsync();
            return vm;
        });
        navigationService.RegisterPage(NavigationPageKey.LiveWallpapers, () => new LiveWallpapersViewModel());
        navigationService.RegisterPage(NavigationPageKey.Icons, () =>
        {
            var vm = new IconsViewModel(iconService, iconPreferencesService, filePickerService, errorService, iconLibraryService);
            _ = vm.InitializeAsync();
            return vm;
        });
        navigationService.RegisterPage(NavigationPageKey.Layouts, () => new LayoutsViewModel());
        navigationService.RegisterPage(NavigationPageKey.Themes, () => new ThemesViewModel());
        navigationService.RegisterPage(NavigationPageKey.Widgets, () => new WidgetsViewModel());
        navigationService.RegisterPage(NavigationPageKey.Settings, () => new SettingsViewModel(themeService, errorService));
        navigationService.RegisterPage(NavigationPageKey.About, () => new AboutViewModel());
    }
}


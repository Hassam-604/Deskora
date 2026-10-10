using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Xunit;

namespace Deskora.Tests;

public class MainWindowViewModelTests
{
    private class DummyHomeViewModel : ViewModelBase
    {
        public DummyHomeViewModel() => Title = "Home";
    }

    private class DummyWallpapersViewModel : ViewModelBase
    {
        public DummyWallpapersViewModel() => Title = "Wallpapers";
    }

    [Fact]
    public void MainWindowViewModel_InitializesNavigationAndDefaults()
    {
        var nav = new NavigationService();
        var theme = new ThemeService();
        var error = new ErrorService();

        nav.RegisterPage(NavigationPageKey.Home, () => new DummyHomeViewModel());
        nav.RegisterPage(NavigationPageKey.Wallpapers, () => new DummyWallpapersViewModel());

        var vm = new MainWindowViewModel(nav, theme, error);

        Assert.NotEmpty(vm.PrimaryNavItems);
        Assert.NotEmpty(vm.SecondaryNavItems);
        Assert.NotNull(vm.CurrentPage);
        Assert.Equal("Home", vm.CurrentPage.Title);
        Assert.Equal("Theme: System Default", vm.CurrentThemeStatus);
        Assert.Equal("Ready", vm.CurrentStatus);
    }

    [Fact]
    public void NavigateCommand_SwitchesCurrentPage_AndUpdatesSelection()
    {
        var nav = new NavigationService();
        var theme = new ThemeService();
        var error = new ErrorService();

        nav.RegisterPage(NavigationPageKey.Home, () => new DummyHomeViewModel());
        nav.RegisterPage(NavigationPageKey.Wallpapers, () => new DummyWallpapersViewModel());

        var vm = new MainWindowViewModel(nav, theme, error);

        vm.NavigateCommand.Execute(NavigationPageKey.Wallpapers);

        Assert.Equal("Wallpapers", vm.CurrentPage.Title);

        var wallpapersItem = vm.PrimaryNavItems.First(i => i.Key == NavigationPageKey.Wallpapers);
        var homeItem = vm.PrimaryNavItems.First(i => i.Key == NavigationPageKey.Home);

        Assert.True(wallpapersItem.IsSelected);
        Assert.False(homeItem.IsSelected);
    }

    [Fact]
    public void DismissNotificationCommand_ClearsActiveAlert()
    {
        var nav = new NavigationService();
        var theme = new ThemeService();
        var error = new ErrorService();

        nav.RegisterPage(NavigationPageKey.Home, () => new DummyHomeViewModel());

        var vm = new MainWindowViewModel(nav, theme, error);

        error.Notify("Test alert");
        Assert.NotNull(vm.CurrentNotification);

        vm.DismissNotificationCommand.Execute(null);
        Assert.Null(vm.CurrentNotification);
    }
}

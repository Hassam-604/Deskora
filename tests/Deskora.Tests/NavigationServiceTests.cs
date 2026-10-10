using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Xunit;

namespace Deskora.Tests;

public class NavigationServiceTests
{
    private class DummyViewModel : ViewModelBase { }
    private class AlternateViewModel : ViewModelBase { }

    [Fact]
    public void InitialPage_IsHome_ByDefault()
    {
        var nav = new NavigationService();
        nav.RegisterPage(NavigationPageKey.Home, () => new DummyViewModel());

        Assert.Equal(NavigationPageKey.Home, nav.CurrentPageKey);
        Assert.NotNull(nav.CurrentViewModel);
        Assert.IsType<DummyViewModel>(nav.CurrentViewModel);
    }

    [Fact]
    public void NavigateTo_SwitchesCurrentViewModel_AndFiresEvent()
    {
        var nav = new NavigationService();
        nav.RegisterPage(NavigationPageKey.Home, () => new DummyViewModel());
        nav.RegisterPage(NavigationPageKey.Wallpapers, () => new AlternateViewModel());

        NavigationPageKey? receivedKey = null;
        nav.CurrentPageChanged += (sender, key) => receivedKey = key;

        nav.NavigateTo(NavigationPageKey.Wallpapers);

        Assert.Equal(NavigationPageKey.Wallpapers, nav.CurrentPageKey);
        Assert.Equal(NavigationPageKey.Wallpapers, receivedKey);
        Assert.IsType<AlternateViewModel>(nav.CurrentViewModel);
    }

    [Fact]
    public void NavigateTo_SamePage_DoesNotFireRedundantEvent()
    {
        var nav = new NavigationService();
        nav.RegisterPage(NavigationPageKey.Home, () => new DummyViewModel());

        int eventCount = 0;
        nav.CurrentPageChanged += (sender, key) => eventCount++;

        nav.NavigateTo(NavigationPageKey.Home);

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void NavigateTo_UnregisteredKey_ThrowsInvalidOperationException()
    {
        var nav = new NavigationService();
        Assert.Throws<InvalidOperationException>(() => nav.NavigateTo(NavigationPageKey.About));
    }

    [Fact]
    public void CanNavigateTo_ReturnsCorrectRegistrationStatus()
    {
        var nav = new NavigationService();
        nav.RegisterPage(NavigationPageKey.Home, () => new DummyViewModel());

        Assert.True(nav.CanNavigateTo(NavigationPageKey.Home));
        Assert.False(nav.CanNavigateTo(NavigationPageKey.Settings));
    }
}

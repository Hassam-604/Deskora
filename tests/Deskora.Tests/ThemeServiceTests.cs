using Deskora.Core.Models;
using Deskora.Core.Services;
using Xunit;

namespace Deskora.Tests;

public class ThemeServiceTests
{
    [Fact]
    public void InitialTheme_IsSystem_ByDefault()
    {
        var service = new ThemeService();
        Assert.Equal(ThemePreference.System, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_UpdatesCurrentTheme_AndFiresEvent()
    {
        var service = new ThemeService();
        ThemePreference? receivedTheme = null;
        service.ThemeChanged += (sender, theme) => receivedTheme = theme;

        service.SetTheme(ThemePreference.Dark);

        Assert.Equal(ThemePreference.Dark, service.CurrentTheme);
        Assert.Equal(ThemePreference.Dark, receivedTheme);
    }

    [Fact]
    public void SetTheme_SameTheme_DoesNotFireEvent()
    {
        var service = new ThemeService();
        int eventCount = 0;
        service.ThemeChanged += (sender, theme) => eventCount++;

        service.SetTheme(ThemePreference.System);

        Assert.Equal(0, eventCount);
    }
}

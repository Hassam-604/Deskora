using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Xunit;

namespace Deskora.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void ThemeCommands_ChangeSelectedTheme_AndPropagateToThemeService()
    {
        var theme = new ThemeService();
        var error = new ErrorService();
        var vm = new SettingsViewModel(theme, error);

        vm.SetDarkThemeCommand.Execute(null);
        Assert.Equal(ThemePreference.Dark, vm.SelectedTheme);
        Assert.Equal(ThemePreference.Dark, theme.CurrentTheme);

        vm.SetLightThemeCommand.Execute(null);
        Assert.Equal(ThemePreference.Light, vm.SelectedTheme);
        Assert.Equal(ThemePreference.Light, theme.CurrentTheme);

        vm.SetSystemThemeCommand.Execute(null);
        Assert.Equal(ThemePreference.System, vm.SelectedTheme);
        Assert.Equal(ThemePreference.System, theme.CurrentTheme);
    }

    [Fact]
    public void DiagnosticCommands_TriggerNotifications()
    {
        var theme = new ThemeService();
        var error = new ErrorService();
        var vm = new SettingsViewModel(theme, error);

        vm.TriggerTestNotificationCommand.Execute(null);
        Assert.NotNull(error.CurrentNotification);
        Assert.Equal(NotificationSeverity.Info, error.CurrentNotification.Severity);

        vm.TriggerTestWarningCommand.Execute(null);
        Assert.NotNull(error.CurrentNotification);
        Assert.Equal(NotificationSeverity.Warning, error.CurrentNotification.Severity);

        vm.TriggerTestErrorCommand.Execute(null);
        Assert.NotNull(error.CurrentNotification);
        Assert.Equal(NotificationSeverity.Error, error.CurrentNotification.Severity);
    }
}

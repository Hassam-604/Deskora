using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private ThemePreference _selectedTheme;

    [ObservableProperty]
    private bool _reducedMotion;

    public string AppVersion => "0.1.0-alpha";
    public string DotNetVersion => ".NET 8.0 (LTS)";
    public string AvaloniaVersion => "Avalonia UI 11.2.5";
    public string Architecture => "x64 Desktop Architecture";

    public SettingsViewModel(IThemeService themeService, IErrorService errorService)
    {
        _themeService = themeService;
        _errorService = errorService;
        Title = "Settings";

        _selectedTheme = _themeService.CurrentTheme;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, ThemePreference newTheme)
    {
        SelectedTheme = newTheme;
    }

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        _themeService.SetTheme(value);
    }

    [RelayCommand]
    private void SetSystemTheme() => SelectedTheme = ThemePreference.System;

    [RelayCommand]
    private void SetLightTheme() => SelectedTheme = ThemePreference.Light;

    [RelayCommand]
    private void SetDarkTheme() => SelectedTheme = ThemePreference.Dark;

    [RelayCommand]
    private void TriggerTestNotification()
    {
        _errorService.Notify("This is a test notification message confirming the alert bar works.", NotificationSeverity.Info);
    }

    [RelayCommand]
    private void TriggerTestWarning()
    {
        _errorService.Notify("Warning test: Memory usage optimization threshold reached (Sample).", NotificationSeverity.Warning);
    }

    [RelayCommand]
    private void TriggerTestError()
    {
        _errorService.ReportError("Sample diagnostic error: Simulated file access or system permission issue.");
    }
}

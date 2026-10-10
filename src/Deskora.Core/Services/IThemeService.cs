using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for managing application theme/appearance preferences.
/// </summary>
public interface IThemeService
{
    event EventHandler<ThemePreference>? ThemeChanged;
    ThemePreference CurrentTheme { get; }
    void SetTheme(ThemePreference theme);
}

/// <summary>
/// In-memory implementation of theme preferences.
/// </summary>
public class ThemeService : IThemeService
{
    private ThemePreference _currentTheme = ThemePreference.System;

    public event EventHandler<ThemePreference>? ThemeChanged;

    public ThemePreference CurrentTheme => _currentTheme;

    public void SetTheme(ThemePreference theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        _currentTheme = theme;
        ThemeChanged?.Invoke(this, _currentTheme);
    }
}

using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for persisting and retrieving user wallpaper preferences across sessions.
/// </summary>
public interface IWallpaperPreferencesService
{
    Task<WallpaperPreferences> LoadPreferencesAsync();
    Task SavePreferencesAsync(WallpaperPreferences preferences);
}

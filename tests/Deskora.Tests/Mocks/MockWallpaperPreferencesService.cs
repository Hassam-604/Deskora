using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockWallpaperPreferencesService : IWallpaperPreferencesService
{
    public WallpaperPreferences Preferences { get; set; } = new();
    public int SaveCallCount { get; private set; }
    public int LoadCallCount { get; private set; }

    public Task<WallpaperPreferences> LoadPreferencesAsync()
    {
        LoadCallCount++;
        return Task.FromResult(Preferences);
    }

    public Task SavePreferencesAsync(WallpaperPreferences preferences)
    {
        SaveCallCount++;
        Preferences = preferences;
        return Task.CompletedTask;
    }
}

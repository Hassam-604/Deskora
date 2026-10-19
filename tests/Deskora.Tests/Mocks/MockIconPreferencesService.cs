using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockIconPreferencesService : IIconPreferencesService
{
    public IconPreferences Preferences { get; set; } = new();
    public int LoadCallCount { get; private set; }
    public int SaveCallCount { get; private set; }

    public Task<IconPreferences> LoadPreferencesAsync(CancellationToken ct = default)
    {
        LoadCallCount++;
        return Task.FromResult(Preferences);
    }

    public Task SavePreferencesAsync(IconPreferences preferences, CancellationToken ct = default)
    {
        SaveCallCount++;
        Preferences = preferences;
        return Task.CompletedTask;
    }
}

using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for persisting and retrieving customized icon states and snapshots.
/// </summary>
public interface IIconPreferencesService
{
    Task<IconPreferences> LoadPreferencesAsync(CancellationToken ct = default);
    Task SavePreferencesAsync(IconPreferences preferences, CancellationToken ct = default);
}

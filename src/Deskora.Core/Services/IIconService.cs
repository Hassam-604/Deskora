using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for discovering, inspecting, applying, and safely reverting Windows desktop icons.
/// </summary>
public interface IIconService
{
    /// <summary>
    /// Scans the desktop for all customizable items (shortcuts .lnk and desktop folders).
    /// </summary>
    Task<IReadOnlyList<DesktopIconItem>> GetDesktopIconsAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies a validated custom icon (.ico or resource container) to the target shortcut or folder.
    /// Captures a baseline snapshot beforehand if one does not already exist.
    /// </summary>
    Task<IconApplyResult> ApplyCustomIconAsync(DesktopIconItem item, string iconPath, int iconIndex = 0, CancellationToken ct = default);

    /// <summary>
    /// Reverts the specified item to its original baseline state recorded in the snapshot.
    /// </summary>
    Task<IconApplyResult> RestoreOriginalIconAsync(DesktopIconItem item, CancellationToken ct = default);

    /// <summary>
    /// Reverts all previously customized desktop items back to their baseline snapshots.
    /// </summary>
    Task<IconApplyResult> ResetAllToDefaultsAsync(CancellationToken ct = default);

    /// <summary>
    /// Signals Windows Explorer (via SHChangeNotify) to refresh the desktop icon cache without restarting Explorer.
    /// </summary>
    Task RefreshShellCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// Extracts an icon thumbnail (as a platform bitmap object) for display in the UI.
    /// </summary>
    object? ExtractThumbnail(string path, bool isFolder = false);
}

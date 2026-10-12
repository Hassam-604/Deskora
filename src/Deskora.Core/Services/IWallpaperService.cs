using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for querying display monitors and applying/restoring desktop wallpapers.
/// Platform-specific implementations (e.g. Windows COM / IDesktopWallpaper) encapsulate all native Win32 calls.
/// </summary>
public interface IWallpaperService
{
    /// <summary>Indicates whether wallpaper manipulation is supported on this platform/session.</summary>
    bool IsSupported { get; }

    /// <summary>Enumerates active display monitors.</summary>
    Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync();

    /// <summary>Captures a full snapshot of current wallpaper paths and positions before any modifications occur.</summary>
    Task<WallpaperSnapshot> CaptureCurrentSnapshotAsync();

    /// <summary>
    /// Applies a wallpaper image file to desktop.
    /// </summary>
    /// <param name="filePath">Absolute path to validated image file.</param>
    /// <param name="fitMode">Desired wallpaper fit/position.</param>
    /// <param name="monitorId">Optional monitor ID. When null, applies to all monitors.</param>
    Task<WallpaperApplyResult> ApplyWallpaperAsync(string filePath, WallpaperFitMode fitMode, string? monitorId = null);

    /// <summary>
    /// Reverts desktop wallpapers to a previously captured snapshot.
    /// </summary>
    Task<WallpaperApplyResult> RestoreWallpaperAsync(WallpaperSnapshot snapshot);

    /// <summary>Gets the current wallpaper path for a given monitor or primary display.</summary>
    Task<string?> GetCurrentWallpaperPathAsync(string? monitorId = null);
}

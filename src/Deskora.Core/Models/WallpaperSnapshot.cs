namespace Deskora.Core.Models;

/// <summary>
/// A recorded state snapshot of desktop wallpapers across monitors prior to modifications.
/// Enables 1-click restore/undo.
/// </summary>
public record WallpaperSnapshot(
    DateTime Timestamp,
    string? GlobalWallpaperPath,
    Dictionary<string, string> MonitorWallpapers,
    WallpaperFitMode Position
)
{
    public string Summary => !string.IsNullOrEmpty(GlobalWallpaperPath)
        ? Path.GetFileName(GlobalWallpaperPath)
        : $"{MonitorWallpapers.Count} monitor(s) configured";
}

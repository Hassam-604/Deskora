namespace Deskora.Core.Models;

/// <summary>
/// User preferences and library state for local wallpapers.
/// Persisted locally to restore state between application launches.
/// </summary>
public class WallpaperPreferences
{
    public string? SelectedFolder { get; set; }
    public List<string> CustomImagePaths { get; set; } = new();
    public List<string> FavoritePaths { get; set; } = new();
    public List<string> RecentWallpaperPaths { get; set; } = new();
    public List<string> ExcludedPaths { get; set; } = new();
    public WallpaperFitMode LastFitMode { get; set; } = WallpaperFitMode.Fill;
    public WallpaperSnapshot? LastSnapshot { get; set; }
}

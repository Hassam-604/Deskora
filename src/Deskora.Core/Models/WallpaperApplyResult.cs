namespace Deskora.Core.Models;

/// <summary>
/// Result of an attempt to apply or restore a desktop wallpaper.
/// </summary>
public record WallpaperApplyResult(
    bool Success,
    string Message,
    string? AppliedPath = null,
    WallpaperFitMode? FitMode = null,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.Now : Timestamp;
}

namespace Deskora.Core.Models;

/// <summary>
/// Represents a curated or online web icon ready for download, conversion, and desktop customization.
/// </summary>
public sealed class WebIconItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "Apps";
    public string Description { get; init; } = string.Empty;
    public string Source { get; init; } = "Curated";
    public string? DownloadUrl { get; init; }
    public string? LocalCachedPath { get; set; }
    public object? ThumbnailBitmap { get; set; }
}

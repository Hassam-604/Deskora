namespace Deskora.Core.Models;

/// <summary>
/// Category classification for gallery entries.
/// </summary>
public enum GalleryCategory
{
    All,
    Nature,
    Space,
    Abstract,
    Architecture,
    Minimal,
    Anime
}

/// <summary>
/// Current download/availability state of a gallery entry.
/// </summary>
public enum DownloadState
{
    /// <summary>Not yet downloaded; available in catalog.</summary>
    Available,
    /// <summary>Download in progress.</summary>
    Downloading,
    /// <summary>Download completed and asset is on disk.</summary>
    Downloaded,
    /// <summary>Download failed with an error.</summary>
    Failed,
    /// <summary>Download was cancelled by the user.</summary>
    Cancelled
}

/// <summary>
/// An immutable record representing a single curated, approved asset in the Deskora gallery.
/// All downloadable URLs must be explicitly listed here and must originate from approved sources.
/// No web scraping, no arbitrary URL construction, no untrusted content.
/// </summary>
public sealed class GalleryEntry
{
    /// <summary>Stable unique identifier for this entry in the catalog.</summary>
    public required string Id { get; init; }

    /// <summary>Display title shown in the gallery card.</summary>
    public required string Title { get; init; }

    /// <summary>Credit/attribution for the original author or photographer.</summary>
    public required string Author { get; init; }

    /// <summary>Source platform or website (e.g., "Unsplash", "NASA").</summary>
    public required string Source { get; init; }

    /// <summary>License name (e.g., "Unsplash License", "CC0 Public Domain").</summary>
    public required string License { get; init; }

    /// <summary>HTTPS URL to download the full-resolution image. Must be pre-approved.</summary>
    public required string DownloadUrl { get; init; }

    /// <summary>HTTPS URL to a lower-resolution preview thumbnail for gallery display.</summary>
    public required string PreviewUrl { get; init; }

    /// <summary>Expected SHA-256 hash of the downloaded file for integrity verification.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Image file extension including dot, e.g. ".jpg".</summary>
    public required string FileExtension { get; init; }

    /// <summary>Expected full-resolution width in pixels.</summary>
    public int Width { get; init; }

    /// <summary>Expected full-resolution height in pixels.</summary>
    public int Height { get; init; }

    /// <summary>Known approximate file size in bytes (0 if unknown).</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>Gallery category for filtering.</summary>
    public GalleryCategory Category { get; init; }

    /// <summary>Human-readable description shown in the detail/preview panel.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Tags for search/filter.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    // ── Computed convenience properties ──────────────────────────────────
    public string ResolutionLabel => (Width > 0 && Height > 0) ? $"{Width} × {Height}" : "Unknown";

    public string FileSizeLabel => FileSizeBytes > 0
        ? FileSizeBytes >= 1_048_576
            ? $"{FileSizeBytes / 1_048_576.0:F1} MB"
            : $"{FileSizeBytes / 1024.0:F0} KB"
        : "Unknown";

    public string LocalFileName => $"{Id}{FileExtension}";

    public bool IsLandscape => Width >= Height;
    public string AspectRatioLabel => (Width > 0 && Height > 0)
        ? Gcd(Width, Height) is int g
            ? $"{Width / g}:{Height / g}"
            : "?"
        : "?";

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}

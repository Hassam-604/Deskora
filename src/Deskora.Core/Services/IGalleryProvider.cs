using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Provides the curated catalog of approved downloadable gallery assets.
/// Implementations must only return entries whose download URLs are explicitly approved.
/// No dynamic URL construction. No web scraping. No untrusted content.
/// </summary>
public interface IGalleryProvider
{
    /// <summary>Load and return all entries in the curated catalog.</summary>
    Task<IReadOnlyList<GalleryEntry>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>Search or browse catalog entries with optional category filtering and pagination.</summary>
    Task<IReadOnlyList<GalleryEntry>> SearchCatalogAsync(
        string query = "",
        GalleryCategory category = GalleryCategory.All,
        int page = 1,
        CancellationToken ct = default);

    /// <summary>Returns catalog version/source metadata for display and debugging.</summary>
    string CatalogSource { get; }
}

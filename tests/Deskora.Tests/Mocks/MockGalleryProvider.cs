using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockGalleryProvider : IGalleryProvider
{
    public List<GalleryEntry> Entries { get; } = new();
    public string CatalogSource { get; set; } = "Mock Curated Catalog";
    public bool ShouldThrow { get; set; }

    public Task<IReadOnlyList<GalleryEntry>> GetCatalogAsync(CancellationToken ct = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated catalog failure");
        }

        return Task.FromResult<IReadOnlyList<GalleryEntry>>(Entries.ToList());
    }

    public Task<IReadOnlyList<GalleryEntry>> SearchCatalogAsync(
        string query = "",
        GalleryCategory category = GalleryCategory.All,
        int page = 1,
        CancellationToken ct = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated search failure");
        }

        var filtered = Entries.AsEnumerable();

        if (category != GalleryCategory.All)
        {
            filtered = filtered.Where(e => e.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            filtered = filtered.Where(e =>
                e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Author.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult<IReadOnlyList<GalleryEntry>>(filtered.ToList());
    }
}

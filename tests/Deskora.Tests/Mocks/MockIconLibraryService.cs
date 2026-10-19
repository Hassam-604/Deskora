using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockIconLibraryService : IIconLibraryService
{
    public List<SystemIconItem> SystemIconsToReturn { get; } = new();
    public List<WebIconItem> CuratedIconsToReturn { get; } = new();

    public Task<IReadOnlyList<SystemIconItem>> GetSystemIconsAsync(string? category = null, CancellationToken ct = default)
    {
        var items = SystemIconsToReturn.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            items = items.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<SystemIconItem>>(items.ToList());
    }

    public Task<IReadOnlyList<WebIconItem>> GetCuratedIconsAsync(string? category = null, CancellationToken ct = default)
    {
        var items = CuratedIconsToReturn.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            items = items.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<WebIconItem>>(items.ToList());
    }

    public Task<IReadOnlyList<WebIconItem>> SearchIconsAsync(string query, CancellationToken ct = default)
    {
        var items = CuratedIconsToReturn.Where(i =>
            i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            i.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<WebIconItem>>(items);
    }

    public Task<string?> PrepareWebIconAsync(WebIconItem item, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(item.LocalCachedPath ?? "C:\\mock_icon.ico");
    }
}

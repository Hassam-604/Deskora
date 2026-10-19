using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for discovering and preparing built-in Windows system icons and web/curated icons.
/// </summary>
public interface IIconLibraryService
{
    /// <summary>
    /// Retrieves native Windows icons from system libraries (imageres.dll, shell32.dll, ddores.dll).
    /// </summary>
    Task<IReadOnlyList<SystemIconItem>> GetSystemIconsAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves curated high-quality desktop icons (apps, brands, aesthetic folders).
    /// </summary>
    Task<IReadOnlyList<WebIconItem>> GetCuratedIconsAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// Searches online icons or filtered curated catalog.
    /// </summary>
    Task<IReadOnlyList<WebIconItem>> SearchIconsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Ensures the web/curated icon is downloaded and converted to a valid local .ico file ready to apply.
    /// </summary>
    Task<string?> PrepareWebIconAsync(WebIconItem item, CancellationToken ct = default);
}

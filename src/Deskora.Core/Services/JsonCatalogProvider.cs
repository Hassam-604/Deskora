using System.Text.Json;
using System.Text.Json.Serialization;
using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Loads and validates curated catalog entries from JSON data.
/// Supports loading from a file path, stream, or embedded/default fallback.
/// </summary>
public sealed class JsonCatalogProvider : IGalleryProvider
{
    private readonly string? _catalogFilePath;
    private readonly string? _directJson;
    private string _catalogSource = "Deskora Curated Catalog";

    public string CatalogSource => _catalogSource;

    public JsonCatalogProvider(string? catalogFilePath = null, string? directJson = null)
    {
        _catalogFilePath = catalogFilePath;
        _directJson = directJson;
    }

    public async Task<IReadOnlyList<GalleryEntry>> GetCatalogAsync(CancellationToken ct = default)
    {
        string? json = _directJson;

        if (json is null && _catalogFilePath != null && File.Exists(_catalogFilePath))
        {
            json = await File.ReadAllTextAsync(_catalogFilePath, ct);
        }

        if (json is null)
        {
            // Try standard relative paths from app execution base
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Gallery", "catalog.json"),
                Path.Combine(AppContext.BaseDirectory, "catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "Deskora.App", "Gallery", "catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Gallery", "catalog.json")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    json = await File.ReadAllTextAsync(path, ct);
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<GalleryEntry>();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        CatalogDto? root;
        try
        {
            root = JsonSerializer.Deserialize<CatalogDto>(json, options);
        }
        catch (JsonException)
        {
            return Array.Empty<GalleryEntry>();
        }

        if (root is null)
        {
            return Array.Empty<GalleryEntry>();
        }

        if (!string.IsNullOrWhiteSpace(root.CatalogSource))
        {
            _catalogSource = root.CatalogSource;
        }

        var result = new List<GalleryEntry>();
        if (root.Entries != null)
        {
            foreach (var dto in root.Entries)
            {
                if (string.IsNullOrWhiteSpace(dto.Id) ||
                    string.IsNullOrWhiteSpace(dto.Title) ||
                    string.IsNullOrWhiteSpace(dto.DownloadUrl))
                {
                    continue;
                }

                // Security: must be HTTPS
                if (!dto.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var category = GalleryCategory.All;
                if (!string.IsNullOrWhiteSpace(dto.Category) &&
                    Enum.TryParse<GalleryCategory>(dto.Category, ignoreCase: true, out var parsedCategory))
                {
                    category = parsedCategory;
                }

                var ext = !string.IsNullOrWhiteSpace(dto.FileExtension)
                    ? (dto.FileExtension.StartsWith('.') ? dto.FileExtension : "." + dto.FileExtension)
                    : ".jpg";

                result.Add(new GalleryEntry
                {
                    Id = dto.Id.Trim(),
                    Title = dto.Title.Trim(),
                    Author = dto.Author ?? "Unknown",
                    Source = dto.Source ?? "Curated Catalog",
                    License = dto.License ?? "Open License",
                    DownloadUrl = dto.DownloadUrl.Trim(),
                    PreviewUrl = !string.IsNullOrWhiteSpace(dto.PreviewUrl) ? dto.PreviewUrl.Trim() : dto.DownloadUrl.Trim(),
                    Sha256 = dto.Sha256 ?? string.Empty,
                    FileExtension = ext,
                    Width = dto.Width,
                    Height = dto.Height,
                    FileSizeBytes = dto.FileSizeBytes,
                    Category = category,
                    Description = dto.Description ?? string.Empty,
                    Tags = dto.Tags ?? Array.Empty<string>()
                });
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<GalleryEntry>> SearchCatalogAsync(
        string query = "",
        GalleryCategory category = GalleryCategory.All,
        int page = 1,
        CancellationToken ct = default)
    {
        var all = await GetCatalogAsync(ct);
        var filtered = all.AsEnumerable();

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

        return filtered.ToList();
    }

    private sealed class CatalogDto
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("catalog_source")]
        public string? CatalogSource { get; set; }

        [JsonPropertyName("entries")]
        public List<EntryDto>? Entries { get; set; }
    }

    private sealed class EntryDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }

        [JsonPropertyName("file_extension")]
        public string? FileExtension { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("file_size_bytes")]
        public long FileSizeBytes { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }
}

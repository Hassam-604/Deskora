using System.Text.Json;
using System.Text.Json.Serialization;
using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Hybrid multi-source online gallery provider combining:
/// 1. Wallhaven 4K Public Library (high-res 4K desktop wallpapers: nature, abstract, cyberpunk, anime, minimal, architecture)
/// 2. Nekos.best Anime Art Archive (vetted high-resolution illustrations with artist attribution)
/// Features concurrent search across providers, deduplication, and graceful offline fallback.
/// </summary>
public sealed class OnlineGalleryProvider : IGalleryProvider, IDisposable
{
    private static readonly string[] NatureTitles = new[]
    {
        "Alpine Wilderness", "Emerald Forest Canopy", "Misty Mountain Ridge", "Golden Sunset Shores",
        "Cascading River Valley", "Highland Morning Light", "Autumn Foliage Harmony", "Tranquil Lake Reflection",
        "Nordic Fjords Vista", "Pine Valley Whispers", "Glacial Horizon", "Sunlit Meadow Path"
    };

    private static readonly string[] SpaceTitles = new[]
    {
        "Cosmic Nebula Dream", "Milky Way Galaxy Arc", "Deep Stellar Horizon", "Supernova Radiance",
        "Orbital Atmosphere", "Interstellar Voyage", "Celestial Starlight", "Aurora Borealis Night",
        "Galactic Dust Cloud", "Planetary Eclipse", "Infinite Cosmos", "Andromeda Drift"
    };

    private static readonly string[] AbstractTitles = new[]
    {
        "Prismatic Flow", "Neon Fluid Dynamics", "Velvet Spectrum", "Liquid Chromatics",
        "Geometric Monolith", "Vivid Color Fusion", "Minimalist Vector Wave", "Dark Ambient Waves",
        "Holographic Prism", "Ethereal Gradients", "Fractal Symmetry", "Chromatic Dispersion"
    };

    private static readonly string[] ArchitectureTitles = new[]
    {
        "Metropolis Skyline", "Brutalist Monolith", "Midnight Cityscape", "Modernist Glass Facade",
        "Urban Architecture", "Tokyo Night Horizons", "Illuminated Bridge Vista", "Steel & Concrete",
        "Architectural Angles", "Downtown Twilight", "Gothic Cathedral Spire", "Futuristic Cityway"
    };

    private static readonly string[] MinimalTitles = new[]
    {
        "Zen Monochrome", "Solitary Horizon", "Clean Architecture", "Quiet Gradient",
        "Minimal Shadowplay", "Pure Stillness", "Modern Geometry", "Subtle Horizon",
        "Dark Minimalist Plane", "Calm Aesthetic Space", "Soft Contrast", "Neutral Tone Sanctuary"
    };

    private static readonly string[] AnimeTitles = new[]
    {
        "Starlight Fantasy", "Neo Tokyo Twilight", "Cherry Blossom Breeze", "Ethereal Anime Horizon",
        "Midnight City Lights", "Sunset Reverie", "Celestial Dreamscape", "Moonlit Sakura",
        "Cyberpunk Alleyways", "Quiet Rain Anime Mood", "Anime Serenity", "Golden Hour Wanderer"
    };

    private static readonly string[] GeneralTitles = new[]
    {
        "Cinematic Vista", "Studio Composition", "Atmospheric Scene", "High-Fidelity Perspective",
        "Epic Desktop Canvas", "Dramatic Lighting Art", "Vibrant Horizon", "Fine Art Masterpiece"
    };

    private readonly IGalleryProvider _fallbackProvider;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private string _catalogSource = "Online Wallpaper Library (Wallhaven • Nekos Art)";

    public string CatalogSource => _catalogSource;

    public OnlineGalleryProvider(IGalleryProvider fallbackProvider, HttpClient? httpClient = null)
    {
        _fallbackProvider = fallbackProvider;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Deskora/0.3.0");
            }
            catch
            {
                // Ignore fallback
            }
            _ownsHttpClient = true;
        }
    }

    public async Task<IReadOnlyList<GalleryEntry>> GetCatalogAsync(CancellationToken ct = default)
    {
        return await SearchCatalogAsync(query: "", category: GalleryCategory.All, page: 1, ct: ct);
    }

    public async Task<IReadOnlyList<GalleryEntry>> SearchCatalogAsync(
        string query = "",
        GalleryCategory category = GalleryCategory.All,
        int page = 1,
        CancellationToken ct = default)
    {
        var tasks = new List<Task<List<GalleryEntry>>>();

        // 1. Wallhaven (General, Anime, People, Nature, Abstract, Space, Architecture, Minimal)
        tasks.Add(FetchWallhavenAsync(query, category, page, ct));

        // 2. Nekos.best (Vetted High-Resolution Anime & Character Artwork)
        if (category == GalleryCategory.All || category == GalleryCategory.Anime)
        {
            tasks.Add(FetchNekosAsync(query, category, page, ct));
        }

        try
        {
            var resultsArrays = await Task.WhenAll(tasks);
            var combined = new List<GalleryEntry>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Interleave results from providers to provide diverse wallpaper browsing
            int maxCount = resultsArrays.Max(arr => arr.Count);
            for (int i = 0; i < maxCount; i++)
            {
                foreach (var arr in resultsArrays)
                {
                    if (i < arr.Count)
                    {
                        var entry = arr[i];
                        if (seenIds.Add(entry.Id))
                        {
                            combined.Add(entry);
                        }
                    }
                }
            }

            if (combined.Count > 0)
            {
                var sources = new List<string>();
                if (combined.Any(e => e.Id.StartsWith("wh_"))) sources.Add("Wallhaven 4K");
                if (combined.Any(e => e.Id.StartsWith("nekos_"))) sources.Add("Nekos Art");

                _catalogSource = sources.Count > 0
                    ? $"{string.Join(" • ", sources)} (Online)"
                    : "Online Wallpaper Library (Online)";

                return combined;
            }
        }
        catch
        {
            // Network failure: fall through to offline catalog
        }

        _catalogSource = $"{_fallbackProvider.CatalogSource} (Offline Mode)";
        return await _fallbackProvider.SearchCatalogAsync(query, category, page, ct);
    }

    // ── Wallhaven Provider ──────────────────────────────────────────────────

    private async Task<List<GalleryEntry>> FetchWallhavenAsync(
        string query,
        GalleryCategory category,
        int page,
        CancellationToken ct)
    {
        try
        {
            var url = BuildWallhavenApiUrl(query, category, page);
            using var response = await _httpClient.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                return ParseWallhavenJson(json, category, query);
            }
        }
        catch
        {
            // Ignore single provider failure
        }

        return new List<GalleryEntry>();
    }

    private static string BuildWallhavenApiUrl(string query, GalleryCategory category, int page)
    {
        var queryParts = new List<string> { "purity=100" }; // strictly SFW

        string catCode = category switch
        {
            GalleryCategory.Anime => "010",
            _ => "110"
        };
        queryParts.Add($"categories={catCode}");

        var searchTerms = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            searchTerms.Add(query.Trim());
        }

        switch (category)
        {
            case GalleryCategory.Nature:
                searchTerms.Add("nature");
                break;
            case GalleryCategory.Space:
                searchTerms.Add("space");
                break;
            case GalleryCategory.Abstract:
                searchTerms.Add("abstract");
                break;
            case GalleryCategory.Architecture:
                searchTerms.Add("architecture");
                break;
            case GalleryCategory.Minimal:
                searchTerms.Add("minimal");
                break;
        }

        // High-fidelity desktop aspect ratios (16:9 and 16:10)
        queryParts.Add("ratios=16x9,16x10");
        queryParts.Add("resolutions=1920x1080,2560x1440,3840x2160");

        if (searchTerms.Count > 0)
        {
            var combinedQuery = Uri.EscapeDataString(string.Join(" ", searchTerms));
            queryParts.Add($"q={combinedQuery}");
            queryParts.Add("sorting=relevance");
        }
        else
        {
            queryParts.Add("sorting=toplist");
            queryParts.Add("topRange=1M");
        }

        if (page > 1)
        {
            queryParts.Add($"page={page}");
        }

        return "https://wallhaven.cc/api/v1/search?" + string.Join("&", queryParts);
    }

    private static List<GalleryEntry> ParseWallhavenJson(string json, GalleryCategory defaultCategory, string query)
    {
        var entries = new List<GalleryEntry>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var root = JsonSerializer.Deserialize<WallhavenResponseDto>(json, options);
            if (root?.Data == null) return entries;

            foreach (var item in root.Data)
            {
                if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Path))
                    continue;

                if (!item.Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    continue;

                var ext = item.FileType == "image/png" ? ".png" : ".jpg";
                var preview = item.Thumbs?.Large ?? item.Thumbs?.Small ?? item.Path;

                var cat = defaultCategory != GalleryCategory.All
                    ? defaultCategory
                    : (item.Category?.Equals("anime", StringComparison.OrdinalIgnoreCase) == true
                        ? GalleryCategory.Anime
                        : GalleryCategory.All);

                var title = GenerateDescriptiveTitle(item.Id, cat, query);

                var tagsList = new List<string> { "4K", "desktop", "wallpaper", "wallhaven" };
                if (!string.IsNullOrWhiteSpace(query))
                {
                    tagsList.AddRange(query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
                if (cat != GalleryCategory.All)
                {
                    tagsList.Add(cat.ToString().ToLowerInvariant());
                }

                entries.Add(new GalleryEntry
                {
                    Id = $"wh_{item.Id}",
                    Title = title,
                    Author = !string.IsNullOrWhiteSpace(item.Source) ? "Featured Artist" : "Wallhaven Community",
                    Source = "Wallhaven Public Gallery",
                    License = "Free for Personal Use (SFW)",
                    DownloadUrl = item.Path,
                    PreviewUrl = preview,
                    Sha256 = string.Empty,
                    FileExtension = ext,
                    Width = item.DimensionX,
                    Height = item.DimensionY,
                    FileSizeBytes = item.FileSize,
                    Category = cat,
                    Description = $"{item.Resolution} resolution ({item.Ratio} aspect ratio). Ultra high-definition desktop artwork.",
                    Tags = tagsList
                });
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return entries;
    }

    // ── Nekos.best Provider (Anime Artwork) ─────────────────────────────────

    private async Task<List<GalleryEntry>> FetchNekosAsync(
        string query,
        GalleryCategory category,
        int page,
        CancellationToken ct)
    {
        try
        {
            string searchTerm = !string.IsNullOrWhiteSpace(query) ? query.Trim() : "anime";
            var escaped = Uri.EscapeDataString(searchTerm);
            var url = $"https://nekos.best/api/v2/search?query={escaped}&type=1&amount=12";

            using var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                return ParseNekosJson(json, query);
            }
        }
        catch
        {
            // Ignore single provider failure
        }

        return new List<GalleryEntry>();
    }

    private static List<GalleryEntry> ParseNekosJson(string json, string query)
    {
        var entries = new List<GalleryEntry>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var root = JsonSerializer.Deserialize<NekosResponseDto>(json, options);
            if (root?.Results == null) return entries;

            int index = 1;
            foreach (var item in root.Results)
            {
                if (string.IsNullOrWhiteSpace(item.Url) || !item.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    continue;

                string rawId = Path.GetFileNameWithoutExtension(item.Url);
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    rawId = $"item_{index}";
                }

                var title = !string.IsNullOrWhiteSpace(query)
                    ? $"{char.ToUpperInvariant(query[0]) + (query.Length > 1 ? query[1..].ToLowerInvariant() : "")} — Anime Artwork #{index}"
                    : (!string.IsNullOrWhiteSpace(item.ArtistName) ? $"{item.ArtistName}'s Illustration" : "Anime Artwork Collection");

                var ext = item.Url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
                int w = item.Dimensions?.Width ?? 1920;
                int h = item.Dimensions?.Height ?? 1080;

                entries.Add(new GalleryEntry
                {
                    Id = $"nekos_{SanitizeId(rawId)}",
                    Title = title,
                    Author = !string.IsNullOrWhiteSpace(item.ArtistName) ? item.ArtistName : "Pixiv Artist",
                    Source = "Nekos.best Anime Archive",
                    License = "Free for Personal Use (Credit to Artist)",
                    DownloadUrl = item.Url,
                    PreviewUrl = item.Url,
                    Sha256 = string.Empty,
                    FileExtension = ext,
                    Width = w,
                    Height = h,
                    FileSizeBytes = 2000000,
                    Category = GalleryCategory.Anime,
                    Description = $"Vetted high-resolution anime illustration. Original source: {item.SourceUrl ?? item.ArtistHref ?? "Pixiv/Twitter"}",
                    Tags = new List<string> { "Anime", "Illustration", "Artwork", "Desktop" }
                });

                index++;
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return entries;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string SanitizeId(string id)
    {
        return new string(id.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string GenerateDescriptiveTitle(string id, GalleryCategory cat, string query)
    {
        uint hash = string.IsNullOrEmpty(id) ? 0u : (uint)id.GetHashCode();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Select(w => char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..].ToLowerInvariant() : ""));
            var topic = string.Join(" ", words);

            if (!string.IsNullOrWhiteSpace(topic))
            {
                var descriptors = new[] { "Artwork", "Edition", "4K Wallpaper", "Perspective", "Visuals", "Theme", "Atmosphere", "Horizon" };
                var desc = descriptors[hash % (uint)descriptors.Length];
                return $"{topic} — {desc}";
            }
        }

        var list = cat switch
        {
            GalleryCategory.Nature => NatureTitles,
            GalleryCategory.Space => SpaceTitles,
            GalleryCategory.Abstract => AbstractTitles,
            GalleryCategory.Architecture => ArchitectureTitles,
            GalleryCategory.Minimal => MinimalTitles,
            GalleryCategory.Anime => AnimeTitles,
            _ => GeneralTitles
        };

        var index = (int)(hash % (uint)list.Length);
        return list[index];
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed class WallhavenResponseDto
    {
        [JsonPropertyName("data")]
        public List<WallhavenItemDto>? Data { get; set; }
    }

    private sealed class WallhavenItemDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("dimension_x")]
        public int DimensionX { get; set; }

        [JsonPropertyName("dimension_y")]
        public int DimensionY { get; set; }

        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; }

        [JsonPropertyName("ratio")]
        public string? Ratio { get; set; }

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("file_type")]
        public string? FileType { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("thumbs")]
        public WallhavenThumbsDto? Thumbs { get; set; }
    }

    private sealed class WallhavenThumbsDto
    {
        [JsonPropertyName("large")]
        public string? Large { get; set; }

        [JsonPropertyName("small")]
        public string? Small { get; set; }
    }

    private sealed class NekosResponseDto
    {
        [JsonPropertyName("results")]
        public List<NekosResultDto>? Results { get; set; }
    }

    private sealed class NekosResultDto
    {
        [JsonPropertyName("artist_name")]
        public string? ArtistName { get; set; }

        [JsonPropertyName("artist_href")]
        public string? ArtistHref { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("dimensions")]
        public NekosDimensionsDto? Dimensions { get; set; }
    }

    private sealed class NekosDimensionsDto
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}

using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests;

public class JsonCatalogProviderTests
{
    private const string SampleValidJson = """
    {
      "version": "0.3.0",
      "catalog_source": "Deskora Curated Test Catalog",
      "entries": [
        {
          "id": "nature_01",
          "title": "Misty Forest",
          "author": "Jane Doe",
          "source": "Unsplash",
          "license": "Unsplash License",
          "download_url": "https://example.com/forest.jpg",
          "preview_url": "https://example.com/forest_thumb.jpg",
          "sha256": "abc123def456",
          "file_extension": ".jpg",
          "width": 3840,
          "height": 2160,
          "file_size_bytes": 4500000,
          "category": "Nature",
          "description": "A tranquil morning in the forest.",
          "tags": ["nature", "trees", "mist"]
        },
        {
          "id": "space_01",
          "title": "Deep Cosmos",
          "author": "NASA",
          "source": "NASA",
          "license": "CC0",
          "download_url": "https://example.com/space.png",
          "preview_url": "https://example.com/space_thumb.png",
          "sha256": "",
          "file_extension": "png",
          "width": 2560,
          "height": 1440,
          "file_size_bytes": 3200000,
          "category": "Space",
          "description": "Distant galaxies.",
          "tags": ["space", "stars"]
        }
      ]
    }
    """;

    [Fact]
    public async Task GetCatalogAsync_ParsesValidJson_ReturnsEntries()
    {
        var provider = new JsonCatalogProvider(directJson: SampleValidJson);
        var entries = await provider.GetCatalogAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal("Deskora Curated Test Catalog", provider.CatalogSource);

        var first = entries[0];
        Assert.Equal("nature_01", first.Id);
        Assert.Equal("Misty Forest", first.Title);
        Assert.Equal("Jane Doe", first.Author);
        Assert.Equal(GalleryCategory.Nature, first.Category);
        Assert.Equal(".jpg", first.FileExtension);
        Assert.Equal(3840, first.Width);
        Assert.Equal(2160, first.Height);
        Assert.Equal("3840 × 2160", first.ResolutionLabel);
        Assert.Equal("nature_01.jpg", first.LocalFileName);
        Assert.Contains("nature", first.Tags);
    }

    [Fact]
    public async Task GetCatalogAsync_RejectsNonHttpsDownloadUrls()
    {
        var invalidJson = """
        {
          "catalog_source": "Insecure Test",
          "entries": [
            {
              "id": "insecure_01",
              "title": "Insecure Entry",
              "download_url": "http://insecure.example.com/test.jpg"
            },
            {
              "id": "secure_01",
              "title": "Secure Entry",
              "download_url": "https://secure.example.com/test.jpg"
            }
          ]
        }
        """;

        var provider = new JsonCatalogProvider(directJson: invalidJson);
        var entries = await provider.GetCatalogAsync();

        Assert.Single(entries);
        Assert.Equal("secure_01", entries[0].Id);
    }

    [Fact]
    public async Task GetCatalogAsync_EmptyOrInvalidJson_ReturnsEmptyListWithoutCrashing()
    {
        var provider = new JsonCatalogProvider(directJson: "{ broken json }");
        var entries = await provider.GetCatalogAsync();

        Assert.Empty(entries);
    }

    [Fact]
    public async Task GetCatalogAsync_NullJson_ReturnsEmptyList()
    {
        var provider = new JsonCatalogProvider(directJson: "");
        var entries = await provider.GetCatalogAsync();

        Assert.Empty(entries);
    }
}

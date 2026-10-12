using System.Net;
using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Tests.Mocks;

namespace Deskora.Tests;

public class OnlineGalleryProviderTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task SearchCatalogAsync_WhenOnline_ParsesWallhavenJsonSuccessfully()
    {
        var sampleJson = """
        {
          "data": [
            {
              "id": "abc123",
              "dimension_x": 3840,
              "dimension_y": 2160,
              "resolution": "3840x2160",
              "ratio": "1.78",
              "file_size": 4500000,
              "file_type": "image/jpeg",
              "category": "general",
              "source": "Artist Studio",
              "path": "https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg",
              "thumbs": {
                "large": "https://th.wallhaven.cc/lg/ab/abc123.jpg",
                "small": "https://th.wallhaven.cc/small/ab/abc123.jpg"
              }
            }
          ],
          "meta": {
            "current_page": 1,
            "last_page": 50,
            "total": 1200
          }
        }
        """;

        var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sampleJson)
        });

        using var httpClient = new HttpClient(handler);
        var fallback = new MockGalleryProvider();
        using var provider = new OnlineGalleryProvider(fallback, httpClient);

        var results = await provider.SearchCatalogAsync(query: "nature", category: GalleryCategory.Nature);

        Assert.Single(results);
        var item = results[0];
        Assert.Equal("wh_abc123", item.Id);
        Assert.Equal(3840, item.Width);
        Assert.Equal(2160, item.Height);
        Assert.Equal("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg", item.DownloadUrl);
        Assert.Equal("https://th.wallhaven.cc/lg/ab/abc123.jpg", item.PreviewUrl);
        Assert.Equal(".jpg", item.FileExtension);
        Assert.Contains("Online", provider.CatalogSource);
    }

    [Fact]
    public async Task SearchCatalogAsync_WhenNetworkFails_FallsBackToOfflineCatalog()
    {
        var handler = new FakeHttpMessageHandler(req => throw new HttpRequestException("Network down"));
        using var httpClient = new HttpClient(handler);

        var fallback = new MockGalleryProvider();
        fallback.Entries.Add(new GalleryEntry
        {
            Id = "offline_entry_1",
            Title = "Offline Forest",
            Author = "Local Photographer",
            Source = "Local Catalog",
            License = "CC0",
            DownloadUrl = "https://example.com/forest.jpg",
            PreviewUrl = "https://example.com/thumb.jpg",
            Sha256 = "",
            FileExtension = ".jpg",
            Category = GalleryCategory.Nature
        });

        using var provider = new OnlineGalleryProvider(fallback, httpClient);

        var results = await provider.SearchCatalogAsync(category: GalleryCategory.Nature);

        Assert.Single(results);
        Assert.Equal("offline_entry_1", results[0].Id);
        Assert.Contains("Offline Mode", provider.CatalogSource);
    }

    [Fact]
    public async Task SearchCatalogAsync_WhenNekosReturnsResults_ParsesSuccessfully()
    {
        var nekosJson = """
        {
          "results": [
            {
              "artist_name": "StudioManga",
              "artist_href": "https://pixiv.net/users/123",
              "source_url": "https://pixiv.net/artworks/456",
              "url": "https://nekos.best/api/v2/neko/test_uuid.png",
              "dimensions": { "width": 2560, "height": 1440 }
            }
          ]
        }
        """;

        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri?.Host.Contains("nekos.best") == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(nekosJson) };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        var fallback = new MockGalleryProvider();
        using var provider = new OnlineGalleryProvider(fallback, httpClient);

        var results = await provider.SearchCatalogAsync(query: "cyberpunk", category: GalleryCategory.Anime);

        Assert.Single(results);
        Assert.Equal("nekos_testuuid", results[0].Id);
        Assert.Equal("StudioManga", results[0].Author);
        Assert.Equal("Nekos.best Anime Archive", results[0].Source);
        Assert.Equal("https://nekos.best/api/v2/neko/test_uuid.png", results[0].DownloadUrl);
        Assert.Equal(".png", results[0].FileExtension);
        Assert.Contains("Nekos Art", provider.CatalogSource);
    }
}


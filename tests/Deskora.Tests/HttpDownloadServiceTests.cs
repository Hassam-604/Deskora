using System.Net;
using System.Security.Cryptography;
using System.Text;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests;

public class HttpDownloadServiceTests : IDisposable
{
    private readonly string _testDir;

    public HttpDownloadServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DeskoraTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore temp dir cleanup
        }
    }

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
    public async Task DownloadEntryAsync_RejectsNonHttpsUrl()
    {
        using var service = new HttpDownloadService(_testDir);
        var entry = new GalleryEntry
        {
            Id = "test1",
            Title = "Insecure Test",
            Author = "Author",
            Source = "Source",
            License = "License",
            DownloadUrl = "http://insecure.example.com/image.jpg",
            PreviewUrl = "https://preview.com",
            Sha256 = "",
            FileExtension = ".jpg"
        };

        var result = await service.DownloadEntryAsync(entry, new Progress<double>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("HTTPS", result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadEntryAsync_RejectsDisallowedFileExtension()
    {
        using var service = new HttpDownloadService(_testDir);
        var entry = new GalleryEntry
        {
            Id = "test_script",
            Title = "Dangerous Script",
            Author = "Author",
            Source = "Source",
            License = "License",
            DownloadUrl = "https://example.com/malicious.exe",
            PreviewUrl = "https://preview.com",
            Sha256 = "",
            FileExtension = ".exe"
        };

        var result = await service.DownloadEntryAsync(entry, new Progress<double>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Disallowed file extension", result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadEntryAsync_ValidDownloadWithSha256_SucceedsAndSavesFile()
    {
        var sampleBytes = Encoding.UTF8.GetBytes("Deskora Wallpaper Image Sample Content");
        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(sampleBytes));

        var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(sampleBytes)
        });

        using var httpClient = new HttpClient(handler);
        using var service = new HttpDownloadService(_testDir, httpClient);

        var entry = new GalleryEntry
        {
            Id = "wall_01",
            Title = "Mountain Peak",
            Author = "Artist",
            Source = "Source",
            License = "License",
            DownloadUrl = "https://example.com/mountain.jpg",
            PreviewUrl = "https://preview.com",
            Sha256 = expectedHash,
            FileExtension = ".jpg"
        };

        var reportedProgress = new List<double>();
        var progress = new Progress<double>(p => reportedProgress.Add(p));

        var result = await service.DownloadEntryAsync(entry, progress, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.LocalPath);
        Assert.True(File.Exists(result.LocalPath));
        Assert.Equal(sampleBytes.Length, new FileInfo(result.LocalPath).Length);

        // Verify GetLocalPath
        var storedPath = service.GetLocalPath(entry);
        Assert.Equal(result.LocalPath, storedPath);

        // Verify storage used
        Assert.True(service.GetStorageUsedBytes() > 0);

        // Verify delete
        var deleted = service.DeleteLocalAsset(entry);
        Assert.True(deleted);
        Assert.Null(service.GetLocalPath(entry));
    }

    [Fact]
    public async Task DownloadEntryAsync_CorruptedHash_CleansUpAndReturnsCorrupted()
    {
        var sampleBytes = Encoding.UTF8.GetBytes("Legitimate Image Content");
        var incorrectHash = "1111222233334444555566667777888899990000aaaabbbbccccddddeeeeffff";

        var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(sampleBytes)
        });

        using var httpClient = new HttpClient(handler);
        using var service = new HttpDownloadService(_testDir, httpClient);

        var entry = new GalleryEntry
        {
            Id = "wall_corrupted",
            Title = "Corrupted Image",
            Author = "Artist",
            Source = "Source",
            License = "License",
            DownloadUrl = "https://example.com/image.jpg",
            PreviewUrl = "https://preview.com",
            Sha256 = incorrectHash,
            FileExtension = ".jpg"
        };

        var result = await service.DownloadEntryAsync(entry, new Progress<double>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.WasCorrupted);
        Assert.Null(service.GetLocalPath(entry));
        // Verify no leftover tmp files
        Assert.Empty(Directory.GetFiles(_testDir, "*.tmp"));
    }

    [Fact]
    public async Task DownloadEntryAsync_CancellationRequested_CleansUpAndReturnsWasCancelled()
    {
        var sampleBytes = new byte[1000];
        var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(sampleBytes)
        });

        using var httpClient = new HttpClient(handler);
        using var service = new HttpDownloadService(_testDir, httpClient);

        var entry = new GalleryEntry
        {
            Id = "wall_cancelled",
            Title = "Cancelled Asset",
            Author = "Artist",
            Source = "Source",
            License = "License",
            DownloadUrl = "https://example.com/asset.jpg",
            PreviewUrl = "https://preview.com",
            Sha256 = "",
            FileExtension = ".jpg"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var result = await service.DownloadEntryAsync(entry, new Progress<double>(), cts.Token);

        Assert.False(result.Success);
        Assert.True(result.WasCancelled);
        Assert.Null(service.GetLocalPath(entry));
        Assert.Empty(Directory.GetFiles(_testDir, "*.tmp"));
    }
}

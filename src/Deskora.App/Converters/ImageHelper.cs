using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Deskora.App.Converters;

/// <summary>
/// Attached property for Avalonia Image controls that provides non-blocking,
/// cached asynchronous image loading from local files or remote HTTPS URLs.
/// </summary>
public static class ImageHelper
{
    private static readonly ConcurrentDictionary<string, Bitmap> MemoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HttpClient HttpClient = new();
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deskora",
        "Cache",
        "Thumbnails");

    public static readonly AttachedProperty<string?> SourceUrlProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>("SourceUrl", typeof(ImageHelper));

    public static readonly AttachedProperty<int> DecodeWidthProperty =
        AvaloniaProperty.RegisterAttached<Image, int>("DecodeWidth", typeof(ImageHelper), 400);

    static ImageHelper()
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Deskora/0.3.0");
            HttpClient.Timeout = TimeSpan.FromSeconds(20);
        }
        catch
        {
            // Ignore init failure
        }

        SourceUrlProperty.Changed.AddClassHandler<Image>(OnSourceUrlChanged);
    }

    public static string? GetSourceUrl(Image element) => element.GetValue(SourceUrlProperty);
    public static void SetSourceUrl(Image element, string? value) => element.SetValue(SourceUrlProperty, value);

    public static int GetDecodeWidth(Image element) => element.GetValue(DecodeWidthProperty);
    public static void SetDecodeWidth(Image element, int value) => element.SetValue(DecodeWidthProperty, value);

    private static void OnSourceUrlChanged(Image image, AvaloniaPropertyChangedEventArgs args)
    {
        var url = args.NewValue as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            image.Source = null;
            return;
        }

        int targetWidth = GetDecodeWidth(image);
        if (targetWidth <= 0) targetWidth = 400;

        string cacheKey = $"{url}_{targetWidth}";

        // 1. Check memory cache first (instant synchronous render)
        if (MemoryCache.TryGetValue(cacheKey, out var cached))
        {
            image.Source = cached;
            return;
        }

        // 2. Clear previous image while loading new one
        image.Source = null;

        // 3. Load asynchronously
        _ = LoadImageAsync(image, url, targetWidth, cacheKey);
    }

    private static async Task LoadImageAsync(Image image, string url, int targetWidth, string cacheKey)
    {
        try
        {
            Bitmap? bitmap = null;

            // Case A: Local file
            if (File.Exists(url))
            {
                await using var stream = File.OpenRead(url);
                bitmap = Bitmap.DecodeToWidth(stream, targetWidth);
            }
            // Case B: Remote HTTPS URL
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                     url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                var diskPath = GetDiskCachePath(url);

                if (!File.Exists(diskPath))
                {
                    var bytes = await HttpClient.GetByteArrayAsync(url);
                    var tmp = $"{diskPath}.tmp";
                    await File.WriteAllBytesAsync(tmp, bytes);
                    File.Move(tmp, diskPath, overwrite: true);
                }

                if (File.Exists(diskPath))
                {
                    await using var stream = File.OpenRead(diskPath);
                    bitmap = Bitmap.DecodeToWidth(stream, targetWidth);
                }
            }

            if (bitmap != null)
            {
                MemoryCache[cacheKey] = bitmap;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Verify the image still wants this URL (avoid race condition on recycled items)
                    if (GetSourceUrl(image) == url)
                    {
                        image.Source = bitmap;
                    }
                });
            }
        }
        catch
        {
            // Silently swallow network / decoding failures
        }
    }

    private static string GetDiskCachePath(string url)
    {
        using var md5 = MD5.Create();
        var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(url)));
        return Path.Combine(CacheDirectory, $"{hash}.thumb");
    }
}

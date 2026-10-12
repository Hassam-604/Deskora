using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Deskora.App.Converters;

/// <summary>
/// Converts either a local file path or a remote HTTPS URL to an Avalonia Bitmap with 2-tier caching:
/// 1. In-memory ConcurrentDictionary
/// 2. Persistent disk cache at %LocalAppData%/Deskora/Cache/Thumbnails
/// </summary>
public class AsyncImageConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap?> MemoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PendingDownloads = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deskora",
        "Cache",
        "Thumbnails");

    static AsyncImageConverter()
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
        }
        catch
        {
            // Ignore directory creation failure
        }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string input || string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        int targetWidth = 400;
        if (parameter is string paramStr && int.TryParse(paramStr, out int customWidth) && customWidth > 0)
        {
            targetWidth = customWidth;
        }

        string memoryKey = $"{input}_{targetWidth}";

        // 1. Check memory cache first
        if (MemoryCache.TryGetValue(memoryKey, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        // 2. Check if local file exists
        if (File.Exists(input))
        {
            try
            {
                using var stream = File.OpenRead(input);
                var bmp = Bitmap.DecodeToWidth(stream, targetWidth);
                MemoryCache[memoryKey] = bmp;
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        // 3. Handle remote HTTPS URL
        if (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            var diskCacheFile = GetDiskCachePath(input);
            if (File.Exists(diskCacheFile))
            {
                try
                {
                    using var stream = File.OpenRead(diskCacheFile);
                    var bmp = Bitmap.DecodeToWidth(stream, targetWidth);
                    MemoryCache[memoryKey] = bmp;
                    return bmp;
                }
                catch
                {
                    // If corrupted, remove disk cache file
                    try { File.Delete(diskCacheFile); } catch { }
                }
            }

            // Not yet downloaded to disk: initiate background fetch
            lock (PendingDownloads)
            {
                if (!PendingDownloads.Contains(input))
                {
                    PendingDownloads.Add(input);
                    _ = FetchAndCacheAsync(input, diskCacheFile);
                }
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string GetDiskCachePath(string url)
    {
        using var md5 = MD5.Create();
        var hash = System.Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(url)));
        return Path.Combine(CacheDirectory, $"{hash}.thumb");
    }

    private static async Task FetchAndCacheAsync(string url, string diskCachePath)
    {
        try
        {
            var data = await HttpClient.GetByteArrayAsync(url);
            var temp = $"{diskCachePath}.tmp";
            await File.WriteAllBytesAsync(temp, data);
            File.Move(temp, diskCachePath, overwrite: true);
        }
        catch
        {
            // Offline or failed download - silent fallback
        }
        finally
        {
            lock (PendingDownloads)
            {
                PendingDownloads.Remove(url);
            }
        }
    }
}

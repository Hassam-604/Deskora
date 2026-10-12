using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Deskora.App.Converters;

/// <summary>
/// Converts a local image file path to a decoded Avalonia Bitmap with memory caching.
/// Downscales thumbnails to 400px width to keep memory consumption low.
/// </summary>
public class FilePathToBitmapConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        int targetWidth = 400;
        if (parameter is string paramStr && int.TryParse(paramStr, out int customWidth) && customWidth > 0)
        {
            targetWidth = customWidth;
        }

        string cacheKey = $"{path}_{targetWidth}";

        return Cache.GetOrAdd(cacheKey, _ =>
        {
            try
            {
                using var stream = File.OpenRead(path);
                return Bitmap.DecodeToWidth(stream, targetWidth);
            }
            catch
            {
                return null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

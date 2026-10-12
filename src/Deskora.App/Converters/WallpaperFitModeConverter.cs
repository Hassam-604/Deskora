using System.Globalization;
using Avalonia.Data.Converters;
using Deskora.Core.Models;

namespace Deskora.App.Converters;

/// <summary>
/// Converts a WallpaperFitMode enum value into a user-friendly display string.
/// </summary>
public class WallpaperFitModeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is WallpaperFitMode mode)
        {
            return mode switch
            {
                WallpaperFitMode.Fill => "Fill (Standard Zoom)",
                WallpaperFitMode.Fit => "Fit (Letterbox)",
                WallpaperFitMode.Stretch => "Stretch (Full Frame)",
                WallpaperFitMode.Center => "Center (Original 1:1)",
                WallpaperFitMode.Tile => "Tile (Pattern)",
                WallpaperFitMode.Span => "Span (Across Displays)",
                _ => mode.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

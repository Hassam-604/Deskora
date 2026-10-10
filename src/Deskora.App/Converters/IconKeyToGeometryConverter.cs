using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Deskora.App.Converters;

/// <summary>
/// Resolves a resource key string (e.g. "IconHome") to its corresponding StreamGeometry resource.
/// </summary>
public class IconKeyToGeometryConverter : IValueConverter
{
    public static readonly IconKeyToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current != null)
        {
            if (Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var res) && res is Geometry geom)
            {
                return geom;
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

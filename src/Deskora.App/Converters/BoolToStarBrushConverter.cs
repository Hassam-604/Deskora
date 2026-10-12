using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Deskora.App.Converters;

/// <summary>
/// Converts a boolean favorite state to a vibrant gold brush when favorited
/// or a subtle semi-transparent white brush when unfavorited.
/// </summary>
public class BoolToStarBrushConverter : IValueConverter
{
    private static readonly IBrush GoldBrush = new SolidColorBrush(Color.Parse("#FBBF24"));
    private static readonly IBrush DefaultInactiveBrush = new SolidColorBrush(Color.Parse("#9CA3AF"));
    private static readonly IBrush CardOverlayInactiveBrush = new SolidColorBrush(Color.Parse("#CBD5E1"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return GoldBrush;
        }

        if (parameter is string p && p.Equals("card", StringComparison.OrdinalIgnoreCase))
        {
            return CardOverlayInactiveBrush;
        }

        return DefaultInactiveBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

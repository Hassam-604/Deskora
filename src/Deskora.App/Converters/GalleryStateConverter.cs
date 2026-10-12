using System.Globalization;
using Avalonia.Data.Converters;
using Deskora.Core.Models;

namespace Deskora.App.Converters;

/// <summary>
/// Multi-purpose converter for DownloadState checks in Avalonia XAML bindings.
/// Parameter specifies target condition:
/// - "IsAvailable": returns true if state is Available or Cancelled
/// - "IsDownloading": returns true if state is Downloading
/// - "IsDownloaded": returns true if state is Downloaded
/// - "IsFailed": returns true if state is Failed
/// </summary>
public class GalleryStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DownloadState state)
        {
            return parameter?.ToString() == "IsAvailable";
        }

        return parameter?.ToString() switch
        {
            "IsAvailable" => state == DownloadState.Available || state == DownloadState.Cancelled,
            "IsDownloading" => state == DownloadState.Downloading,
            "IsDownloaded" => state == DownloadState.Downloaded,
            "IsFailed" => state == DownloadState.Failed,
            "StateLabel" => state switch
            {
                DownloadState.Downloaded => "Downloaded",
                DownloadState.Downloading => "Downloading...",
                DownloadState.Failed => "Download Failed",
                DownloadState.Cancelled => "Cancelled",
                _ => "Available"
            },
            _ => false
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

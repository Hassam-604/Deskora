namespace Deskora.Core.Models;

/// <summary>
/// Supported desktop wallpaper fit and layout positions.
/// Maps to Windows DESKTOP_WALLPAPER_POSITION enumeration.
/// </summary>
public enum WallpaperFitMode
{
    /// <summary>Crops or scales the image to completely fill the monitor without distortion.</summary>
    Fill = 0,

    /// <summary>Scales the image to fit entirely within the display, maintaining aspect ratio (letterboxing if needed).</summary>
    Fit = 1,

    /// <summary>Stretches the image to fill the display, ignoring aspect ratio.</summary>
    Stretch = 2,

    /// <summary>Repeats the image across the display in a tile pattern.</summary>
    Tile = 3,

    /// <summary>Centers the image on the display at its original pixel dimensions.</summary>
    Center = 4,

    /// <summary>Spans a single panoramic wallpaper across all available displays.</summary>
    Span = 5
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.Models;

/// <summary>
/// Represents a customizable icon on the user's desktop (shortcut .lnk or folder).
/// </summary>
public sealed partial class DesktopIconItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Path to the shortcut (.lnk) or directory on the desktop.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Target path of the shortcut (e.g. C:\Program Files\App\app.exe), or directory path if folder.
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// Current icon location override (if any). If empty, Windows resolves the icon from TargetPath.
    /// </summary>
    [ObservableProperty]
    private string _currentIconPath = string.Empty;

    /// <summary>
    /// Index of the icon within the file (if .ico, usually 0).
    /// </summary>
    [ObservableProperty]
    private int _currentIconIndex;

    /// <summary>
    /// Original icon location captured in the first baseline snapshot.
    /// </summary>
    public string OriginalIconPath { get; set; } = string.Empty;

    /// <summary>
    /// Original icon index captured in the first baseline snapshot.
    /// </summary>
    public int OriginalIconIndex { get; set; }

    /// <summary>
    /// True if Deskora has applied a custom icon to this item.
    /// </summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// True if this item is currently selected in the UI.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// True if this item is a desktop folder rather than a .lnk shortcut.
    /// </summary>
    public bool IsFolder { get; init; }

    /// <summary>
    /// Friendly type label for display in UI.
    /// </summary>
    public string TypeLabel => IsFolder ? "Desktop Folder" : "Desktop Shortcut (.lnk)";

    /// <summary>
    /// Platform-layer icon thumbnail image, set by the App layer after scanning.
    /// Type is object? to avoid a Core-layer dependency on Avalonia/GDI.
    /// Cast to Avalonia.Media.Imaging.Bitmap in the presentation layer.
    /// </summary>
    [ObservableProperty]
    private object? _thumbnailBitmap;
}

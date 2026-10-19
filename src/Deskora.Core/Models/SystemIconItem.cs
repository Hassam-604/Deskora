namespace Deskora.Core.Models;

/// <summary>
/// Represents a native Windows built-in icon from a system library (e.g. imageres.dll, shell32.dll, ddores.dll).
/// </summary>
public sealed class SystemIconItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string LibraryPath { get; init; } = string.Empty;
    public string LibraryName { get; init; } = string.Empty;
    public int IconIndex { get; init; }
    public string Category { get; init; } = "General";
    public object? ThumbnailBitmap { get; set; }
}

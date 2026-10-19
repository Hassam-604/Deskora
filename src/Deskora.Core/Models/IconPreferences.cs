namespace Deskora.Core.Models;

/// <summary>
/// Persisted preferences and snapshot history for customized desktop icons.
/// </summary>
public sealed class IconPreferences
{
    /// <summary>
    /// Map of normalized item path to its original baseline snapshot.
    /// </summary>
    public Dictionary<string, IconSnapshot> Snapshots { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Map of normalized item path to current custom icon path.
    /// </summary>
    public Dictionary<string, string> CustomIcons { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Last known user-selected icon folder or pack directory.
    /// </summary>
    public string? LastIconDirectory { get; set; }
}

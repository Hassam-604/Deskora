namespace Deskora.Core.Models;

/// <summary>
/// Point-in-time snapshot of an icon's state prior to customization for 1-click safe rollback.
/// </summary>
public sealed record IconSnapshot(
    string ItemPath,
    string OriginalIconPath,
    int OriginalIconIndex,
    DateTime CapturedAtUtc
);

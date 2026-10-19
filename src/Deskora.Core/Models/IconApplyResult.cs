namespace Deskora.Core.Models;

/// <summary>
/// Result of applying or restoring a desktop icon.
/// </summary>
public sealed record IconApplyResult(
    bool Success,
    string? ErrorMessage = null,
    string? ItemPath = null
);

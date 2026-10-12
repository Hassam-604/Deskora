namespace Deskora.Core.Models;

/// <summary>
/// Information about a connected display monitor.
/// </summary>
public record MonitorInfo(
    string Id,
    int Index,
    string Name,
    string Resolution,
    bool IsPrimary
);

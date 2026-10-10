namespace Deskora.Core.Models;

public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Represents an application notification banner or alert.
/// </summary>
public record AppNotification(
    string Message,
    NotificationSeverity Severity = NotificationSeverity.Info,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.Now : Timestamp;
}

using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for reporting and handling application errors and notifications.
/// </summary>
public interface IErrorService
{
    event EventHandler<AppNotification?>? NotificationChanged;
    AppNotification? CurrentNotification { get; }

    void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info);
    void ReportError(string message, Exception? ex = null);
    void ClearNotification();
}

/// <summary>
/// In-memory implementation of error reporting and notifications.
/// </summary>
public class ErrorService : IErrorService
{
    private AppNotification? _currentNotification;

    public event EventHandler<AppNotification?>? NotificationChanged;

    public AppNotification? CurrentNotification => _currentNotification;

    public void Notify(string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        _currentNotification = new AppNotification(message, severity);
        NotificationChanged?.Invoke(this, _currentNotification);
    }

    public void ReportError(string message, Exception? ex = null)
    {
        var formattedMessage = ex == null ? message : $"{message} ({ex.Message})";
        _currentNotification = new AppNotification(formattedMessage, NotificationSeverity.Error);
        NotificationChanged?.Invoke(this, _currentNotification);
    }

    public void ClearNotification()
    {
        _currentNotification = null;
        NotificationChanged?.Invoke(this, null);
    }
}

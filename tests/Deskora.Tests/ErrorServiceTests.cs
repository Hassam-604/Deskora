using Deskora.Core.Models;
using Deskora.Core.Services;
using Xunit;

namespace Deskora.Tests;

public class ErrorServiceTests
{
    [Fact]
    public void Notify_SetsCurrentNotification_AndFiresEvent()
    {
        var service = new ErrorService();
        AppNotification? receivedNotification = null;
        service.NotificationChanged += (sender, notif) => receivedNotification = notif;

        service.Notify("Test notification", NotificationSeverity.Info);

        Assert.NotNull(service.CurrentNotification);
        Assert.Equal("Test notification", service.CurrentNotification.Message);
        Assert.Equal(NotificationSeverity.Info, service.CurrentNotification.Severity);
        Assert.NotNull(receivedNotification);
    }

    [Fact]
    public void ReportError_FormatsMessageWithErrorSeverity()
    {
        var service = new ErrorService();
        var ex = new InvalidOperationException("Disk full");

        service.ReportError("Save failed", ex);

        Assert.NotNull(service.CurrentNotification);
        Assert.Equal(NotificationSeverity.Error, service.CurrentNotification.Severity);
        Assert.Contains("Save failed", service.CurrentNotification.Message);
        Assert.Contains("Disk full", service.CurrentNotification.Message);
    }

    [Fact]
    public void ClearNotification_ResetsStateToNull()
    {
        var service = new ErrorService();
        service.Notify("Sample");
        Assert.NotNull(service.CurrentNotification);

        service.ClearNotification();
        Assert.Null(service.CurrentNotification);
    }
}

using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockWallpaperService : IWallpaperService
{
    public bool IsSupported { get; set; } = true;
    public List<MonitorInfo> Monitors { get; set; } = new()
    {
        new MonitorInfo("mon-1", 0, "Display 1 (Primary)", "1920 × 1080", true),
        new MonitorInfo("mon-2", 1, "Display 2", "2560 × 1440", false)
    };

    public string? CurrentWallpaper { get; set; } = "C:\\Wallpapers\\original_default.jpg";
    public WallpaperFitMode CurrentFitMode { get; set; } = WallpaperFitMode.Fill;

    public int CaptureSnapshotCallCount { get; private set; }
    public int ApplyWallpaperCallCount { get; private set; }
    public int RestoreWallpaperCallCount { get; private set; }

    public string? LastAppliedPath { get; private set; }
    public WallpaperFitMode? LastAppliedFitMode { get; private set; }
    public string? LastAppliedMonitorId { get; private set; }
    public WallpaperSnapshot? LastRestoredSnapshot { get; private set; }

    public bool ShouldFailApply { get; set; }
    public bool ShouldFailRestore { get; set; }

    public Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync()
    {
        return Task.FromResult<IReadOnlyList<MonitorInfo>>(Monitors);
    }

    public Task<WallpaperSnapshot> CaptureCurrentSnapshotAsync()
    {
        CaptureSnapshotCallCount++;
        var snapshot = new WallpaperSnapshot(
            Timestamp: DateTime.Now,
            GlobalWallpaperPath: CurrentWallpaper,
            MonitorWallpapers: new Dictionary<string, string> { { "mon-1", CurrentWallpaper ?? "" } },
            Position: CurrentFitMode
        );
        return Task.FromResult(snapshot);
    }

    public Task<WallpaperApplyResult> ApplyWallpaperAsync(string filePath, WallpaperFitMode fitMode, string? monitorId = null)
    {
        ApplyWallpaperCallCount++;
        LastAppliedPath = filePath;
        LastAppliedFitMode = fitMode;
        LastAppliedMonitorId = monitorId;

        if (ShouldFailApply)
        {
            return Task.FromResult(new WallpaperApplyResult(false, "Simulated COM error applying wallpaper."));
        }

        CurrentWallpaper = filePath;
        CurrentFitMode = fitMode;
        return Task.FromResult(new WallpaperApplyResult(true, "Applied successfully.", filePath, fitMode));
    }

    public Task<WallpaperApplyResult> RestoreWallpaperAsync(WallpaperSnapshot snapshot)
    {
        RestoreWallpaperCallCount++;
        LastRestoredSnapshot = snapshot;

        if (ShouldFailRestore)
        {
            return Task.FromResult(new WallpaperApplyResult(false, "Simulated restore error."));
        }

        CurrentWallpaper = snapshot.GlobalWallpaperPath;
        CurrentFitMode = snapshot.Position;
        return Task.FromResult(new WallpaperApplyResult(true, "Restored successfully.", snapshot.GlobalWallpaperPath, snapshot.Position));
    }

    public Task<string?> GetCurrentWallpaperPathAsync(string? monitorId = null)
    {
        return Task.FromResult(CurrentWallpaper);
    }
}

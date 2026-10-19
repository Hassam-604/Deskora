using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockIconService : IIconService
{
    public List<DesktopIconItem> IconsToReturn { get; } = new();
    public int ScanCallCount { get; private set; }
    public int ApplyCallCount { get; private set; }
    public int RestoreCallCount { get; private set; }
    public int ResetAllCallCount { get; private set; }
    public int RefreshShellCallCount { get; private set; }

    public bool ApplyShouldSucceed { get; set; } = true;
    public bool RestoreShouldSucceed { get; set; } = true;

    public Task<IReadOnlyList<DesktopIconItem>> GetDesktopIconsAsync(CancellationToken ct = default)
    {
        ScanCallCount++;
        return Task.FromResult<IReadOnlyList<DesktopIconItem>>(IconsToReturn.ToList());
    }

    public Task<IconApplyResult> ApplyCustomIconAsync(DesktopIconItem item, string iconPath, int iconIndex = 0, CancellationToken ct = default)
    {
        ApplyCallCount++;
        if (!ApplyShouldSucceed)
        {
            return Task.FromResult(new IconApplyResult(false, "Simulated apply error", item.FilePath));
        }

        item.CurrentIconPath = iconPath;
        item.CurrentIconIndex = iconIndex;
        item.IsModified = true;
        return Task.FromResult(new IconApplyResult(true, ItemPath: item.FilePath));
    }

    public Task<IconApplyResult> RestoreOriginalIconAsync(DesktopIconItem item, CancellationToken ct = default)
    {
        RestoreCallCount++;
        if (!RestoreShouldSucceed)
        {
            return Task.FromResult(new IconApplyResult(false, "Simulated restore error", item.FilePath));
        }

        item.CurrentIconPath = item.OriginalIconPath;
        item.CurrentIconIndex = item.OriginalIconIndex;
        item.IsModified = false;
        return Task.FromResult(new IconApplyResult(true, ItemPath: item.FilePath));
    }

    public Task<IconApplyResult> ResetAllToDefaultsAsync(CancellationToken ct = default)
    {
        ResetAllCallCount++;
        foreach (var item in IconsToReturn)
        {
            item.CurrentIconPath = item.OriginalIconPath;
            item.CurrentIconIndex = item.OriginalIconIndex;
            item.IsModified = false;
        }

        return Task.FromResult(new IconApplyResult(true));
    }

    public Task RefreshShellCacheAsync(CancellationToken ct = default)
    {
        RefreshShellCallCount++;
        return Task.CompletedTask;
    }

    public object? ExtractThumbnail(string path, bool isFolder = false) => null;
}

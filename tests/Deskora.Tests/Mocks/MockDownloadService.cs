using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockDownloadService : IDownloadService
{
    public string GalleryStorageDirectory { get; set; } = @"C:\Test\Deskora\Gallery";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    public HashSet<string> DownloadedIds { get; } = new();
    public List<string> DownloadHistory { get; } = new();
    public bool SimulateCancellation { get; set; }
    public bool SimulateCorruption { get; set; }
    public bool SimulateFailure { get; set; }
    public string? FailureReason { get; set; }

    public Task<DownloadResult> DownloadEntryAsync(
        GalleryEntry entry,
        IProgress<double> progress,
        CancellationToken ct)
    {
        DownloadHistory.Add(entry.Id);

        if (ct.IsCancellationRequested || SimulateCancellation)
        {
            return Task.FromResult(new DownloadResult(false, WasCancelled: true));
        }

        if (SimulateCorruption)
        {
            return Task.FromResult(new DownloadResult(false, WasCorrupted: true, ErrorMessage: "Checksum mismatch"));
        }

        if (SimulateFailure)
        {
            return Task.FromResult(new DownloadResult(false, ErrorMessage: FailureReason ?? "Network error"));
        }

        progress.Report(50);
        progress.Report(100);

        DownloadedIds.Add(entry.Id);
        var localPath = Path.Combine(GalleryStorageDirectory, entry.LocalFileName);

        return Task.FromResult(new DownloadResult(true, LocalPath: localPath));
    }

    public string? GetLocalPath(GalleryEntry entry)
    {
        return DownloadedIds.Contains(entry.Id)
            ? Path.Combine(GalleryStorageDirectory, entry.LocalFileName)
            : null;
    }

    public bool DeleteLocalAsset(GalleryEntry entry)
    {
        return DownloadedIds.Remove(entry.Id);
    }

    public long GetStorageUsedBytes()
    {
        return DownloadedIds.Count * 2_500_000L; // 2.5 MB per file simulation
    }
}

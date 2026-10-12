using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Result of a file download operation.
/// </summary>
public sealed record DownloadResult(
    bool Success,
    string? LocalPath = null,
    string? ErrorMessage = null,
    bool WasCancelled = false,
    bool WasCorrupted = false
);

/// <summary>
/// Service contract for downloading, validating, and managing gallery asset files.
/// 
/// Security constraints:
/// - Only downloads from explicitly approved URLs in catalog entries.
/// - Never executes downloaded content.
/// - Verifies SHA-256 hash before marking a download as complete.
/// - Enforces maximum file size limits.
/// - Stores assets in an application-managed directory only.
/// </summary>
public interface IDownloadService
{
    /// <summary>Application-managed directory for downloaded gallery assets.</summary>
    string GalleryStorageDirectory { get; }

    /// <summary>
    /// Download a single gallery asset with progress reporting and cancellation support.
    /// Performs SHA-256 integrity verification after download.
    /// Partial/failed downloads are cleaned up automatically.
    /// </summary>
    Task<DownloadResult> DownloadEntryAsync(
        GalleryEntry entry,
        IProgress<double> progress,
        CancellationToken ct);

    /// <summary>Returns local path if the entry has already been downloaded successfully, null otherwise.</summary>
    string? GetLocalPath(GalleryEntry entry);

    /// <summary>Deletes a downloaded asset from local storage. Does not affect catalog metadata.</summary>
    bool DeleteLocalAsset(GalleryEntry entry);

    /// <summary>Returns approximate total bytes used by the gallery storage directory.</summary>
    long GetStorageUsedBytes();

    /// <summary>Maximum allowed download size in bytes (safety cap).</summary>
    long MaxFileSizeBytes { get; }
}

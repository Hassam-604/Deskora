using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.Models;

/// <summary>
/// Runtime observable state for a gallery entry: download progress, local path, error.
/// Kept separate from the immutable GalleryEntry catalog record.
/// </summary>
public partial class GalleryDownloadRecord : ObservableObject
{
    [ObservableProperty]
    private DownloadState _state = DownloadState.Available;

    [ObservableProperty]
    private double _progressPercent; // 0.0 – 100.0

    [ObservableProperty]
    private string? _localPath;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isPreviewLoaded;

    public string EntryId { get; }

    public GalleryDownloadRecord(string entryId)
    {
        EntryId = entryId;
    }

    /// <summary>Returns true if the asset is available on disk and usable.</summary>
    public bool IsReady => State == DownloadState.Downloaded && !string.IsNullOrEmpty(LocalPath) && File.Exists(LocalPath);

    /// <summary>Whether a download operation is actively running.</summary>
    public bool IsActive => State == DownloadState.Downloading;
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

/// <summary>
/// ViewModel for the 0.3.0 Curated Downloadable Gallery feature.
/// Manages catalog browsing, filtering, detail preview, download lifecycle,
/// offline detection, and local asset management.
/// </summary>
public partial class GalleryViewModel : ViewModelBase
{
    private readonly IGalleryProvider _catalogProvider;
    private readonly IDownloadService _downloadService;
    private readonly IErrorService _errorService;
    private readonly IWallpaperService? _wallpaperService;

    // Full catalog (never mutated after load)
    private readonly List<GalleryEntry> _catalog = new();

    // Per-entry runtime state dictionary
    private readonly Dictionary<string, GalleryDownloadRecord> _records = new();

    // Active download cancellation tokens (one per entry)
    private readonly Dictionary<string, CancellationTokenSource> _activeCts = new();

    // ── Observable state ──────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private string _loadError = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private GalleryCategory _activeCategory = GalleryCategory.All;

    [ObservableProperty]
    private GalleryEntry? _selectedEntry;

    [ObservableProperty]
    private GalleryDownloadRecord? _selectedRecord;

    [ObservableProperty]
    private string _storageUsedLabel = "Calculating...";

    [ObservableProperty]
    private string _catalogSource = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _downloadedCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private bool _hasMorePages = true;

    [ObservableProperty]
    private bool _isLoadingMore;

    public ObservableCollection<GalleryEntry> DisplayedEntries { get; } = new();

    // ── Category filter options for UI binding ─────────────────────────────
    public IReadOnlyList<GalleryCategory> CategoryFilters { get; } = new[]
    {
        GalleryCategory.All,
        GalleryCategory.Nature,
        GalleryCategory.Space,
        GalleryCategory.Abstract,
        GalleryCategory.Architecture,
        GalleryCategory.Minimal,
        GalleryCategory.Anime
    };

    private readonly SynchronizationContext? _syncContext;

    public GalleryViewModel(
        IGalleryProvider catalogProvider,
        IDownloadService downloadService,
        IErrorService errorService,
        IWallpaperService? wallpaperService = null)
    {
        _syncContext = SynchronizationContext.Current;
        _catalogProvider = catalogProvider;
        _downloadService = downloadService;
        _errorService = errorService;
        _wallpaperService = wallpaperService;
        Title = "Gallery";
        CatalogSource = catalogProvider.CatalogSource;
    }

    private void RunOnUi(Action action)
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Send(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    // ── Initialization ─────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        IsLoading = true;
        LoadError = string.Empty;
        CurrentPage = 1;
        try
        {
            var entries = await _catalogProvider.SearchCatalogAsync(SearchQuery, ActiveCategory, 1);
            _catalog.Clear();
            _catalog.AddRange(entries);

            SyncRecords(_catalog);

            CatalogSource = _catalogProvider.CatalogSource;
            UpdateStorageLabel();
            ApplyFilter();
            UpdateCounts();
            HasMorePages = entries.Count >= 8;
        }
        catch (Exception ex)
        {
            LoadError = $"Failed to load gallery catalog: {ex.Message}";
            _errorService.ReportError("Could not load the gallery catalog", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Filtering / Search / Pagination ────────────────────────────────────

    private CancellationTokenSource? _searchDebounceCts;

    partial void OnSearchQueryChanged(string value)
    {
        _searchDebounceCts?.Cancel();

        if (string.IsNullOrWhiteSpace(value))
        {
            _ = SearchAsync();
            return;
        }

        // Immediately filter existing in-memory entries on the UI thread
        ApplyFilter();

        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = DebounceAndSearchAsync(token);
    }

    private async Task DebounceAndSearchAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(400, token);
            if (!token.IsCancellationRequested)
            {
                await SearchAsync();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _errorService.Notify($"Search error: {ex.Message}", NotificationSeverity.Warning);
        }
    }

    partial void OnActiveCategoryChanged(GalleryCategory value)
    {
        _ = SearchAsync();
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        CurrentPage = 1;
        IsLoading = true;
        LoadError = string.Empty;
        try
        {
            var entries = await _catalogProvider.SearchCatalogAsync(SearchQuery, ActiveCategory, 1);
            _catalog.Clear();
            _catalog.AddRange(entries);

            SyncRecords(_catalog);
            CatalogSource = _catalogProvider.CatalogSource;
            ApplyFilter();
            UpdateCounts();
            HasMorePages = entries.Count >= 8;
        }
        catch (Exception ex)
        {
            _errorService.Notify($"Search error: {ex.Message}", NotificationSeverity.Warning);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsLoading) return;

        IsLoadingMore = true;
        try
        {
            int nextPage = CurrentPage + 1;
            var entries = await _catalogProvider.SearchCatalogAsync(SearchQuery, ActiveCategory, nextPage);

            if (entries.Count == 0)
            {
                HasMorePages = false;
                _errorService.Notify("Reached the end of available wallpapers for this search.", NotificationSeverity.Info);
                return;
            }

            CurrentPage = nextPage;
            foreach (var entry in entries)
            {
                if (!_catalog.Any(e => e.Id == entry.Id))
                {
                    _catalog.Add(entry);
                }
            }

            SyncRecords(entries);
            ApplyFilter();
            UpdateCounts();
            HasMorePages = entries.Count >= 8;
        }
        catch (Exception ex)
        {
            _errorService.Notify($"Could not load more wallpapers: {ex.Message}", NotificationSeverity.Warning);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private void SyncRecords(IEnumerable<GalleryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!_records.TryGetValue(entry.Id, out var rec))
            {
                rec = new GalleryDownloadRecord(entry.Id);
                _records[entry.Id] = rec;
            }

            var localPath = _downloadService.GetLocalPath(entry);
            if (localPath != null)
            {
                rec.LocalPath = localPath;
                rec.State = DownloadState.Downloaded;
            }
        }
    }

    private void ApplyFilter()
    {
        var filtered = _catalog.AsEnumerable();

        if (ActiveCategory != GalleryCategory.All)
        {
            filtered = filtered.Where(e => e.Category == ActiveCategory);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            filtered = filtered.Where(e =>
                e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Author.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        var list = filtered.ToList();
        RunOnUi(() =>
        {
            DisplayedEntries.Clear();
            foreach (var entry in list)
            {
                DisplayedEntries.Add(entry);
            }
        });
    }

    // ── Selection ──────────────────────────────────────────────────────────

    [RelayCommand]
    public void SelectEntry(GalleryEntry? entry)
    {
        SelectedEntry = entry;
        SelectedRecord = entry != null && _records.TryGetValue(entry.Id, out var rec) ? rec : null;
    }

    [RelayCommand]
    public void SetCategoryFilter(string categoryName)
    {
        if (Enum.TryParse<GalleryCategory>(categoryName, true, out var cat))
        {
            ActiveCategory = cat;
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        _searchDebounceCts?.Cancel();
        SearchQuery = string.Empty;
    }

    // ── Download lifecycle ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task DownloadSelectedAsync()
    {
        if (SelectedEntry == null) return;
        await DownloadEntryAsync(SelectedEntry);
    }

    [RelayCommand]
    public async Task RetryDownloadAsync()
    {
        if (SelectedEntry == null) return;
        await DownloadEntryAsync(SelectedEntry);
    }

    private async Task DownloadEntryAsync(GalleryEntry entry)
    {
        // Prevent duplicate downloads
        if (_activeCts.ContainsKey(entry.Id))
        {
            _errorService.Notify($"'{entry.Title}' is already being downloaded.", NotificationSeverity.Info);
            return;
        }

        if (!_records.TryGetValue(entry.Id, out var rec))
        {
            rec = new GalleryDownloadRecord(entry.Id);
            _records[entry.Id] = rec;
        }

        // Re-select so the UI record reflects new state
        if (SelectedEntry?.Id == entry.Id)
        {
            SelectedRecord = rec;
        }

        var cts = new CancellationTokenSource();
        _activeCts[entry.Id] = cts;

        rec.State = DownloadState.Downloading;
        rec.ProgressPercent = 0;
        rec.ErrorMessage = null;

        try
        {
            var progress = new Progress<double>(p =>
            {
                if (rec.State == DownloadState.Downloading)
                {
                    rec.ProgressPercent = p;
                }
            });

            var result = await _downloadService.DownloadEntryAsync(entry, progress, cts.Token);

            if (result.WasCancelled)
            {
                rec.State = DownloadState.Cancelled;
                rec.ProgressPercent = 0;
            }
            else if (result.WasCorrupted)
            {
                rec.State = DownloadState.Failed;
                rec.ErrorMessage = "Integrity check failed: file hash did not match. Download discarded.";
                _errorService.Notify($"'{entry.Title}' download failed integrity check.", NotificationSeverity.Error);
            }
            else if (result.Success)
            {
                rec.State = DownloadState.Downloaded;
                rec.LocalPath = result.LocalPath;
                rec.ProgressPercent = 100;
                UpdateStorageLabel();
                UpdateCounts();
                _errorService.Notify($"'{entry.Title}' downloaded successfully.", NotificationSeverity.Success);
            }
            else
            {
                rec.State = DownloadState.Failed;
                rec.ErrorMessage = result.ErrorMessage ?? "Unknown download error.";
                _errorService.Notify($"Failed to download '{entry.Title}': {rec.ErrorMessage}", NotificationSeverity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            rec.State = DownloadState.Cancelled;
            rec.ProgressPercent = 0;
        }
        catch (Exception ex)
        {
            rec.State = DownloadState.Failed;
            rec.ErrorMessage = ex.Message;
            _errorService.ReportError($"Exception downloading '{entry.Title}'", ex);
        }
        finally
        {
            _activeCts.Remove(entry.Id);
            cts.Dispose();
        }
    }

    [RelayCommand]
    public void CancelDownload()
    {
        if (SelectedEntry == null) return;
        CancelEntryDownload(SelectedEntry.Id);
    }

    public void CancelEntryDownload(string entryId)
    {
        if (_activeCts.TryGetValue(entryId, out var cts))
        {
            cts.Cancel();
        }
    }

    // ── Local asset management ─────────────────────────────────────────────

    [RelayCommand]
    public void DeleteLocalAsset()
    {
        if (SelectedEntry == null || SelectedRecord == null) return;

        var entry = SelectedEntry;
        var rec = SelectedRecord;

        if (rec.State != DownloadState.Downloaded)
        {
            _errorService.Notify("No local copy exists to delete.", NotificationSeverity.Info);
            return;
        }

        bool deleted = _downloadService.DeleteLocalAsset(entry);
        if (deleted)
        {
            rec.State = DownloadState.Available;
            rec.LocalPath = null;
            rec.ProgressPercent = 0;
            UpdateStorageLabel();
            UpdateCounts();
            _errorService.Notify($"Local copy of '{entry.Title}' removed. Original file on catalog preserved.", NotificationSeverity.Info);
        }
        else
        {
            _errorService.Notify($"Could not delete local file for '{entry.Title}'.", NotificationSeverity.Warning);
        }
    }

    [RelayCommand]
    public async Task SetAsWallpaperAsync()
    {
        if (SelectedEntry == null || SelectedRecord == null) return;
        if (SelectedRecord.State != DownloadState.Downloaded || string.IsNullOrEmpty(SelectedRecord.LocalPath))
        {
            _errorService.Notify("Asset must be downloaded before applying as wallpaper.", NotificationSeverity.Warning);
            return;
        }

        if (_wallpaperService == null)
        {
            _errorService.Notify("Wallpaper service is unavailable on this platform.", NotificationSeverity.Warning);
            return;
        }

        try
        {
            var result = await _wallpaperService.ApplyWallpaperAsync(SelectedRecord.LocalPath, WallpaperFitMode.Fill);
            if (result.Success)
            {
                _errorService.Notify($"Applied '{SelectedEntry.Title}' as desktop wallpaper!", NotificationSeverity.Success);
            }
            else
            {
                _errorService.Notify($"Could not apply wallpaper: {result.Message}", NotificationSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError($"Error applying wallpaper '{SelectedEntry.Title}'", ex);
        }
    }

    [RelayCommand]
    public void RefreshCatalog()
    {
        _ = InitializeAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Returns the live download record for a given entry ID (for gallery card state binding).</summary>
    public GalleryDownloadRecord GetRecordFor(string entryId)
    {
        if (_records.TryGetValue(entryId, out var rec)) return rec;
        var newRec = new GalleryDownloadRecord(entryId);
        _records[entryId] = newRec;
        return newRec;
    }

    private void UpdateStorageLabel()
    {
        long bytes = _downloadService.GetStorageUsedBytes();
        StorageUsedLabel = bytes >= 1_048_576
            ? $"{bytes / 1_048_576.0:F1} MB used"
            : $"{bytes / 1024.0:F0} KB used";
    }

    private void UpdateCounts()
    {
        TotalCount = _catalog.Count;
        DownloadedCount = _records.Values.Count(r => r.State == DownloadState.Downloaded);
    }
}

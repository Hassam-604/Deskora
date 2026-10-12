using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

public enum WallpaperFilterTab
{
    All,
    Favorites,
    Recent
}

public partial class WallpapersViewModel : ViewModelBase
{
    private readonly IWallpaperService _wallpaperService;
    private readonly IWallpaperPreferencesService _preferencesService;
    private readonly IFilePickerService _filePickerService;
    private readonly IErrorService _errorService;

    private readonly List<WallpaperItem> _allWallpapers = new();
    private WallpaperPreferences _preferences = new();

    public ObservableCollection<WallpaperItem> DisplayedWallpapers { get; } = new();
    public ObservableCollection<MonitorInfo> Monitors { get; } = new();

    public IReadOnlyList<WallpaperFitMode> AvailableFitModes { get; } = new[]
    {
        WallpaperFitMode.Fill,
        WallpaperFitMode.Fit,
        WallpaperFitMode.Stretch,
        WallpaperFitMode.Center,
        WallpaperFitMode.Tile,
        WallpaperFitMode.Span
    };

    [ObservableProperty]
    private WallpaperItem? _selectedWallpaper;

    [ObservableProperty]
    private MonitorInfo? _selectedMonitor;

    [ObservableProperty]
    private WallpaperFitMode _selectedFitMode = WallpaperFitMode.Fill;

    [ObservableProperty]
    private WallpaperFilterTab _activeFilterTab = WallpaperFilterTab.All;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingStatus = string.Empty;

    [ObservableProperty]
    private bool _isApplying;

    [ObservableProperty]
    private bool _canRestore;

    [ObservableProperty]
    private string _restoreButtonTooltip = "No prior wallpaper snapshot available to restore.";

    [ObservableProperty]
    private string? _currentFolderPath;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _favoritesCount;

    [ObservableProperty]
    private string _activeSystemWallpaperPath = string.Empty;

    private readonly SemaphoreSlim _initLock = new(1, 1);

    public Task InitializationTask { get; }

    public WallpapersViewModel(
        IWallpaperService wallpaperService,
        IWallpaperPreferencesService preferencesService,
        IFilePickerService filePickerService,
        IErrorService errorService)
    {
        _wallpaperService = wallpaperService;
        _preferencesService = preferencesService;
        _filePickerService = filePickerService;
        _errorService = errorService;

        Title = "Wallpapers";

        InitializationTask = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            IsLoading = true;
            LoadingStatus = "Initializing Wallpaper Studio...";

            await LoadMonitorsAsync();
            _preferences = await _preferencesService.LoadPreferencesAsync();
            SelectedFitMode = _preferences.LastFitMode;

            if (_preferences.LastSnapshot != null)
            {
                CanRestore = true;
                RestoreButtonTooltip = $"Restore previous wallpaper: {_preferences.LastSnapshot.Summary}";
            }

            CurrentFolderPath = _preferences.SelectedFolder;
            await ScanAndLoadLibraryAsync();

            var current = await _wallpaperService.GetCurrentWallpaperPathAsync();
            ActiveSystemWallpaperPath = current ?? string.Empty;
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Failed to initialize wallpaper library", ex);
        }
        finally
        {
            IsLoading = false;
            LoadingStatus = string.Empty;
            _initLock.Release();
        }
    }

    private async Task LoadMonitorsAsync()
    {
        Monitors.Clear();
        var detected = await _wallpaperService.GetMonitorsAsync();
        foreach (var m in detected)
        {
            Monitors.Add(m);
        }
    }

    [RelayCommand]
    public async Task SelectFolderAsync()
    {
        var folder = await _filePickerService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        CurrentFolderPath = folder;
        _preferences.SelectedFolder = folder;
        await _preferencesService.SavePreferencesAsync(_preferences);

        await ScanAndLoadLibraryAsync();
    }

    [RelayCommand]
    public async Task AddImagesAsync()
    {
        var files = await _filePickerService.PickImageFilesAsync();
        if (files.Count == 0) return;

        int addedCount = 0;
        bool stateChanged = false;

        foreach (var file in files)
        {
            // Un-exclude if it was previously removed from library
            int removedFromExcluded = _preferences.ExcludedPaths.RemoveAll(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase));
            if (removedFromExcluded > 0)
            {
                stateChanged = true;
            }

            if (!_preferences.CustomImagePaths.Contains(file, StringComparer.OrdinalIgnoreCase))
            {
                _preferences.CustomImagePaths.Add(file);
                addedCount++;
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            await _preferencesService.SavePreferencesAsync(_preferences);
            await ScanAndLoadLibraryAsync();
            _errorService.Notify(
                addedCount > 0
                    ? $"Added {addedCount} image(s) to wallpaper library."
                    : "Restored image(s) to wallpaper library.",
                NotificationSeverity.Success);
        }
        else
        {
            _errorService.Notify("The selected image(s) are already in your wallpaper library.", NotificationSeverity.Info);
        }
    }

    [RelayCommand]
    public async Task RefreshLibraryAsync()
    {
        await ScanAndLoadLibraryAsync();
    }

    public async Task ScanAndLoadLibraryAsync()
    {
        IsLoading = true;
        LoadingStatus = "Scanning and indexing wallpaper images...";

        try
        {
            var filesToInspect = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add custom individually added images
            foreach (var customPath in _preferences.CustomImagePaths)
            {
                filesToInspect.Add(customPath);
            }

            // Add files from selected folder if present
            if (!string.IsNullOrWhiteSpace(CurrentFolderPath) && Directory.Exists(CurrentFolderPath))
            {
                var dirFiles = await Task.Run(() =>
                {
                    try
                    {
                        return Directory.EnumerateFiles(CurrentFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(ImageHeaderReader.IsSupportedFormat)
                            .ToList();
                    }
                    catch
                    {
                        return new List<string>();
                    }
                });

                foreach (var f in dirFiles)
                {
                    filesToInspect.Add(f);
                }
            }

            // Exclude files that the user explicitly removed from library
            filesToInspect.RemoveWhere(p => _preferences.ExcludedPaths.Contains(p, StringComparer.OrdinalIgnoreCase));

            // Inspect and create items on a background thread
            var items = await Task.Run(() =>
            {
                var result = new List<WallpaperItem>();
                foreach (var file in filesToInspect)
                {
                    bool exists = File.Exists(file);
                    long size = 0;
                    int width = 0;
                    int height = 0;
                    bool isValid = false;
                    string? error = null;

                    if (exists)
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            size = fi.Length;
                            if (ImageHeaderReader.TryGetDimensions(file, out width, out height))
                            {
                                isValid = true;
                            }
                            else
                            {
                                error = "Unsupported image format or invalid header";
                            }
                        }
                        catch (Exception ex)
                        {
                            error = ex.Message;
                        }
                    }
                    else
                    {
                        error = "File not found";
                    }

                    bool isFav = _preferences.FavoritePaths.Contains(file, StringComparer.OrdinalIgnoreCase);
                    DateTime? lastApplied = null;
                    if (_preferences.RecentWallpaperPaths.Contains(file, StringComparer.OrdinalIgnoreCase))
                    {
                        lastApplied = DateTime.Now; // tagged as recent
                    }

                    result.Add(new WallpaperItem(
                        filePath: file,
                        fileSizeBytes: size,
                        width: width,
                        height: height,
                        isFavorite: isFav,
                        lastApplied: lastApplied,
                        isValid: isValid,
                        errorMessage: error
                    ));
                }

                return result;
            });

            _allWallpapers.Clear();
            _allWallpapers.AddRange(items);

            UpdateCounts();
            ApplyFilter();

            if (SelectedWallpaper == null || !DisplayedWallpapers.Contains(SelectedWallpaper))
            {
                SelectedWallpaper = DisplayedWallpapers.FirstOrDefault(w => w.IsValid) ?? DisplayedWallpapers.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Error indexing wallpaper library", ex);
        }
        finally
        {
            IsLoading = false;
            LoadingStatus = string.Empty;
        }
    }

    partial void OnSelectedWallpaperChanged(WallpaperItem? value)
    {
        foreach (var item in _allWallpapers)
        {
            item.IsSelected = (item == value);
        }
    }

    [RelayCommand]
    public void SelectWallpaper(WallpaperItem? item)
    {
        SelectedWallpaper = item;
    }

    [RelayCommand]
    public async Task ToggleFavoriteSelectedAsync()
    {
        if (SelectedWallpaper != null)
        {
            await ToggleFavoriteAsync(SelectedWallpaper);
        }
    }

    [RelayCommand]
    public async Task RemoveSelectedWallpaperAsync()
    {
        if (SelectedWallpaper == null) return;

        var pathToRemove = SelectedWallpaper.FilePath;
        var fileName = SelectedWallpaper.FileName;

        // Remove from custom images if present
        _preferences.CustomImagePaths.RemoveAll(p => string.Equals(p, pathToRemove, StringComparison.OrdinalIgnoreCase));

        // Add to excluded paths so folder scan won't reload it
        if (!_preferences.ExcludedPaths.Contains(pathToRemove, StringComparer.OrdinalIgnoreCase))
        {
            _preferences.ExcludedPaths.Add(pathToRemove);
        }

        // Clean up favorites and recents
        _preferences.FavoritePaths.RemoveAll(p => string.Equals(p, pathToRemove, StringComparison.OrdinalIgnoreCase));
        _preferences.RecentWallpaperPaths.RemoveAll(p => string.Equals(p, pathToRemove, StringComparison.OrdinalIgnoreCase));

        await _preferencesService.SavePreferencesAsync(_preferences);

        // Remove from in-memory collections
        _allWallpapers.RemoveAll(w => string.Equals(w.FilePath, pathToRemove, StringComparison.OrdinalIgnoreCase));
        DisplayedWallpapers.Remove(SelectedWallpaper);

        UpdateCounts();
        SelectedWallpaper = DisplayedWallpapers.FirstOrDefault(w => w.IsValid) ?? DisplayedWallpapers.FirstOrDefault();

        _errorService.Notify($"Removed '{fileName}' from Deskora library. (Original file on disk was preserved)", NotificationSeverity.Info);
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(WallpaperItem? item)
    {
        if (item == null) return;

        item.IsFavorite = !item.IsFavorite;

        if (item.IsFavorite)
        {
            if (!_preferences.FavoritePaths.Contains(item.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                _preferences.FavoritePaths.Add(item.FilePath);
            }
        }
        else
        {
            _preferences.FavoritePaths.RemoveAll(p => string.Equals(p, item.FilePath, StringComparison.OrdinalIgnoreCase));
        }

        await _preferencesService.SavePreferencesAsync(_preferences);
        UpdateCounts();

        if (ActiveFilterTab == WallpaperFilterTab.Favorites)
        {
            ApplyFilter();
        }
    }

    [RelayCommand]
    public async Task ApplyWallpaperAsync()
    {
        if (SelectedWallpaper == null)
        {
            _errorService.Notify("Please select a wallpaper image to apply.", NotificationSeverity.Warning);
            return;
        }

        if (!SelectedWallpaper.IsValid)
        {
            _errorService.ReportError($"Cannot apply invalid wallpaper: {SelectedWallpaper.ErrorMessage ?? "Unreadable file"}");
            return;
        }

        if (!File.Exists(SelectedWallpaper.FilePath))
        {
            SelectedWallpaper.IsValid = false;
            SelectedWallpaper.ErrorMessage = "File no longer exists on disk";
            _errorService.ReportError("The chosen wallpaper file was not found on disk.");
            return;
        }

        IsApplying = true;
        try
        {
            // 1. Snapshot current desktop state for safety and 1-click restore
            var snapshot = await _wallpaperService.CaptureCurrentSnapshotAsync();
            _preferences.LastSnapshot = snapshot;
            CanRestore = true;
            RestoreButtonTooltip = $"Restore previous wallpaper: {snapshot.Summary}";

            // 2. Apply the chosen wallpaper
            var result = await _wallpaperService.ApplyWallpaperAsync(
                SelectedWallpaper.FilePath,
                SelectedFitMode,
                SelectedMonitor?.Id
            );

            if (result.Success)
            {
                SelectedWallpaper.LastApplied = DateTime.Now;

                // Update recent wallpapers list
                _preferences.RecentWallpaperPaths.RemoveAll(p => string.Equals(p, SelectedWallpaper.FilePath, StringComparison.OrdinalIgnoreCase));
                _preferences.RecentWallpaperPaths.Insert(0, SelectedWallpaper.FilePath);
                if (_preferences.RecentWallpaperPaths.Count > 20)
                {
                    _preferences.RecentWallpaperPaths.RemoveAt(_preferences.RecentWallpaperPaths.Count - 1);
                }

                _preferences.LastFitMode = SelectedFitMode;
                await _preferencesService.SavePreferencesAsync(_preferences);

                ActiveSystemWallpaperPath = SelectedWallpaper.FilePath;
                _errorService.Notify($"Desktop wallpaper successfully set to '{SelectedWallpaper.FileName}'.", NotificationSeverity.Success);
            }
            else
            {
                _errorService.ReportError($"Failed to set desktop wallpaper: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Exception while setting wallpaper", ex);
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand]
    public async Task RestoreWallpaperAsync()
    {
        if (_preferences.LastSnapshot == null)
        {
            _errorService.Notify("No prior wallpaper snapshot is available to restore.", NotificationSeverity.Warning);
            return;
        }

        IsApplying = true;
        try
        {
            var result = await _wallpaperService.RestoreWallpaperAsync(_preferences.LastSnapshot);
            if (result.Success)
            {
                _errorService.Notify($"Prior wallpaper successfully restored ({_preferences.LastSnapshot.Summary}).", NotificationSeverity.Success);
                var current = await _wallpaperService.GetCurrentWallpaperPathAsync();
                ActiveSystemWallpaperPath = current ?? string.Empty;
            }
            else
            {
                _errorService.ReportError($"Could not restore prior wallpaper: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Error restoring wallpaper", ex);
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand]
    public void SetFilterTab(string tabName)
    {
        if (Enum.TryParse<WallpaperFilterTab>(tabName, true, out var tab))
        {
            ActiveFilterTab = tab;
            ApplyFilter();
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allWallpapers.AsEnumerable();

        // Apply Tab Filter
        filtered = ActiveFilterTab switch
        {
            WallpaperFilterTab.Favorites => filtered.Where(w => w.IsFavorite),
            WallpaperFilterTab.Recent => filtered.Where(w => w.LastApplied.HasValue).OrderByDescending(w => w.LastApplied),
            _ => filtered
        };

        // Apply Search query
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered.Where(w => w.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                           w.FilePath.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedWallpapers.Clear();
        foreach (var item in filtered)
        {
            DisplayedWallpapers.Add(item);
        }

        if (SelectedWallpaper != null && !DisplayedWallpapers.Contains(SelectedWallpaper))
        {
            SelectedWallpaper = DisplayedWallpapers.FirstOrDefault();
        }
    }

    private void UpdateCounts()
    {
        TotalCount = _allWallpapers.Count;
        FavoritesCount = _allWallpapers.Count(w => w.IsFavorite);
    }
}

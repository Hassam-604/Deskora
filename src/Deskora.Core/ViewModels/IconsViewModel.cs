using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

public sealed partial class IconsViewModel : ViewModelBase
{
    private readonly IIconService _iconService;
    private readonly IIconPreferencesService _preferencesService;
    private readonly IFilePickerService _filePickerService;
    private readonly IErrorService _errorService;
    private readonly IIconLibraryService _libraryService;
    private readonly SynchronizationContext? _syncContext;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private DesktopIconItem? _selectedIcon;

    [ObservableProperty]
    private string? _selectedCustomIconPath;

    [ObservableProperty]
    private int _selectedCustomIconIndex;

    [ObservableProperty]
    private string _customIconValidationMessage = string.Empty;

    [ObservableProperty]
    private bool _isCustomIconValid;

    [ObservableProperty]
    private object? _customIconPreviewBitmap;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _modifiedCount;

    // Library Modal properties
    [ObservableProperty]
    private bool _isLibraryOpen;

    [ObservableProperty]
    private string _selectedLibraryTab = "System";

    [ObservableProperty]
    private bool _isSystemTabSelected = true;

    [ObservableProperty]
    private bool _isCuratedTabSelected = false;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _librarySearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLibraryLoading;

    [ObservableProperty]
    private int _libraryDisplayedCount;

    public ObservableCollection<DesktopIconItem> DesktopIcons { get; } = new();
    public ObservableCollection<DesktopIconItem> DisplayedIcons { get; } = new();

    public ObservableCollection<CategoryFilterItem> Categories { get; } = new();

    public ObservableCollection<SystemIconItem> SystemIcons { get; } = new();
    public ObservableCollection<SystemIconItem> DisplayedSystemIcons { get; } = new();

    public ObservableCollection<WebIconItem> CuratedIcons { get; } = new();
    public ObservableCollection<WebIconItem> DisplayedCuratedIcons { get; } = new();

    public IconsViewModel(
        IIconService iconService,
        IIconPreferencesService preferencesService,
        IFilePickerService filePickerService,
        IErrorService errorService,
        IIconLibraryService? libraryService = null)
    {
        _syncContext = SynchronizationContext.Current;
        _iconService = iconService;
        _preferencesService = preferencesService;
        _filePickerService = filePickerService;
        _errorService = errorService;
        _libraryService = libraryService ?? new NullIconLibraryService();
        Title = "Icons";
        UpdateAvailableCategories();
    }

    private sealed class NullIconLibraryService : IIconLibraryService
    {
        public Task<IReadOnlyList<SystemIconItem>> GetSystemIconsAsync(string? category = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SystemIconItem>>(Array.Empty<SystemIconItem>());

        public Task<IReadOnlyList<WebIconItem>> GetCuratedIconsAsync(string? category = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WebIconItem>>(Array.Empty<WebIconItem>());

        public Task<IReadOnlyList<WebIconItem>> SearchIconsAsync(string query, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WebIconItem>>(Array.Empty<WebIconItem>());

        public Task<string?> PrepareWebIconAsync(WebIconItem item, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
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

    public async Task InitializeAsync()
    {
        await ScanDesktopAsync();
    }

    [RelayCommand]
    public async Task ScanDesktopAsync()
    {
        IsLoading = true;
        StatusMessage = "Scanning desktop shortcuts and folders...";
        try
        {
            var icons = await _iconService.GetDesktopIconsAsync();

            RunOnUi(() =>
            {
                DesktopIcons.Clear();
                foreach (var icon in icons)
                {
                    DesktopIcons.Add(icon);
                }

                ApplyFilter();
                UpdateCounts();

                if (SelectedIcon != null)
                {
                    SelectedIcon = DesktopIcons.FirstOrDefault(i => string.Equals(i.FilePath, SelectedIcon.FilePath, StringComparison.OrdinalIgnoreCase));
                }

                if (SelectedIcon == null && DisplayedIcons.Count > 0)
                {
                    SelectedIcon = DisplayedIcons[0];
                }
            });

            StatusMessage = $"Found {DesktopIcons.Count} customizable desktop items.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            _errorService.ReportError("Failed to scan desktop items", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectIcon(DesktopIconItem? item)
    {
        SelectedIcon = item;
    }

    partial void OnSelectedIconChanged(DesktopIconItem? oldValue, DesktopIconItem? newValue)
    {
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue != null)
        {
            newValue.IsSelected = true;
        }

        SelectedCustomIconPath = null;
        SelectedCustomIconIndex = 0;
        CustomIconPreviewBitmap = null;
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedCustomIconPathChanged(string? value)
    {
        ValidateCustomIcon(value);
        if (IsCustomIconValid && !string.IsNullOrWhiteSpace(value))
        {
            if (CustomIconPreviewBitmap == null)
            {
                CustomIconPreviewBitmap = _iconService.ExtractThumbnail(value, false);
            }
        }
        else
        {
            CustomIconPreviewBitmap = null;
        }
    }

    private void ValidateCustomIcon(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            IsCustomIconValid = false;
            CustomIconValidationMessage = string.Empty;
            return;
        }

        var result = IconValidator.Validate(path);
        IsCustomIconValid = result.IsValid;
        CustomIconValidationMessage = result.IsValid
            ? $"✓ {result.FormatDescription}"
            : $"✗ {result.ErrorMessage}";
    }

    private void ApplyFilter()
    {
        var filtered = DesktopIcons.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            filtered = filtered.Where(i =>
                i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.TargetPath.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        RunOnUi(() =>
        {
            DisplayedIcons.Clear();
            foreach (var item in list)
            {
                DisplayedIcons.Add(item);
            }
        });
    }

    private void UpdateCounts()
    {
        TotalCount = DesktopIcons.Count;
        ModifiedCount = DesktopIcons.Count(i => i.IsModified);
    }

    [RelayCommand]
    public async Task PickCustomIconAsync()
    {
        try
        {
            var file = await _filePickerService.PickIconFileAsync();
            if (!string.IsNullOrEmpty(file))
            {
                SelectedCustomIconIndex = 0;
                SelectedCustomIconPath = file;
            }
        }
        catch (Exception ex)
        {
            _errorService.Notify($"Could not select icon: {ex.Message}", NotificationSeverity.Warning);
        }
    }

    [RelayCommand]
    public async Task OpenLibraryAsync()
    {
        IsLibraryOpen = true;

        if (SystemIcons.Count == 0 && CuratedIcons.Count == 0)
        {
            await LoadLibraryIconsAsync();
        }
        else
        {
            UpdateAvailableCategories();
            ApplyLibraryFilter();
        }
    }

    [RelayCommand]
    public void CloseLibrary()
    {
        IsLibraryOpen = false;
    }

    [RelayCommand]
    public void SelectLibraryTab(string tab)
    {
        SelectedLibraryTab = tab;
        IsSystemTabSelected = string.Equals(tab, "System", StringComparison.OrdinalIgnoreCase);
        IsCuratedTabSelected = !IsSystemTabSelected;
        UpdateAvailableCategories();
        ApplyLibraryFilter();
    }

    [RelayCommand]
    public void SelectCategory(string category)
    {
        SelectedCategory = category;
        foreach (var cat in Categories)
        {
            cat.IsSelected = string.Equals(cat.Name, category, StringComparison.OrdinalIgnoreCase);
        }
        ApplyLibraryFilter();
    }

    [RelayCommand]
    public void ResetLibraryFilters()
    {
        LibrarySearchQuery = string.Empty;
        SelectCategory("All");
    }

    partial void OnLibrarySearchQueryChanged(string value)
    {
        ApplyLibraryFilter();
    }

    public async Task LoadLibraryIconsAsync()
    {
        IsLibraryLoading = true;
        try
        {
            var sys = await _libraryService.GetSystemIconsAsync();
            var cur = await _libraryService.GetCuratedIconsAsync();

            RunOnUi(() =>
            {
                SystemIcons.Clear();
                foreach (var s in sys) SystemIcons.Add(s);

                CuratedIcons.Clear();
                foreach (var c in cur) CuratedIcons.Add(c);

                UpdateAvailableCategories();
                ApplyLibraryFilter();
            });
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Failed to load icon library", ex);
        }
        finally
        {
            IsLibraryLoading = false;
        }
    }

    /// <summary>
    /// Dynamically derives and updates available categories based exclusively on valid icons present in the selected source.
    /// Resets SelectedCategory to 'All' if the previously selected category does not exist in the newly selected source.
    /// </summary>
    public void UpdateAvailableCategories()
    {
        RunOnUi(() =>
        {
            IEnumerable<string> rawCategories;
            if (IsSystemTabSelected)
            {
                rawCategories = SystemIcons
                    .Where(i => !string.IsNullOrWhiteSpace(i.Title) || !string.IsNullOrWhiteSpace(i.LibraryPath))
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c));
            }
            else
            {
                rawCategories = CuratedIcons
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c));
            }

            var distinctList = rawCategories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (distinctList.Count == 0)
            {
                Categories.Clear();
                SelectedCategory = "All";
                return;
            }

            var preferredOrder = new[] { "Folders", "Devices", "System", "Media", "Apps" };
            var orderedCategories = distinctList
                .OrderBy(c =>
                {
                    int idx = Array.FindIndex(preferredOrder, p => p.Equals(c, StringComparison.OrdinalIgnoreCase));
                    return idx >= 0 ? idx : 1000;
                })
                .ThenBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            bool categoryStillAvailable = !SelectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                                          orderedCategories.Any(c => c.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!categoryStillAvailable)
            {
                SelectedCategory = "All";
            }

            Categories.Clear();
            Categories.Add(new CategoryFilterItem("All", isSelected: SelectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase)));
            foreach (var cat in orderedCategories)
            {
                Categories.Add(new CategoryFilterItem(cat, isSelected: SelectedCategory.Equals(cat, StringComparison.OrdinalIgnoreCase)));
            }
        });
    }

    private void ApplyLibraryFilter()
    {
        var q = LibrarySearchQuery.Trim();
        var cat = SelectedCategory;

        // Filter system icons
        var sysFiltered = SystemIcons.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(cat) && !cat.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            sysFiltered = sysFiltered.Where(i => i.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            sysFiltered = sysFiltered.Where(i => i.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                                 i.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                                 i.LibraryName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var sysList = sysFiltered.ToList();

        // Filter curated icons
        var curFiltered = CuratedIcons.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(cat) && !cat.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            curFiltered = curFiltered.Where(i => i.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            curFiltered = curFiltered.Where(i => i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                                 i.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                                 i.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var curList = curFiltered.ToList();

        RunOnUi(() =>
        {
            DisplayedSystemIcons.Clear();
            foreach (var s in sysList) DisplayedSystemIcons.Add(s);

            DisplayedCuratedIcons.Clear();
            foreach (var c in curList) DisplayedCuratedIcons.Add(c);

            LibraryDisplayedCount = IsSystemTabSelected ? DisplayedSystemIcons.Count : DisplayedCuratedIcons.Count;
        });
    }

    [RelayCommand]
    public void SelectSystemIcon(SystemIconItem? item)
    {
        if (item == null) return;

        SelectedCustomIconPath = item.LibraryPath;
        SelectedCustomIconIndex = item.IconIndex;
        CustomIconPreviewBitmap = item.ThumbnailBitmap;
        IsCustomIconValid = true;
        CustomIconValidationMessage = $"✓ Windows Icon ({item.Title})";
        IsLibraryOpen = false;
    }

    [RelayCommand]
    public async Task SelectWebIconAsync(WebIconItem? item)
    {
        if (item == null) return;

        IsLibraryLoading = true;
        try
        {
            var path = await _libraryService.PrepareWebIconAsync(item);
            if (!string.IsNullOrEmpty(path))
            {
                SelectedCustomIconPath = path;
                SelectedCustomIconIndex = 0;
                CustomIconPreviewBitmap = item.ThumbnailBitmap;
                IsCustomIconValid = true;
                CustomIconValidationMessage = $"✓ {item.Name} Icon ({item.Category})";
                IsLibraryOpen = false;
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError($"Failed to prepare icon {item.Name}", ex);
        }
        finally
        {
            IsLibraryLoading = false;
        }
    }

    [RelayCommand]
    public async Task ApplyCustomIconAsync()
    {
        if (SelectedIcon == null)
        {
            _errorService.Notify("Please select an item from the list to customize.", NotificationSeverity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCustomIconPath) || !IsCustomIconValid)
        {
            _errorService.Notify("Please select a valid .ico file or resource library first.", NotificationSeverity.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _iconService.ApplyCustomIconAsync(SelectedIcon, SelectedCustomIconPath, SelectedCustomIconIndex);
            if (result.Success)
            {
                SelectedIcon.CurrentIconPath = SelectedCustomIconPath;
                SelectedIcon.CurrentIconIndex = SelectedCustomIconIndex;
                SelectedIcon.IsModified = true;
                UpdateCounts();

                _errorService.Notify($"Successfully applied custom icon to '{SelectedIcon.Name}'", NotificationSeverity.Info);
                StatusMessage = $"Custom icon applied to '{SelectedIcon.Name}'.";
            }
            else
            {
                _errorService.Notify($"Failed to apply icon: {result.ErrorMessage}", NotificationSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError($"Error applying icon to {SelectedIcon.Name}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RestoreSelectedAsync()
    {
        if (SelectedIcon == null)
        {
            _errorService.Notify("Please select an item to restore.", NotificationSeverity.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _iconService.RestoreOriginalIconAsync(SelectedIcon);
            if (result.Success)
            {
                SelectedIcon.CurrentIconPath = SelectedIcon.OriginalIconPath;
                SelectedIcon.CurrentIconIndex = SelectedIcon.OriginalIconIndex;
                SelectedIcon.IsModified = false;
                UpdateCounts();

                _errorService.Notify($"Restored '{SelectedIcon.Name}' to original default icon.", NotificationSeverity.Info);
                StatusMessage = $"'{SelectedIcon.Name}' restored to original state.";
            }
            else
            {
                _errorService.Notify($"Failed to restore icon: {result.ErrorMessage}", NotificationSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError($"Error restoring icon for {SelectedIcon.Name}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResetAllToDefaultsAsync()
    {
        if (ModifiedCount == 0)
        {
            _errorService.Notify("No customized desktop icons to reset.", NotificationSeverity.Info);
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _iconService.ResetAllToDefaultsAsync();
            if (result.Success)
            {
                foreach (var icon in DesktopIcons)
                {
                    icon.CurrentIconPath = icon.OriginalIconPath;
                    icon.CurrentIconIndex = icon.OriginalIconIndex;
                    icon.IsModified = false;
                }
                UpdateCounts();

                _errorService.Notify("All customized desktop icons were successfully reset to defaults.", NotificationSeverity.Info);
                StatusMessage = "All icons reset to original defaults.";
            }
            else
            {
                _errorService.Notify($"Reset incomplete: {result.ErrorMessage}", NotificationSeverity.Warning);
            }
        }
        catch (Exception ex)
        {
            _errorService.ReportError("Error resetting icons", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshShellCacheAsync()
    {
        try
        {
            await _iconService.RefreshShellCacheAsync();
            _errorService.Notify("Signaled Windows Explorer to redraw desktop icons.", NotificationSeverity.Info);
        }
        catch (Exception ex)
        {
            _errorService.Notify($"Shell refresh error: {ex.Message}", NotificationSeverity.Warning);
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }
}

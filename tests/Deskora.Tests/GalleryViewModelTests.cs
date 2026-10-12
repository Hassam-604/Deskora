using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Deskora.Tests.Mocks;

namespace Deskora.Tests;

public class GalleryViewModelTests
{
    private readonly MockGalleryProvider _catalogProvider = new();
    private readonly MockDownloadService _downloadService = new();
    private readonly ErrorService _errorService = new();
    private readonly MockWallpaperService _wallpaperService = new();

    private GalleryViewModel CreateViewModel()
    {
        return new GalleryViewModel(_catalogProvider, _downloadService, _errorService, _wallpaperService);
    }

    private void PopulateSampleCatalog()
    {
        _catalogProvider.Entries.Clear();
        _catalogProvider.Entries.AddRange(new[]
        {
            new GalleryEntry
            {
                Id = "nature_01",
                Title = "Alpine Lake",
                Author = "Alice Photographer",
                Source = "Unsplash",
                License = "Unsplash License",
                DownloadUrl = "https://example.com/lake.jpg",
                PreviewUrl = "https://example.com/lake_t.jpg",
                Sha256 = "",
                FileExtension = ".jpg",
                Width = 3840,
                Height = 2160,
                Category = GalleryCategory.Nature,
                Tags = new[] { "mountain", "water", "nature" }
            },
            new GalleryEntry
            {
                Id = "space_01",
                Title = "Orion Nebula",
                Author = "NASA Hubble",
                Source = "NASA",
                License = "CC0",
                DownloadUrl = "https://example.com/orion.png",
                PreviewUrl = "https://example.com/orion_t.png",
                Sha256 = "",
                FileExtension = ".png",
                Width = 2560,
                Height = 1440,
                Category = GalleryCategory.Space,
                Tags = new[] { "space", "stars", "nebula" }
            },
            new GalleryEntry
            {
                Id = "arch_01",
                Title = "Modern Glass Tower",
                Author = "Bob Architect",
                Source = "Pexels",
                License = "Pexels License",
                DownloadUrl = "https://example.com/tower.jpg",
                PreviewUrl = "https://example.com/tower_t.jpg",
                Sha256 = "",
                FileExtension = ".jpg",
                Width = 1920,
                Height = 1080,
                Category = GalleryCategory.Architecture,
                Tags = new[] { "building", "glass", "city" }
            }
        });
    }

    [Fact]
    public async Task InitializeAsync_LoadsCatalogAndUpdatesCounts()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Equal(3, vm.TotalCount);
        Assert.Equal(0, vm.DownloadedCount);
        Assert.Equal(3, vm.DisplayedEntries.Count);
        Assert.False(vm.IsLoading);
        Assert.Empty(vm.LoadError);
    }

    [Fact]
    public async Task InitializeAsync_WhenAssetAlreadyOnDisk_MarksAsDownloaded()
    {
        PopulateSampleCatalog();
        _downloadService.DownloadedIds.Add("space_01");

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.DownloadedCount);
        var rec = vm.GetRecordFor("space_01");
        Assert.Equal(DownloadState.Downloaded, rec.State);
        Assert.NotNull(rec.LocalPath);
    }

    [Fact]
    public async Task CategoryFiltering_FiltersDisplayedEntries()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SetCategoryFilter("Space");

        Assert.Single(vm.DisplayedEntries);
        Assert.Equal("space_01", vm.DisplayedEntries[0].Id);

        vm.SetCategoryFilter("All");
        Assert.Equal(3, vm.DisplayedEntries.Count);
    }

    [Fact]
    public async Task SearchFiltering_MatchesTitleAuthorAndTags()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Search by author
        vm.SearchQuery = "hubble";
        Assert.Single(vm.DisplayedEntries);
        Assert.Equal("space_01", vm.DisplayedEntries[0].Id);

        // Search by tag
        vm.SearchQuery = "glass";
        Assert.Single(vm.DisplayedEntries);
        Assert.Equal("arch_01", vm.DisplayedEntries[0].Id);

        // Clear search
        vm.ClearSearch();
        Assert.Equal(3, vm.DisplayedEntries.Count);
    }

    [Fact]
    public async Task Selection_UpdatesSelectedEntryAndRecord()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        var entry = vm.DisplayedEntries[0];
        vm.SelectEntry(entry);

        Assert.NotNull(vm.SelectedEntry);
        Assert.Equal(entry.Id, vm.SelectedEntry.Id);
        Assert.NotNull(vm.SelectedRecord);
        Assert.Equal(entry.Id, vm.SelectedRecord.EntryId);
    }

    [Fact]
    public async Task DownloadSelectedAsync_SuccessfulDownload_UpdatesStateAndCounts()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        var entry = vm.DisplayedEntries[0];
        vm.SelectEntry(entry);

        await vm.DownloadSelectedAsync();

        Assert.NotNull(vm.SelectedRecord);
        Assert.Equal(DownloadState.Downloaded, vm.SelectedRecord.State);
        Assert.Equal(100, vm.SelectedRecord.ProgressPercent);
        Assert.Equal(1, vm.DownloadedCount);
        Assert.Contains(entry.Id, _downloadService.DownloadedIds);
    }

    [Fact]
    public async Task DownloadSelectedAsync_CorruptedHash_SetsFailedStateAndErrorMessage()
    {
        PopulateSampleCatalog();
        _downloadService.SimulateCorruption = true;

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectEntry(vm.DisplayedEntries[0]);
        await vm.DownloadSelectedAsync();

        Assert.NotNull(vm.SelectedRecord);
        Assert.Equal(DownloadState.Failed, vm.SelectedRecord.State);
        Assert.Contains("Integrity check failed", vm.SelectedRecord.ErrorMessage);
    }

    [Fact]
    public async Task DeleteLocalAsset_DeletesFileAndResetsState()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectEntry(vm.DisplayedEntries[0]);
        await vm.DownloadSelectedAsync();
        Assert.Equal(1, vm.DownloadedCount);

        vm.DeleteLocalAsset();

        Assert.Equal(DownloadState.Available, vm.SelectedRecord!.State);
        Assert.Equal(0, vm.DownloadedCount);
        Assert.Null(vm.SelectedRecord.LocalPath);
    }

    [Fact]
    public async Task SetAsWallpaperAsync_WhenDownloaded_AppliesWallpaper()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectEntry(vm.DisplayedEntries[0]);
        await vm.DownloadSelectedAsync();

        await vm.SetAsWallpaperAsync();

        Assert.Equal(1, _wallpaperService.ApplyWallpaperCallCount);
        Assert.Equal(vm.SelectedRecord!.LocalPath, _wallpaperService.LastAppliedPath);
    }

    [Fact]
    public async Task SetAsWallpaperAsync_WhenNotDownloaded_DoesNotApplyWallpaper()
    {
        PopulateSampleCatalog();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.SelectEntry(vm.DisplayedEntries[0]);

        await vm.SetAsWallpaperAsync();

        Assert.Equal(0, _wallpaperService.ApplyWallpaperCallCount);
    }
}

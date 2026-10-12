using System.Buffers.Binary;
using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Deskora.Tests.Mocks;
using Xunit;

namespace Deskora.Tests;

public class WallpapersViewModelTests : IDisposable
{
    private readonly string _tempTestDir;
    private readonly MockWallpaperService _wallpaperService;
    private readonly MockWallpaperPreferencesService _preferencesService;
    private readonly MockFilePickerService _filePickerService;
    private readonly ErrorService _errorService;

    public WallpapersViewModelTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), $"deskora_vm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempTestDir);

        _wallpaperService = new MockWallpaperService();
        _preferencesService = new MockWallpaperPreferencesService();
        _filePickerService = new MockFilePickerService();
        _errorService = new ErrorService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private string CreateFakeImageFile(string filename, int width = 1920, int height = 1080)
    {
        var path = Path.Combine(_tempTestDir, filename);
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // PNG Header + IHDR
        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x0D });
        writer.Write(System.Text.Encoding.ASCII.GetBytes("IHDR"));
        byte[] w = new byte[4];
        byte[] h = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(w, width);
        BinaryPrimitives.WriteInt32BigEndian(h, height);
        writer.Write(w);
        writer.Write(h);
        writer.Write(new byte[100]); // extra padding bytes

        File.WriteAllBytes(path, ms.ToArray());
        return path;
    }

    [Fact]
    public async Task InitializeAsync_LoadsMonitorsAndPreferences()
    {
        _preferencesService.Preferences.LastFitMode = WallpaperFitMode.Fit;
        _preferencesService.Preferences.LastSnapshot = new WallpaperSnapshot(
            Timestamp: DateTime.UtcNow,
            GlobalWallpaperPath: "C:\\Windows\\Web\\Wallpaper\\original.jpg",
            MonitorWallpapers: new Dictionary<string, string>(),
            Position: WallpaperFitMode.Fill
        );

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();

        Assert.Equal(2, vm.Monitors.Count);
        Assert.Equal(WallpaperFitMode.Fit, vm.SelectedFitMode);
        Assert.True(vm.CanRestore);
        Assert.Contains("original.jpg", vm.RestoreButtonTooltip);
    }

    [Fact]
    public async Task SelectFolderAsync_ValidFolder_UpdatesLibraryAndSavesPreferences()
    {
        var img1 = CreateFakeImageFile("forest.png", 1920, 1080);
        var img2 = CreateFakeImageFile("mountain.png", 2560, 1440);

        _filePickerService.FolderToReturn = _tempTestDir;

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.SelectFolderAsync();

        Assert.Equal(_tempTestDir, vm.CurrentFolderPath);
        Assert.Equal(_tempTestDir, _preferencesService.Preferences.SelectedFolder);
        Assert.True(_preferencesService.SaveCallCount > 0);
        Assert.Equal(2, vm.TotalCount);
        Assert.NotNull(vm.SelectedWallpaper);
    }

    [Fact]
    public async Task AddImagesAsync_ValidImages_AddsToCustomListAndNotifies()
    {
        var img1 = CreateFakeImageFile("sunset.png", 1920, 1080);
        _filePickerService.FilesToReturn.Add(img1);

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.AddImagesAsync();

        Assert.Contains(img1, _preferencesService.Preferences.CustomImagePaths);
        Assert.Equal(1, vm.TotalCount);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Success, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task AddImagesAsync_PreviouslyExcludedImage_UnExcludesAndRestoresToLibrary()
    {
        var img = CreateFakeImageFile("readded_art.png", 2560, 1440);
        _preferencesService.Preferences.ExcludedPaths.Add(img);
        _preferencesService.Preferences.CustomImagePaths.Add(img);

        _filePickerService.FilesToReturn.Add(img);

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        // Since it was excluded, it shouldn't show up initially
        Assert.Equal(0, vm.TotalCount);

        // User explicitly re-adds it
        await vm.AddImagesAsync();

        Assert.DoesNotContain(img, _preferencesService.Preferences.ExcludedPaths);
        Assert.Contains(img, _preferencesService.Preferences.CustomImagePaths);
        Assert.Equal(1, vm.TotalCount);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Success, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_UpdatesStateAndSaves()
    {
        var img = CreateFakeImageFile("galaxy.png", 3840, 2160);
        _preferencesService.Preferences.CustomImagePaths.Add(img);

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        Assert.Single(vm.DisplayedWallpapers);

        var item = vm.DisplayedWallpapers[0];
        Assert.False(item.IsFavorite);
        Assert.Equal(0, vm.FavoritesCount);

        // Toggle ON
        await vm.ToggleFavoriteAsync(item);
        Assert.True(item.IsFavorite);
        Assert.Equal(1, vm.FavoritesCount);
        Assert.Contains(img, _preferencesService.Preferences.FavoritePaths);

        // Toggle OFF
        await vm.ToggleFavoriteAsync(item);
        Assert.False(item.IsFavorite);
        Assert.Equal(0, vm.FavoritesCount);
        Assert.DoesNotContain(img, _preferencesService.Preferences.FavoritePaths);
    }

    [Fact]
    public async Task SearchQuery_FiltersWallpapersByFilename()
    {
        CreateFakeImageFile("aurora_borealis.png");
        CreateFakeImageFile("desert_dunes.png");
        CreateFakeImageFile("ocean_waves.png");

        _preferencesService.Preferences.SelectedFolder = _tempTestDir;

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        Assert.Equal(3, vm.DisplayedWallpapers.Count);

        vm.SearchQuery = "dunes";
        Assert.Single(vm.DisplayedWallpapers);
        Assert.Equal("desert_dunes.png", vm.DisplayedWallpapers[0].FileName);

        vm.ClearSearch();
        Assert.Equal(3, vm.DisplayedWallpapers.Count);
    }

    [Fact]
    public async Task ApplyWallpaperAsync_CapturesSnapshotAndAppliesWallpaper()
    {
        var img = CreateFakeImageFile("cyberpunk_neon.png", 3840, 2160);
        _preferencesService.Preferences.CustomImagePaths.Add(img);

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        vm.SelectedWallpaper = vm.DisplayedWallpapers[0];
        vm.SelectedFitMode = WallpaperFitMode.Stretch;

        await vm.ApplyWallpaperAsync();

        // Must capture snapshot before applying
        Assert.Equal(1, _wallpaperService.CaptureSnapshotCallCount);
        Assert.Equal(1, _wallpaperService.ApplyWallpaperCallCount);
        Assert.Equal(img, _wallpaperService.LastAppliedPath);
        Assert.Equal(WallpaperFitMode.Stretch, _wallpaperService.LastAppliedFitMode);

        Assert.True(vm.CanRestore);
        Assert.NotNull(_preferencesService.Preferences.LastSnapshot);
        Assert.Contains(img, _preferencesService.Preferences.RecentWallpaperPaths);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Success, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task ApplyWallpaperAsync_InvalidWallpaper_ReportsErrorAndDoesNotApply()
    {
        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );
        await vm.InitializeAsync();

        var invalidItem = new WallpaperItem(
            filePath: "C:\\NonExistent\\fake.png",
            fileSizeBytes: 0,
            width: 0,
            height: 0,
            isValid: false,
            errorMessage: "Corrupt file header"
        );

        vm.SelectedWallpaper = invalidItem;

        await vm.ApplyWallpaperAsync();

        Assert.Equal(0, _wallpaperService.ApplyWallpaperCallCount);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Error, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task RestoreWallpaperAsync_WithSnapshot_RestoresPreviousWallpaper()
    {
        var snapshot = new WallpaperSnapshot(
            Timestamp: DateTime.UtcNow,
            GlobalWallpaperPath: "C:\\Windows\\Web\\Wallpaper\\default.jpg",
            MonitorWallpapers: new Dictionary<string, string>(),
            Position: WallpaperFitMode.Center
        );

        _preferencesService.Preferences.LastSnapshot = snapshot;

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        Assert.True(vm.CanRestore);

        await vm.RestoreWallpaperAsync();

        Assert.Equal(1, _wallpaperService.RestoreWallpaperCallCount);
        Assert.Equal(snapshot, _wallpaperService.LastRestoredSnapshot);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Success, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task RestoreWallpaperAsync_WithoutSnapshot_ShowsWarning()
    {
        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        Assert.False(vm.CanRestore);

        await vm.RestoreWallpaperAsync();

        Assert.Equal(0, _wallpaperService.RestoreWallpaperCallCount);
        Assert.NotNull(_errorService.CurrentNotification);
        Assert.Equal(NotificationSeverity.Warning, _errorService.CurrentNotification!.Severity);
    }

    [Fact]
    public async Task RemoveSelectedWallpaperAsync_RemovesFromLibraryAndSavesPreferences()
    {
        var img = CreateFakeImageFile("removable.png", 1920, 1080);
        _preferencesService.Preferences.CustomImagePaths.Add(img);

        var vm = new WallpapersViewModel(
            _wallpaperService,
            _preferencesService,
            _filePickerService,
            _errorService
        );

        await vm.InitializeAsync();
        Assert.Single(vm.DisplayedWallpapers);

        vm.SelectedWallpaper = vm.DisplayedWallpapers[0];
        await vm.RemoveSelectedWallpaperAsync();

        Assert.Empty(vm.DisplayedWallpapers);
        Assert.DoesNotContain(img, _preferencesService.Preferences.CustomImagePaths);
        Assert.Contains(img, _preferencesService.Preferences.ExcludedPaths);
        Assert.True(File.Exists(img)); // Verifies original file on disk was preserved safely
        Assert.NotNull(_errorService.CurrentNotification);
    }
}

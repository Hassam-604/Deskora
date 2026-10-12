using Deskora.Core.Models;
using Deskora.Core.Services;
using Xunit;

namespace Deskora.Tests;

public class WallpaperPreferencesServiceTests
{
    [Fact]
    public async Task SaveAndLoadPreferences_RoundTrip_PreservesAllFields()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"deskora_pref_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonWallpaperPreferencesService(tempFile);

            var original = new WallpaperPreferences
            {
                SelectedFolder = "C:\\Wallpapers\\Nature",
                CustomImagePaths = new List<string> { "C:\\Wallpapers\\custom1.jpg", "C:\\Wallpapers\\custom2.png" },
                FavoritePaths = new List<string> { "C:\\Wallpapers\\custom1.jpg" },
                RecentWallpaperPaths = new List<string> { "C:\\Wallpapers\\recent.jpg" },
                LastFitMode = WallpaperFitMode.Fit,
                LastSnapshot = new WallpaperSnapshot(
                    Timestamp: DateTime.UtcNow,
                    GlobalWallpaperPath: "C:\\Windows\\Web\\Wallpaper\\Theme1\\img1.jpg",
                    MonitorWallpapers: new Dictionary<string, string> { { "mon1", "C:\\img1.jpg" } },
                    Position: WallpaperFitMode.Center
                )
            };

            await service.SavePreferencesAsync(original);
            var loaded = await service.LoadPreferencesAsync();

            Assert.NotNull(loaded);
            Assert.Equal("C:\\Wallpapers\\Nature", loaded.SelectedFolder);
            Assert.Equal(2, loaded.CustomImagePaths.Count);
            Assert.Single(loaded.FavoritePaths);
            Assert.Single(loaded.RecentWallpaperPaths);
            Assert.Equal(WallpaperFitMode.Fit, loaded.LastFitMode);
            Assert.NotNull(loaded.LastSnapshot);
            Assert.Equal("C:\\Windows\\Web\\Wallpaper\\Theme1\\img1.jpg", loaded.LastSnapshot.GlobalWallpaperPath);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadPreferences_NonExistentFile_ReturnsFreshDefaults()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"deskora_missing_{Guid.NewGuid():N}.json");
        var service = new JsonWallpaperPreferencesService(nonExistentPath);

        var prefs = await service.LoadPreferencesAsync();

        Assert.NotNull(prefs);
        Assert.Null(prefs.SelectedFolder);
        Assert.Empty(prefs.CustomImagePaths);
        Assert.Empty(prefs.FavoritePaths);
        Assert.Equal(WallpaperFitMode.Fill, prefs.LastFitMode);
        Assert.Null(prefs.LastSnapshot);
    }
}

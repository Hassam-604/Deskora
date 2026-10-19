using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Tests;

public class JsonIconPreferencesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JsonIconPreferencesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"Deskora_IconPrefs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "icon_preferences.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task LoadPreferencesAsync_WhenFileDoesNotExist_ReturnsCleanDefault()
    {
        var service = new JsonIconPreferencesService(_filePath);
        var prefs = await service.LoadPreferencesAsync();

        Assert.NotNull(prefs);
        Assert.Empty(prefs.Snapshots);
        Assert.Empty(prefs.CustomIcons);
    }

    [Fact]
    public async Task SaveAndLoadPreferencesAsync_PreservesDataCorrectly()
    {
        var service = new JsonIconPreferencesService(_filePath);

        var prefs = new IconPreferences();
        prefs.Snapshots["C:\\Desktop\\app.lnk"] = new IconSnapshot(
            "C:\\Desktop\\app.lnk",
            "C:\\Icons\\original.ico",
            0,
            DateTime.UtcNow
        );
        prefs.CustomIcons["C:\\Desktop\\app.lnk"] = "C:\\Icons\\custom.ico";
        prefs.LastIconDirectory = "C:\\Icons";

        await service.SavePreferencesAsync(prefs);

        var loaded = await service.LoadPreferencesAsync();

        Assert.NotNull(loaded);
        Assert.Single(loaded.Snapshots);
        Assert.True(loaded.Snapshots.ContainsKey("C:\\Desktop\\app.lnk"));
        Assert.Equal("C:\\Icons\\original.ico", loaded.Snapshots["C:\\Desktop\\app.lnk"].OriginalIconPath);
        Assert.Equal("C:\\Icons\\custom.ico", loaded.CustomIcons["C:\\Desktop\\app.lnk"]);
        Assert.Equal("C:\\Icons", loaded.LastIconDirectory);
    }

    [Fact]
    public async Task LoadPreferencesAsync_WhenJsonIsCorrupted_ReturnsCleanDefaultGracefully()
    {
        File.WriteAllText(_filePath, "{ corrupted invalid json :::");

        var service = new JsonIconPreferencesService(_filePath);
        var prefs = await service.LoadPreferencesAsync();

        Assert.NotNull(prefs);
        Assert.Empty(prefs.Snapshots);
    }
}

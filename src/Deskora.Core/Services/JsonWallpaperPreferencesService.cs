using System.Text.Json;
using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// JSON file persistence for wallpaper preferences stored in %LocalAppData%\Deskora.
/// </summary>
public class JsonWallpaperPreferencesService : IWallpaperPreferencesService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonWallpaperPreferencesService(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            _filePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var deskoraDir = Path.Combine(appData, "Deskora");
            _filePath = Path.Combine(deskoraDir, "wallpaper_preferences.json");
        }
    }

    public async Task<WallpaperPreferences> LoadPreferencesAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var preferences = JsonSerializer.Deserialize<WallpaperPreferences>(json, JsonOptions);
                if (preferences != null)
                {
                    return preferences;
                }
            }
        }
        catch
        {
            // If file is corrupted or unreadable, return fresh safe defaults
        }

        return new WallpaperPreferences();
    }

    public async Task SavePreferencesAsync(WallpaperPreferences preferences)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // Fail silently on disk write permission errors
        }
    }
}

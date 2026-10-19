using System.Text.Json;
using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Safe JSON file persistence for icon snapshots and custom assignments in %LocalAppData%\Deskora\icon_preferences.json.
/// </summary>
public sealed class JsonIconPreferencesService : IIconPreferencesService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonIconPreferencesService(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            _filePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var deskoraDir = Path.Combine(appData, "Deskora");
            _filePath = Path.Combine(deskoraDir, "icon_preferences.json");
        }
    }

    public async Task<IconPreferences> LoadPreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, ct);
                var preferences = JsonSerializer.Deserialize<IconPreferences>(json, JsonOptions);
                if (preferences != null)
                {
                    return preferences;
                }
            }
        }
        catch
        {
            // If file is missing or corrupted, return clean default instance
        }

        return new IconPreferences();
    }

    public async Task SavePreferencesAsync(IconPreferences preferences, CancellationToken ct = default)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            var tempPath = $"{_filePath}.tmp";

            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch
        {
            // Best effort persistence
        }
    }
}

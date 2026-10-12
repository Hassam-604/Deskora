using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Deskora.Core.Services;

namespace Deskora.App.Services;

/// <summary>
/// Avalonia 11 implementation of IFilePickerService using native desktop StorageProvider dialogs.
/// </summary>
public class AvaloniaFilePickerService : IFilePickerService
{
    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
        }
        return null;
    }

    public async Task<IReadOnlyList<string>> PickImageFilesAsync()
    {
        var storage = GetStorageProvider();
        if (storage == null) return Array.Empty<string>();

        var options = new FilePickerOpenOptions
        {
            Title = "Select Wallpaper Images — Deskora",
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Supported Images (*.png, *.jpg, *.jpeg, *.bmp, *.webp)")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp" },
                    MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp", "image/webp" }
                },
                new("All Files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        };

        var files = await storage.OpenFilePickerAsync(options);
        return files.Select(f => f.Path.LocalPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        var storage = GetStorageProvider();
        if (storage == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Wallpaper Folder — Deskora",
            AllowMultiple = false
        };

        var folders = await storage.OpenFolderPickerAsync(options);
        return folders.FirstOrDefault()?.Path.LocalPath;
    }
}

namespace Deskora.Core.Services;

/// <summary>
/// Service contract for presenting native file and folder selection dialogs to the user.
/// Keeps ViewModels decoupled from UI window handles and Avalonia dialog specifics.
/// </summary>
public interface IFilePickerService
{
    /// <summary>Prompts user to select one or more image files. Returns paths of selected files or empty list if cancelled.</summary>
    Task<IReadOnlyList<string>> PickImageFilesAsync();

    /// <summary>Prompts user to select a folder containing wallpapers. Returns folder path or null if cancelled.</summary>
    Task<string?> PickFolderAsync();

    /// <summary>Prompts user to select an icon file (.ico, .dll, .exe). Returns file path or null if cancelled.</summary>
    Task<string?> PickIconFileAsync();
}

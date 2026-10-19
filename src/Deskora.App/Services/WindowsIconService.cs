using System.Runtime.InteropServices;
using System.Text;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.App.Services;

/// <summary>
/// Safe Windows desktop icon customization service using official IShellLinkW COM interface,
/// desktop.ini folder styling, and SHChangeNotify Explorer icon cache refreshes.
/// </summary>
public sealed class WindowsIconService : IIconService
{
    private readonly IIconPreferencesService _preferencesService;

    public WindowsIconService(IIconPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public async Task<IReadOnlyList<DesktopIconItem>> GetDesktopIconsAsync(CancellationToken ct = default)
    {
        var items = new List<DesktopIconItem>();

        if (!OperatingSystem.IsWindows())
        {
            return items;
        }

        var preferences = await _preferencesService.LoadPreferencesAsync(ct);

        var desktopDirectories = new List<string>();
        var userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrEmpty(userDesktop) && Directory.Exists(userDesktop))
        {
            desktopDirectories.Add(userDesktop);
        }

        var commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        if (!string.IsNullOrEmpty(commonDesktop) && Directory.Exists(commonDesktop) &&
            !string.Equals(commonDesktop, userDesktop, StringComparison.OrdinalIgnoreCase))
        {
            desktopDirectories.Add(commonDesktop);
        }

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var desktopDir in desktopDirectories)
        {
            // 1. Scan Shortcuts (.lnk)
            try
            {
                var lnkFiles = Directory.EnumerateFiles(desktopDir, "*.lnk", SearchOption.TopDirectoryOnly);
                foreach (var lnkPath in lnkFiles)
                {
                    if (!seenPaths.Add(lnkPath)) continue;

                    try
                    {
                        var (targetPath, iconPath, iconIndex) = ReadShortcutInfo(lnkPath);
                        var name = Path.GetFileNameWithoutExtension(lnkPath);
                        var normalizedKey = Path.GetFullPath(lnkPath);

                        bool isModified = false;
                        string originalIconPath = iconPath;
                        int originalIconIndex = iconIndex;

                        if (preferences.Snapshots.TryGetValue(normalizedKey, out var snapshot))
                        {
                            originalIconPath = snapshot.OriginalIconPath;
                            originalIconIndex = snapshot.OriginalIconIndex;
                            isModified = !string.Equals(iconPath, originalIconPath, StringComparison.OrdinalIgnoreCase) ||
                                         iconIndex != originalIconIndex;
                        }

                        items.Add(new DesktopIconItem
                        {
                            Id = $"lnk_{Guid.NewGuid():N}",
                            Name = name,
                            FilePath = lnkPath,
                            TargetPath = targetPath,
                            CurrentIconPath = iconPath,
                            CurrentIconIndex = iconIndex,
                            OriginalIconPath = originalIconPath,
                            OriginalIconIndex = originalIconIndex,
                            IsModified = isModified,
                            IsFolder = false
                        });

                        // Load icon thumbnail asynchronously without blocking scan
                        if (OperatingSystem.IsWindows())
                        {
                            var item = items[^1];
                            var thumb = WindowsIconThumbnailService.ExtractShortcutIconBitmap(lnkPath, targetPath, iconPath);
                            if (thumb != null) item.ThumbnailBitmap = thumb;
                        }
                    }
                    catch
                    {
                        // Skip corrupted single shortcut
                    }
                }
            }
            catch
            {
                // Ignore enumeration failure
            }

            // 2. Scan Desktop Subfolders
            try
            {
                var subDirs = Directory.EnumerateDirectories(desktopDir, "*", SearchOption.TopDirectoryOnly);
                foreach (var dirPath in subDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    if ((dirInfo.Attributes & FileAttributes.Hidden) != 0 ||
                        (dirInfo.Attributes & FileAttributes.System) != 0)
                    {
                        continue;
                    }

                    if (!seenPaths.Add(dirPath)) continue;

                    var name = dirInfo.Name;
                    var normalizedKey = Path.GetFullPath(dirPath);
                    var (folderIconPath, folderIconIndex) = ReadFolderIconInfo(dirPath);

                    bool isModified = false;
                    string originalIconPath = folderIconPath;
                    int originalIconIndex = folderIconIndex;

                    if (preferences.Snapshots.TryGetValue(normalizedKey, out var snapshot))
                    {
                        originalIconPath = snapshot.OriginalIconPath;
                        originalIconIndex = snapshot.OriginalIconIndex;
                        isModified = !string.Equals(folderIconPath, originalIconPath, StringComparison.OrdinalIgnoreCase);
                    }

                    items.Add(new DesktopIconItem
                    {
                        Id = $"folder_{Guid.NewGuid():N}",
                        Name = name,
                        FilePath = dirPath,
                        TargetPath = dirPath,
                        CurrentIconPath = folderIconPath,
                        CurrentIconIndex = folderIconIndex,
                        OriginalIconPath = originalIconPath,
                        OriginalIconIndex = originalIconIndex,
                        IsModified = isModified,
                        IsFolder = true
                    });

                    // Load folder icon thumbnail
                    if (OperatingSystem.IsWindows())
                    {
                        var item = items[^1];
                        var thumb = WindowsIconThumbnailService.ExtractIconBitmap(dirPath, isFolder: true);
                        if (thumb != null) item.ThumbnailBitmap = thumb;
                    }
                }
            }
            catch
            {
                // Ignore folder enumeration failure
            }
        }

        return items.OrderBy(i => i.IsFolder).ThenBy(i => i.Name).ToList();
    }

    public async Task<IconApplyResult> ApplyCustomIconAsync(
        DesktopIconItem item,
        string iconPath,
        int iconIndex = 0,
        CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new IconApplyResult(false, "Icon customization is supported only on Windows.", item.FilePath);
        }

        if (string.IsNullOrWhiteSpace(item.FilePath) || (!File.Exists(item.FilePath) && !Directory.Exists(item.FilePath)))
        {
            return new IconApplyResult(false, $"Target desktop item does not exist: {item.FilePath}", item.FilePath);
        }

        var validation = IconValidator.Validate(iconPath);
        if (!validation.IsValid)
        {
            return new IconApplyResult(false, validation.ErrorMessage ?? "Invalid icon file.", item.FilePath);
        }

        var preferences = await _preferencesService.LoadPreferencesAsync(ct);
        var normalizedKey = Path.GetFullPath(item.FilePath);

        // Capture baseline snapshot if not already recorded
        if (!preferences.Snapshots.ContainsKey(normalizedKey))
        {
            preferences.Snapshots[normalizedKey] = new IconSnapshot(
                normalizedKey,
                item.OriginalIconPath,
                item.OriginalIconIndex,
                DateTime.UtcNow
            );
        }

        preferences.CustomIcons[normalizedKey] = iconPath;

        try
        {
            if (item.IsFolder)
            {
                ApplyFolderIcon(item.FilePath, iconPath, iconIndex);
            }
            else
            {
                ApplyShortcutIcon(item.FilePath, iconPath, iconIndex);
            }

            await _preferencesService.SavePreferencesAsync(preferences, ct);
            await RefreshShellCacheAsync(ct);

            // Update item thumbnail immediately in UI
            if (OperatingSystem.IsWindows())
            {
                var thumb = WindowsIconThumbnailService.ExtractIconBitmap(iconPath, item.IsFolder);
                if (thumb != null) item.ThumbnailBitmap = thumb;
            }

            return new IconApplyResult(true, ItemPath: item.FilePath);
        }
        catch (Exception ex)
        {
            return new IconApplyResult(false, $"Failed to apply icon: {ex.Message}", item.FilePath);
        }
    }

    public async Task<IconApplyResult> RestoreOriginalIconAsync(DesktopIconItem item, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new IconApplyResult(false, "Icon customization is supported only on Windows.", item.FilePath);
        }

        var preferences = await _preferencesService.LoadPreferencesAsync(ct);
        var normalizedKey = Path.GetFullPath(item.FilePath);

        string targetIconPath = item.OriginalIconPath;
        int targetIconIndex = item.OriginalIconIndex;

        if (preferences.Snapshots.TryGetValue(normalizedKey, out var snapshot))
        {
            targetIconPath = snapshot.OriginalIconPath;
            targetIconIndex = snapshot.OriginalIconIndex;
        }

        try
        {
            if (item.IsFolder)
            {
                RestoreFolderIcon(item.FilePath, targetIconPath);
            }
            else
            {
                ApplyShortcutIcon(item.FilePath, targetIconPath, targetIconIndex);
            }

            preferences.CustomIcons.Remove(normalizedKey);
            await _preferencesService.SavePreferencesAsync(preferences, ct);
            await RefreshShellCacheAsync(ct);

            // Restore thumbnail in UI
            if (OperatingSystem.IsWindows())
            {
                var thumb = item.IsFolder
                    ? WindowsIconThumbnailService.ExtractIconBitmap(item.FilePath, isFolder: true)
                    : WindowsIconThumbnailService.ExtractShortcutIconBitmap(item.FilePath, item.TargetPath, targetIconPath);
                if (thumb != null) item.ThumbnailBitmap = thumb;
            }

            return new IconApplyResult(true, ItemPath: item.FilePath);
        }
        catch (Exception ex)
        {
            return new IconApplyResult(false, $"Failed to restore original icon: {ex.Message}", item.FilePath);
        }
    }

    public async Task<IconApplyResult> ResetAllToDefaultsAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new IconApplyResult(false, "Icon customization is supported only on Windows.");
        }

        var preferences = await _preferencesService.LoadPreferencesAsync(ct);
        int restoredCount = 0;

        foreach (var kvp in preferences.Snapshots)
        {
            var itemPath = kvp.Key;
            var snapshot = kvp.Value;

            try
            {
                if (Directory.Exists(itemPath))
                {
                    RestoreFolderIcon(itemPath, snapshot.OriginalIconPath);
                    restoredCount++;
                }
                else if (File.Exists(itemPath))
                {
                    ApplyShortcutIcon(itemPath, snapshot.OriginalIconPath, snapshot.OriginalIconIndex);
                    restoredCount++;
                }
            }
            catch
            {
                // Continue best effort restore
            }
        }

        preferences.CustomIcons.Clear();
        preferences.Snapshots.Clear();
        await _preferencesService.SavePreferencesAsync(preferences, ct);
        await RefreshShellCacheAsync(ct);

        return new IconApplyResult(true);
    }

    public Task RefreshShellCacheAsync(CancellationToken ct = default)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                // Best effort notification
            }
        }

        return Task.CompletedTask;
    }

    public object? ExtractThumbnail(string path, bool isFolder = false)
    {
        if (!OperatingSystem.IsWindows()) return null;
        return WindowsIconThumbnailService.ExtractIconBitmap(path, isFolder);
    }

    // â”€â”€ Windows COM Helpers for Shortcuts (.lnk) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static (string TargetPath, string IconPath, int IconIndex) ReadShortcutInfo(string lnkPath)
    {
        var link = (IShellLinkW)new ShellLink();
        var persistFile = (IPersistFile)link;

        persistFile.Load(lnkPath, 0);

        var targetSb = new StringBuilder(260);
        link.GetPath(targetSb, targetSb.Capacity, out _, 0);

        var iconSb = new StringBuilder(260);
        link.GetIconLocation(iconSb, iconSb.Capacity, out int iconIndex);

        var iconPath = iconSb.ToString();
        var targetPath = targetSb.ToString();

        // If shortcut doesn't explicitly override icon, it resolves from target
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            iconPath = string.Empty;
            iconIndex = 0;
        }

        return (targetPath, iconPath, iconIndex);
    }

    private static void ApplyShortcutIcon(string lnkPath, string iconPath, int iconIndex)
    {
        var link = (IShellLinkW)new ShellLink();
        var persistFile = (IPersistFile)link;

        persistFile.Load(lnkPath, 0);
        link.SetIconLocation(iconPath, iconIndex);
        persistFile.Save(lnkPath, true);
    }

    // â”€â”€ Windows Helpers for Folders (desktop.ini) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static (string IconPath, int IconIndex) ReadFolderIconInfo(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(iniPath))
        {
            return (string.Empty, 0);
        }

        try
        {
            var lines = File.ReadAllLines(iniPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed["IconResource=".Length..].Trim();
                    var parts = val.Split(',');
                    var p = parts[0].Trim();
                    int idx = parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsed) ? parsed : 0;
                    return (p, idx);
                }
                if (trimmed.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                {
                    var p = trimmed["IconFile=".Length..].Trim();
                    return (p, 0);
                }
            }
        }
        catch
        {
            // Ignore ini read issues
        }

        return (string.Empty, 0);
    }

    private static void ApplyFolderIcon(string folderPath, string iconPath, int iconIndex)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");

        if (File.Exists(iniPath))
        {
            // Remove existing Hidden/System attributes to allow overwrite
            var attrs = File.GetAttributes(iniPath);
            File.SetAttributes(iniPath, attrs & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden & ~FileAttributes.System);
        }

        var content = $"[.ShellClassInfo]\r\nIconResource={iconPath},{iconIndex}\r\n[ViewState]\r\nMode=\r\nVid=\r\nFolderType=Generic\r\n";
        File.WriteAllText(iniPath, content, Encoding.Unicode);

        // Required by Windows Explorer to acknowledge desktop.ini
        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

        var folderInfo = new DirectoryInfo(folderPath);
        folderInfo.Attributes |= FileAttributes.ReadOnly;
    }

    private static void RestoreFolderIcon(string folderPath, string originalIconPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");

        if (string.IsNullOrWhiteSpace(originalIconPath))
        {
            // Delete custom desktop.ini and reset folder attribute
            if (File.Exists(iniPath))
            {
                var attrs = File.GetAttributes(iniPath);
                File.SetAttributes(iniPath, attrs & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden & ~FileAttributes.System);
                File.Delete(iniPath);
            }

            var folderInfo = new DirectoryInfo(folderPath);
            folderInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
        else
        {
            ApplyFolderIcon(folderPath, originalIconPath, 0);
        }
    }

    // â”€â”€ Win32 Shell P/Invoke Definitions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig]
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder ppszFileName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}


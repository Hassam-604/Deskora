using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Deskora.Core.Models;
using Deskora.Core.Services;
using Microsoft.Win32;

namespace Deskora.App.Services;

/// <summary>
/// Windows-specific desktop wallpaper implementation using the IDesktopWallpaper COM interface
/// and SystemParametersInfo fallback.
/// </summary>
public class WindowsWallpaperService : IWallpaperService
{
    private static readonly Guid CLSID_DesktopWallpaper = new("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");
    private static readonly Guid IID_IDesktopWallpaper = new("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPI_GETDESKWALLPAPER = 115;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string? lpvParam, int fuWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int SystemParametersInfo(int uAction, int uParam, [Out] char[] lpvParam, int fuWinIni);

    public bool IsSupported => OperatingSystem.IsWindows();

    public Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync()
    {
        var monitors = new List<MonitorInfo>();

        if (!OperatingSystem.IsWindows())
        {
            monitors.Add(new MonitorInfo("default", 0, "Default Display", "1920 × 1080", true));
            return Task.FromResult<IReadOnlyList<MonitorInfo>>(monitors);
        }

        try
        {
            var wallpaperObj = CreateDesktopWallpaperComObject();
            if (wallpaperObj != null)
            {
                try
                {
                    uint count = wallpaperObj.GetMonitorDevicePathCount();
                    for (uint i = 0; i < count; i++)
                    {
                        string id = wallpaperObj.GetMonitorDevicePathAt(i);
                        wallpaperObj.GetMonitorRECT(id, out RECT rect);
                        int width = Math.Abs(rect.Right - rect.Left);
                        int height = Math.Abs(rect.Bottom - rect.Top);
                        bool isPrimary = i == 0;

                        monitors.Add(new MonitorInfo(
                            Id: id,
                            Index: (int)i,
                            Name: $"Display {i + 1}" + (isPrimary ? " (Primary)" : ""),
                            Resolution: width > 0 && height > 0 ? $"{width} × {height}" : "Standard",
                            IsPrimary: isPrimary
                        ));
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(wallpaperObj);
                }
            }
        }
        catch
        {
            // If COM query fails, provide a fallback standard monitor
        }

        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorInfo("primary", 0, "Primary Display", "1920 × 1080", true));
        }

        return Task.FromResult<IReadOnlyList<MonitorInfo>>(monitors);
    }

    public Task<WallpaperSnapshot> CaptureCurrentSnapshotAsync()
    {
        var monitorMap = new Dictionary<string, string>();
        string? globalPath = null;
        var position = WallpaperFitMode.Fill;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                var wallpaperObj = CreateDesktopWallpaperComObject();
                if (wallpaperObj != null)
                {
                    try
                    {
                        position = (WallpaperFitMode)wallpaperObj.GetPosition();
                        uint count = wallpaperObj.GetMonitorDevicePathCount();
                        for (uint i = 0; i < count; i++)
                        {
                            string monitorId = wallpaperObj.GetMonitorDevicePathAt(i);
                            string path = wallpaperObj.GetWallpaper(monitorId);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                monitorMap[monitorId] = path;
                                globalPath ??= path;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(globalPath))
                        {
                            globalPath = wallpaperObj.GetWallpaper(null);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(wallpaperObj);
                    }
                }
            }
            catch
            {
                // Fallback to Win32 SystemParametersInfo
            }

            if (string.IsNullOrWhiteSpace(globalPath))
            {
                globalPath = GetWallpaperViaSystemParametersInfo();
            }
        }

        var snapshot = new WallpaperSnapshot(
            Timestamp: DateTime.Now,
            GlobalWallpaperPath: globalPath,
            MonitorWallpapers: monitorMap,
            Position: position
        );

        return Task.FromResult(snapshot);
    }

    public Task<WallpaperApplyResult> ApplyWallpaperAsync(string filePath, WallpaperFitMode fitMode, string? monitorId = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new WallpaperApplyResult(false, "Wallpaper application is only supported on Windows."));
        }

        if (!File.Exists(filePath))
        {
            return Task.FromResult(new WallpaperApplyResult(false, $"Wallpaper file not found: {filePath}"));
        }

        try
        {
            // 1. Try modern IDesktopWallpaper COM interface
            var wallpaperObj = CreateDesktopWallpaperComObject();
            if (wallpaperObj != null)
            {
                try
                {
                    wallpaperObj.SetPosition((DESKTOP_WALLPAPER_POSITION)fitMode);
                    wallpaperObj.SetWallpaper(monitorId, filePath);
                }
                finally
                {
                    Marshal.ReleaseComObject(wallpaperObj);
                }
            }

            // 2. Set registry style keys to keep Windows desktop settings in sync
            ApplyFitModeToRegistry(fitMode);

            // 3. Win32 notify broadcast fallback to ensure Explorer repaints immediately
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            return Task.FromResult(new WallpaperApplyResult(
                Success: true,
                Message: "Wallpaper applied successfully.",
                AppliedPath: filePath,
                FitMode: fitMode
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new WallpaperApplyResult(false, $"Failed to apply wallpaper: {ex.Message}"));
        }
    }

    public async Task<WallpaperApplyResult> RestoreWallpaperAsync(WallpaperSnapshot snapshot)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new WallpaperApplyResult(false, "Wallpaper restore is only supported on Windows.");
        }

        try
        {
            // If we have monitor-specific snapshots and COM is available, restore per monitor
            if (snapshot.MonitorWallpapers.Count > 0)
            {
                var wallpaperObj = CreateDesktopWallpaperComObject();
                if (wallpaperObj != null)
                {
                    try
                    {
                        wallpaperObj.SetPosition((DESKTOP_WALLPAPER_POSITION)snapshot.Position);
                        foreach (var kvp in snapshot.MonitorWallpapers)
                        {
                            if (File.Exists(kvp.Value))
                            {
                                wallpaperObj.SetWallpaper(kvp.Key, kvp.Value);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(wallpaperObj);
                    }
                }
            }

            // Also restore global wallpaper if available
            if (!string.IsNullOrWhiteSpace(snapshot.GlobalWallpaperPath) && File.Exists(snapshot.GlobalWallpaperPath))
            {
                await ApplyWallpaperAsync(snapshot.GlobalWallpaperPath, snapshot.Position);
            }

            return new WallpaperApplyResult(
                Success: true,
                Message: "Wallpaper reverted to previous snapshot.",
                AppliedPath: snapshot.GlobalWallpaperPath,
                FitMode: snapshot.Position
            );
        }
        catch (Exception ex)
        {
            return new WallpaperApplyResult(false, $"Failed to restore wallpaper: {ex.Message}");
        }
    }

    public Task<string?> GetCurrentWallpaperPathAsync(string? monitorId = null)
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult<string?>(null);

        try
        {
            var wallpaperObj = CreateDesktopWallpaperComObject();
            if (wallpaperObj != null)
            {
                try
                {
                    string path = wallpaperObj.GetWallpaper(monitorId);
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        return Task.FromResult<string?>(path);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(wallpaperObj);
                }
            }
        }
        catch
        {
            // Fallback
        }

        string? fallback = GetWallpaperViaSystemParametersInfo();
        return Task.FromResult(fallback);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyFitModeToRegistry(WallpaperFitMode fitMode)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (key == null) return;

            // Values according to Microsoft documentation for Desktop Wallpaper Position:
            // Fill: WallpaperStyle = "10", TileWallpaper = "0"
            // Fit: WallpaperStyle = "6", TileWallpaper = "0"
            // Stretch: WallpaperStyle = "2", TileWallpaper = "0"
            // Tile: WallpaperStyle = "0", TileWallpaper = "1"
            // Center: WallpaperStyle = "0", TileWallpaper = "0"
            // Span: WallpaperStyle = "22", TileWallpaper = "0"
            var (style, tile) = fitMode switch
            {
                WallpaperFitMode.Fill => ("10", "0"),
                WallpaperFitMode.Fit => ("6", "0"),
                WallpaperFitMode.Stretch => ("2", "0"),
                WallpaperFitMode.Tile => ("0", "1"),
                WallpaperFitMode.Center => ("0", "0"),
                WallpaperFitMode.Span => ("22", "0"),
                _ => ("10", "0")
            };

            key.SetValue("WallpaperStyle", style);
            key.SetValue("TileWallpaper", tile);
        }
        catch
        {
            // Best effort registry sync
        }
    }

    private static string? GetWallpaperViaSystemParametersInfo()
    {
        try
        {
            var buffer = new char[260];
            if (SystemParametersInfo(SPI_GETDESKWALLPAPER, buffer.Length, buffer, 0) != 0)
            {
                string path = new string(buffer).TrimEnd('\0');
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return path;
                }
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    [SupportedOSPlatform("windows")]
    private static IDesktopWallpaper? CreateDesktopWallpaperComObject()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(CLSID_DesktopWallpaper);
            if (type == null) return null;
            return Activator.CreateInstance(type) as IDesktopWallpaper;
        }
        catch
        {
            return null;
        }
    }

    #region Native COM Definitions

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        uint GetMonitorDevicePathCount();
        void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID, out RECT displayRect);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(DESKTOP_WALLPAPER_POSITION position);
        DESKTOP_WALLPAPER_POSITION GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, uint direction);
        uint GetStatus();
        bool Enable();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private enum DESKTOP_WALLPAPER_POSITION
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5
    }

    #endregion
}

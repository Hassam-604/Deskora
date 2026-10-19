using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Media.Imaging;

namespace Deskora.App.Services;

/// <summary>
/// Extracts icon thumbnails for desktop shortcuts and folders using Windows Shell APIs
/// and direct binary ICO decoders for modern PNG-compressed icon assets.
/// </summary>
public static class WindowsIconThumbnailService
{
    private const int SHGFI_ICON = 0x100;
    private const int SHGFI_LARGEICON = 0x0;
    private const int SHGFI_USEFILEATTRIBUTES = 0x10;
    private const int FILE_ATTRIBUTE_NORMAL = 0x80;
    private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi,
        uint cbFileInfo, uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int ExtractIconEx(
        string szFileName, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Gets total count of icon resources in a .dll or .exe.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static int ExtractIconCount(string libraryPath)
    {
        if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath)) return 0;
        try
        {
            return ExtractIconEx(libraryPath, -1, null, null, 0);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Extracts an icon at a specific index from a library (.dll or .exe).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap? ExtractIconFromLibrary(string libraryPath, int iconIndex)
    {
        if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath)) return null;

        var large = new IntPtr[1];
        try
        {
            int count = ExtractIconEx(libraryPath, iconIndex, large, null, 1);
            if (count > 0 && large[0] != IntPtr.Zero)
            {
                return HIconToAvaloniaBitmap(large[0]);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (large[0] != IntPtr.Zero)
            {
                DestroyIcon(large[0]);
            }
        }

        return null;
    }

    /// <summary>
    /// Directly decodes an .ico file, supporting both legacy BMP-based and modern 256x256 PNG-compressed icons (e.g. Ollama, Jupyter).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap? ExtractFromIcoFile(string icoPath)
    {
        if (string.IsNullOrWhiteSpace(icoPath) || !File.Exists(icoPath)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(icoPath);
            if (bytes.Length < 22) return null;

            ushort type = BitConverter.ToUInt16(bytes, 2);
            ushort count = BitConverter.ToUInt16(bytes, 4);
            if (type != 1 || count == 0) return null;

            // Find best resolution entry
            int bestIdx = 0;
            int maxArea = 0;
            for (int i = 0; i < count; i++)
            {
                int entryOffset = 6 + (i * 16);
                if (entryOffset + 16 > bytes.Length) break;
                int w = bytes[entryOffset];
                int h = bytes[entryOffset + 1];
                if (w == 0) w = 256;
                if (h == 0) h = 256;
                int area = w * h;
                if (area > maxArea)
                {
                    maxArea = area;
                    bestIdx = i;
                }
            }

            int bestEntryOffset = 6 + (bestIdx * 16);
            uint imgSize = BitConverter.ToUInt32(bytes, bestEntryOffset + 8);
            uint imgOffset = BitConverter.ToUInt32(bytes, bestEntryOffset + 12);

            if (imgOffset + imgSize <= bytes.Length)
            {
                // Check if PNG-compressed (0x89, 'P', 'N', 'G')
                if (imgSize > 8 && bytes[imgOffset] == 0x89 && bytes[imgOffset + 1] == 0x50 &&
                    bytes[imgOffset + 2] == 0x4E && bytes[imgOffset + 3] == 0x47)
                {
                    using var ms = new MemoryStream(bytes, (int)imgOffset, (int)imgSize);
                    return new Bitmap(ms);
                }
            }

            // Fallback for standard GDI BMP-based .ico
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                using var fs = new MemoryStream(bytes);
                using var icon = new System.Drawing.Icon(fs);
                using var gdiBitmap = icon.ToBitmap();
                using var pngMs = new MemoryStream();
                gdiBitmap.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
                pngMs.Seek(0, SeekOrigin.Begin);
                return new Bitmap(pngMs);
            }
        }
        catch
        {
            // Fall through to shell extract
        }

        return null;
    }

    /// <summary>
    /// Extracts a large icon bitmap for the given file or folder path using SHGetFileInfo.
    /// Returns null if extraction fails or not on Windows.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap? ExtractIconBitmap(string filePath, bool isFolder)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        if (!isFolder && filePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) && File.Exists(filePath))
        {
            var icoBmp = ExtractFromIcoFile(filePath);
            if (icoBmp != null) return icoBmp;
        }

        var shfi = new SHFILEINFO();
        uint flags = (uint)(SHGFI_ICON | SHGFI_LARGEICON);
        uint fileAttr = isFolder ? (uint)FILE_ATTRIBUTE_DIRECTORY : (uint)FILE_ATTRIBUTE_NORMAL;

        bool pathExists = File.Exists(filePath) || Directory.Exists(filePath);
        if (!pathExists)
        {
            flags |= SHGFI_USEFILEATTRIBUTES;
        }

        try
        {
            var result = SHGetFileInfo(filePath, fileAttr, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
                return null;

            return HIconToAvaloniaBitmap(shfi.hIcon);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (shfi.hIcon != IntPtr.Zero)
                DestroyIcon(shfi.hIcon);
        }
    }

    /// <summary>
    /// Extracts icon for a .lnk shortcut: tries explicit icon override path, then target .exe, then .lnk shell icon.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap? ExtractShortcutIconBitmap(string lnkPath, string targetPath, string iconPath)
    {
        // 1. If explicit icon path exists (.ico, .exe, .dll)
        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            if (iconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                var icoBmp = ExtractFromIcoFile(iconPath);
                if (icoBmp != null) return icoBmp;
            }
            else
            {
                var b = ExtractIconBitmap(iconPath, false);
                if (b != null) return b;
            }
        }

        // 2. If target path exists (.exe, .bat, etc.)
        if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
        {
            if (targetPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                var icoBmp = ExtractFromIcoFile(targetPath);
                if (icoBmp != null) return icoBmp;
            }
            else
            {
                var b = ExtractIconBitmap(targetPath, false);
                if (b != null) return b;
            }
        }

        // 3. Fallback to extracting from the .lnk shortcut file directly
        return ExtractIconBitmap(lnkPath, false);
    }

    [SupportedOSPlatform("windows")]
    private static Bitmap? HIconToAvaloniaBitmap(IntPtr hIcon)
    {
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(hIcon);
            using var gdiBitmap = icon.ToBitmap();
            using var ms = new MemoryStream();
            gdiBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Wraps PNG image bytes into a valid Windows .ico file.
    /// </summary>
    public static void SavePngAsIco(byte[] pngBytes, int width, int height, string targetIcoPath)
    {
        var dir = Path.GetDirectoryName(targetIcoPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fs = new FileStream(targetIcoPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // ICONDIR
        bw.Write((ushort)0); // Reserved
        bw.Write((ushort)1); // Type 1 = ICO
        bw.Write((ushort)1); // 1 Image

        // ICONDIRENTRY
        bw.Write((byte)(width >= 256 ? 0 : width));
        bw.Write((byte)(height >= 256 ? 0 : height));
        bw.Write((byte)0);   // ColorCount
        bw.Write((byte)0);   // Reserved
        bw.Write((ushort)1); // Color planes
        bw.Write((ushort)32);// Bits per pixel
        bw.Write((uint)pngBytes.Length); // Image size in bytes
        bw.Write((uint)22);  // Image offset (6 header + 16 entry)

        // Image payload (PNG bytes)
        bw.Write(pngBytes);
        bw.Flush();
    }
}

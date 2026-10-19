using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.App.Services;

/// <summary>
/// Service providing access to native Windows system icons and curated desktop application/folder icons.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsIconLibraryService : IIconLibraryService
{
    private static readonly List<SystemIconItem> _systemIconsCache = new();
    private static readonly object _lock = new();
    private readonly string _cacheDirectory;

    public WindowsIconLibraryService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Deskora",
            "Icons",
            "Curated");

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public Task<IReadOnlyList<SystemIconItem>> GetSystemIconsAsync(string? category = null, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<SystemIconItem>>(Array.Empty<SystemIconItem>());
        }

        lock (_lock)
        {
            if (_systemIconsCache.Count == 0)
            {
                DiscoverSystemIcons();
            }
        }

        var results = _systemIconsCache.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            results = results.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<SystemIconItem>>(results.ToList());
    }

    [SupportedOSPlatform("windows")]
    private void DiscoverSystemIcons()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var libraries = new[]
        {
            ("imageres.dll", "Windows Modern"),
            ("shell32.dll", "Windows Classic"),
            ("ddores.dll", "Hardware & Devices")
        };

        foreach (var (fileName, friendlyName) in libraries)
        {
            var libPath = Path.Combine(system32, fileName);
            if (!File.Exists(libPath)) continue;

            int count = WindowsIconThumbnailService.ExtractIconCount(libPath);
            if (count <= 0) continue;

            for (int i = 0; i < count; i++)
            {
                string category = DetermineCategory(fileName, i);
                string title = $"{Path.GetFileNameWithoutExtension(fileName)} #{i}";

                var item = new SystemIconItem
                {
                    Id = $"{fileName}_{i}",
                    Title = title,
                    LibraryPath = libPath,
                    LibraryName = friendlyName,
                    IconIndex = i,
                    Category = category
                };

                var thumb = WindowsIconThumbnailService.ExtractIconFromLibrary(libPath, i);
                if (thumb != null)
                {
                    item.ThumbnailBitmap = thumb;
                    _systemIconsCache.Add(item);
                }
            }
        }
    }

    private static string DetermineCategory(string libraryName, int index)
    {
        var lib = libraryName.ToLowerInvariant();
        if (lib.Contains("ddores")) return "Devices";

        if (lib.Contains("imageres"))
        {
            if (index is >= 2 and <= 15 or 112 or 113 or 114 or 175 or 176) return "Folders";
            if (index is >= 20 and <= 45 or 105 or 106) return "Devices";
            if (index is 59 or 68 or 73 or 108 or 109 or 189) return "Media";
            if (index is >= 50 and <= 80 or 100 or 101) return "System";
            return "Apps";
        }

        if (lib.Contains("shell32"))
        {
            if (index is 3 or 4 or 19 or 29 or 45 or 46 or 126 or 127) return "Folders";
            if (index is >= 7 and <= 13 or 27 or 28) return "Devices";
            if (index is 21 or 22 or 23 or 24 or 35 or 44) return "System";
            if (index is 14 or 15 or 16 or 17 or 18) return "Media";
            return "Apps";
        }

        return "Apps";
    }

    public Task<IReadOnlyList<WebIconItem>> GetCuratedIconsAsync(string? category = null, CancellationToken ct = default)
    {
        var items = GetCuratedIconDefinitions();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            items = items.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Task.FromResult<IReadOnlyList<WebIconItem>>(items);
    }

    public async Task<IReadOnlyList<WebIconItem>> SearchIconsAsync(string query, CancellationToken ct = default)
    {
        var curated = await GetCuratedIconsAsync(null, ct);
        if (string.IsNullOrWhiteSpace(query)) return curated;

        var q = query.Trim();
        return curated.Where(i =>
            i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            i.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            i.Description.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public Task<string?> PrepareWebIconAsync(WebIconItem item, CancellationToken ct = default)
    {
        if (File.Exists(item.LocalCachedPath))
        {
            return Task.FromResult<string?>(item.LocalCachedPath);
        }

        try
        {
            var targetIco = Path.Combine(_cacheDirectory, $"{item.Id}.ico");
            if (File.Exists(targetIco))
            {
                item.LocalCachedPath = targetIco;
                return Task.FromResult<string?>(targetIco);
            }

            GenerateCuratedIco(item, targetIco);
            item.LocalCachedPath = targetIco;
            return Task.FromResult<string?>(targetIco);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private List<WebIconItem> GetCuratedIconDefinitions()
    {
        var defs = new List<WebIconItem>
        {
            // Applications & Dev
            new() { Id = "app_chrome", Name = "Google Chrome", Category = "Apps", Description = "Clean modern browser icon" },
            new() { Id = "app_discord", Name = "Discord", Category = "Apps", Description = "Gaming and community chat" },
            new() { Id = "app_spotify", Name = "Spotify", Category = "Apps", Description = "Music streaming glyph" },
            new() { Id = "app_steam", Name = "Steam", Category = "Apps", Description = "Gaming client and library" },
            new() { Id = "app_vscode", Name = "VS Code", Category = "Apps", Description = "Code editor and IDE" },
            new() { Id = "app_github", Name = "GitHub", Category = "Apps", Description = "Version control and repositories" },
            new() { Id = "app_terminal", Name = "Terminal / CLI", Category = "Apps", Description = "PowerShell and developer shell" },
            new() { Id = "app_python", Name = "Python", Category = "Apps", Description = "Python programming language" },
            new() { Id = "app_youtube", Name = "YouTube", Category = "Media", Description = "Video streaming platform" },
            new() { Id = "app_obsidian", Name = "Obsidian", Category = "Apps", Description = "Knowledge base and markdown notes" },
            new() { Id = "app_blender", Name = "Blender 3D", Category = "Apps", Description = "3D creation and graphics suite" },
            new() { Id = "app_games", Name = "Game Center", Category = "Apps", Description = "Arcade & controller launcher" },

            // Aesthetic Folder Styles
            new() { Id = "folder_cyan", Name = "Neon Cyan Folder", Category = "Folders", Description = "Electric cyan folder glyph" },
            new() { Id = "folder_amber", Name = "Sunset Amber Folder", Category = "Folders", Description = "Warm golden amber folder" },
            new() { Id = "folder_emerald", Name = "Emerald Green Folder", Category = "Folders", Description = "Lush modern emerald folder" },
            new() { Id = "folder_purple", Name = "Cyber Purple Folder", Category = "Folders", Description = "Vibrant deep violet folder" },
            new() { Id = "folder_dark", Name = "Minimalist Dark Folder", Category = "Folders", Description = "Sleek obsidian monochrome folder" },
            new() { Id = "folder_ruby", Name = "Ruby Red Folder", Category = "Folders", Description = "Bold crimson red directory" },
        };

        foreach (var def in defs)
        {
            var targetIco = Path.Combine(_cacheDirectory, $"{def.Id}.ico");
            if (!File.Exists(targetIco))
            {
                GenerateCuratedIco(def, targetIco);
            }
            def.LocalCachedPath = targetIco;

            if (File.Exists(targetIco))
            {
                def.ThumbnailBitmap = WindowsIconThumbnailService.ExtractFromIcoFile(targetIco);
            }
        }

        return defs;
    }

    private static void GenerateCuratedIco(WebIconItem item, string targetIcoPath)
    {
        const int size = 128;
        using var bmp = new System.Drawing.Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        var (bgCol1, bgCol2, symbol, symCol) = GetStyleForId(item.Id);

        if (item.Category == "Folders")
        {
            // Draw folder base
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, size, size), bgCol1, bgCol2, 45f);
            using var path = new GraphicsPath();
            // Folder tab
            path.AddRoundedRectangle(new RectangleF(12, 24, 44, 20), 8);
            // Folder body
            path.AddRoundedRectangle(new RectangleF(12, 34, 104, 76), 14);
            g.FillPath(brush, path);

            // Front folder fold
            using var frontBrush = new LinearGradientBrush(new Rectangle(0, 48, size, 60), Color.FromArgb(220, bgCol2), bgCol1, 90f);
            using var frontPath = new GraphicsPath();
            frontPath.AddRoundedRectangle(new RectangleF(12, 48, 104, 62), 12);
            g.FillPath(frontBrush, frontPath);
        }
        else
        {
            // App Squircle
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, size, size), bgCol1, bgCol2, 45f);
            using var path = new GraphicsPath();
            path.AddRoundedRectangle(new RectangleF(14, 14, 100, 100), 24);
            g.FillPath(brush, path);

            // Inner symbol / label
            using var font = new Font(FontFamily.GenericSansSerif, 32, FontStyle.Bold);
            using var textBrush = new SolidBrush(symCol);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(symbol, font, textBrush, new RectangleF(14, 14, 100, 100), sf);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var bytes = ms.ToArray();
        WindowsIconThumbnailService.SavePngAsIco(bytes, size, size, targetIcoPath);
    }

    private static (Color, Color, string, Color) GetStyleForId(string id) => id switch
    {
        "app_chrome" => (Color.FromArgb(234, 67, 53), Color.FromArgb(66, 133, 244), "C", Color.White),
        "app_discord" => (Color.FromArgb(88, 101, 242), Color.FromArgb(71, 82, 196), "D", Color.White),
        "app_spotify" => (Color.FromArgb(30, 215, 96), Color.FromArgb(20, 150, 70), "S", Color.White),
        "app_steam" => (Color.FromArgb(23, 29, 37), Color.FromArgb(102, 192, 244), "ST", Color.White),
        "app_vscode" => (Color.FromArgb(0, 122, 204), Color.FromArgb(0, 90, 160), "VS", Color.White),
        "app_github" => (Color.FromArgb(36, 41, 46), Color.FromArgb(16, 18, 20), "GH", Color.White),
        "app_terminal" => (Color.FromArgb(12, 12, 12), Color.FromArgb(40, 40, 40), ">_", Color.FromArgb(0, 255, 128)),
        "app_python" => (Color.FromArgb(55, 118, 171), Color.FromArgb(255, 212, 59), "Py", Color.White),
        "app_youtube" => (Color.FromArgb(255, 0, 0), Color.FromArgb(180, 0, 0), "▶", Color.White),
        "app_obsidian" => (Color.FromArgb(117, 63, 194), Color.FromArgb(72, 33, 130), "OB", Color.White),
        "app_blender" => (Color.FromArgb(232, 125, 13), Color.FromArgb(40, 83, 140), "3D", Color.White),
        "app_games" => (Color.FromArgb(255, 87, 34), Color.FromArgb(233, 30, 99), "🎮", Color.White),
        "folder_cyan" => (Color.FromArgb(0, 210, 255), Color.FromArgb(58, 123, 213), "", Color.White),
        "folder_amber" => (Color.FromArgb(246, 211, 101), Color.FromArgb(253, 160, 133), "", Color.White),
        "folder_emerald" => (Color.FromArgb(17, 153, 142), Color.FromArgb(56, 239, 125), "", Color.White),
        "folder_purple" => (Color.FromArgb(142, 45, 226), Color.FromArgb(74, 0, 224), "", Color.White),
        "folder_dark" => (Color.FromArgb(67, 67, 67), Color.FromArgb(24, 24, 24), "", Color.White),
        "folder_ruby" => (Color.FromArgb(235, 51, 73), Color.FromArgb(244, 92, 67), "", Color.White),
        _ => (Color.FromArgb(99, 102, 241), Color.FromArgb(67, 56, 202), "★", Color.White)
    };
}

[SupportedOSPlatform("windows")]
public static class GraphicsPathExtensions
{
    public static void AddRoundedRectangle(this GraphicsPath path, RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
    }
}

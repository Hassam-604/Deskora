using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.Models;

/// <summary>
/// Represents a local wallpaper image with metadata and user preference state.
/// </summary>
public partial class WallpaperItem : ObservableObject
{
    public string FilePath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public long FileSizeBytes { get; }
    public string FileSizeFormatted { get; }
    public int Width { get; }
    public int Height { get; }
    public string ResolutionFormatted { get; }
    public string AspectRatio { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private DateTime? _lastApplied;

    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string? _errorMessage;

    public WallpaperItem(
        string filePath,
        long fileSizeBytes,
        int width,
        int height,
        bool isFavorite = false,
        DateTime? lastApplied = null,
        bool isValid = true,
        string? errorMessage = null)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Extension = Path.GetExtension(filePath).ToLowerInvariant();
        FileSizeBytes = fileSizeBytes;
        FileSizeFormatted = FormatBytes(fileSizeBytes);
        Width = width;
        Height = height;
        ResolutionFormatted = width > 0 && height > 0 ? $"{width} × {height}" : "Unknown";
        AspectRatio = CalculateAspectRatio(width, height);
        _isFavorite = isFavorite;
        _lastApplied = lastApplied;
        _isValid = isValid;
        _errorMessage = errorMessage;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }

    private static string CalculateAspectRatio(int width, int height)
    {
        if (width <= 0 || height <= 0) return "Unknown";
        double ratio = (double)width / height;

        // Check common standard aspect ratios with reasonable tolerance
        if (Math.Abs(ratio - (16.0 / 9.0)) < 0.04) return "16:9 Landscape";
        if (Math.Abs(ratio - (16.0 / 10.0)) < 0.04) return "16:10 Wide";
        if (Math.Abs(ratio - (21.0 / 9.0)) < 0.05) return "21:9 Ultrawide";
        if (Math.Abs(ratio - (32.0 / 9.0)) < 0.06) return "32:9 Super Ultrawide";
        if (Math.Abs(ratio - (3.0 / 2.0)) < 0.04) return "3:2 Photo";
        if (Math.Abs(ratio - (4.0 / 3.0)) < 0.04) return "4:3 Standard";
        if (Math.Abs(ratio - (5.0 / 4.0)) < 0.04) return "5:4 Classic";
        if (Math.Abs(ratio - 1.0) < 0.04) return "1:1 Square";
        if (Math.Abs(ratio - (9.0 / 16.0)) < 0.04) return "9:16 Portrait";
        if (Math.Abs(ratio - (3.0 / 4.0)) < 0.04) return "3:4 Portrait";

        // For non-standard dimensions, use clean simplified ratios or decimal representation
        int gcd = Gcd(width, height);
        int num = width / gcd;
        int den = height / gcd;

        if (num <= 16 && den <= 16)
        {
            return $"{num}:{den}";
        }

        if (ratio > 1.2)
        {
            return $"{ratio:0.##}:1 Wide";
        }
        if (ratio < 0.85)
        {
            return $"{ratio:0.##}:1 Tall";
        }

        return $"{ratio:0.##}:1";
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
}

using System.Security.Cryptography;
using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Safe, sandboxed download service for curated gallery assets.
/// Enforces:
/// - Explicit HTTPS scheme only
/// - Allowed image file extensions
/// - Maximum download size limit
/// - Download to temporary file followed by atomic move
/// - Optional SHA-256 integrity verification
/// - Proper cleanup on error or user cancellation
/// </summary>
public sealed class HttpDownloadService : IDownloadService, IDisposable
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp"
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly string _storageDir;
    private readonly long _maxFileSizeBytes;

    public string GalleryStorageDirectory => _storageDir;
    public long MaxFileSizeBytes => _maxFileSizeBytes;

    public HttpDownloadService(
        string? storageDirectory = null,
        HttpClient? httpClient = null,
        long maxFileSizeBytes = 50 * 1024 * 1024) // 50 MB default cap
    {
        _storageDir = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Deskora",
            "Gallery");

        Directory.CreateDirectory(_storageDir);

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Deskora/0.3.0");
            }
            catch
            {
                // Ignore header parsing fallback
            }
            _ownsHttpClient = true;
        }

        _maxFileSizeBytes = maxFileSizeBytes;
    }

    public string? GetLocalPath(GalleryEntry entry)
    {
        var sanitizedExt = SanitizeExtension(entry.FileExtension);
        var expectedPath = Path.Combine(_storageDir, $"{entry.Id}{sanitizedExt}");
        return File.Exists(expectedPath) ? expectedPath : null;
    }

    public bool DeleteLocalAsset(GalleryEntry entry)
    {
        var path = GetLocalPath(entry);
        if (path != null && File.Exists(path))
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    public long GetStorageUsedBytes()
    {
        if (!Directory.Exists(_storageDir)) return 0;

        try
        {
            return Directory.EnumerateFiles(_storageDir)
                .Sum(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0L; }
                });
        }
        catch
        {
            return 0;
        }
    }

    public async Task<DownloadResult> DownloadEntryAsync(
        GalleryEntry entry,
        IProgress<double> progress,
        CancellationToken ct)
    {
        // 1. Validate destination extension
        var ext = SanitizeExtension(entry.FileExtension);
        if (!AllowedExtensions.Contains(ext))
        {
            return new DownloadResult(false, ErrorMessage: $"Disallowed file extension '{ext}'. Only image files are permitted.");
        }

        // 2. Validate URL
        if (!Uri.TryCreate(entry.DownloadUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return new DownloadResult(false, ErrorMessage: "Download rejected: URL must use secure HTTPS.");
        }

        var destinationPath = Path.Combine(_storageDir, $"{entry.Id}{ext}");
        var tempPath = Path.Combine(_storageDir, $"{entry.Id}_{Guid.NewGuid():N}.tmp");

        try
        {
            // 3. Initiate HTTP request with stream
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                return new DownloadResult(false, ErrorMessage: $"Server responded with {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _maxFileSizeBytes)
            {
                return new DownloadResult(false, ErrorMessage: $"File exceeds safety size limit of {_maxFileSizeBytes / 1_048_576} MB.");
            }

            // 4. Stream to temporary file
            await using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead > _maxFileSizeBytes)
                    {
                        return new DownloadResult(false, ErrorMessage: $"Download exceeded size limit of {_maxFileSizeBytes / 1_048_576} MB during transfer.");
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);

                    if (contentLength.HasValue && contentLength.Value > 0)
                    {
                        var pct = Math.Min(100.0, (double)totalBytesRead / contentLength.Value * 100.0);
                        progress.Report(pct);
                    }
                }
            }

            // 5. Integrity verification (SHA-256) if expected hash is provided
            if (!string.IsNullOrWhiteSpace(entry.Sha256))
            {
                var calculatedHash = await ComputeSha256Async(tempPath, ct);
                if (!string.Equals(calculatedHash, entry.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    SafeDelete(tempPath);
                    return new DownloadResult(false, WasCorrupted: true, ErrorMessage: "Checksum mismatch: downloaded file was corrupted or altered.");
                }
            }

            // 6. Atomic move to destination
            File.Move(tempPath, destinationPath, overwrite: true);
            progress.Report(100.0);

            return new DownloadResult(true, LocalPath: destinationPath);
        }
        catch (OperationCanceledException)
        {
            SafeDelete(tempPath);
            return new DownloadResult(false, WasCancelled: true);
        }
        catch (Exception ex)
        {
            SafeDelete(tempPath);
            return new DownloadResult(false, ErrorMessage: ex.Message);
        }
    }

    private static string SanitizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return ".jpg";
        var trimmed = ext.Trim();
        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Ignore temporary deletion failure
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}

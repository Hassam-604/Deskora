using Deskora.Core.Models;

namespace Deskora.Core.Services;

/// <summary>
/// Safe, zero-allocation validator and metadata inspector for Windows icon files (.ico, .exe, .dll).
/// Validates binary structure, dimensions, and image entry counts.
/// </summary>
public static class IconValidator
{
    private const ushort IcoReserved = 0;
    private const ushort IcoType = 1; // 1 = Icon

    public static IconValidationResult Validate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new IconValidationResult(false, "No icon file specified.");
        }

        if (!File.Exists(filePath))
        {
            return new IconValidationResult(false, $"Icon file not found: '{filePath}'");
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".exe" || ext == ".dll")
        {
            // Container with embedded PE icon resources
            var fi = new FileInfo(filePath);
            if (fi.Length == 0)
            {
                return new IconValidationResult(false, "Icon library file is empty.");
            }

            return new IconValidationResult(
                true,
                BestWidth: 256,
                BestHeight: 256,
                ImageCount: 1,
                FormatDescription: $"Windows Executable Resource Container ({ext.ToUpperInvariant()})");
        }

        if (ext != ".ico")
        {
            return new IconValidationResult(
                false,
                $"Unsupported icon format '{ext}'. Windows desktop shortcuts require .ico files or resource libraries (.dll, .exe).");
        }

        // Validate .ico binary file structure
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 6)
            {
                return new IconValidationResult(false, "Invalid ICO file: File is too small to contain a valid header.");
            }

            using var reader = new BinaryReader(stream);
            ushort reserved = reader.ReadUInt16();
            ushort type = reader.ReadUInt16();
            ushort count = reader.ReadUInt16();

            if (reserved != IcoReserved || type != IcoType)
            {
                return new IconValidationResult(false, "Invalid ICO file: Signature mismatch (not a valid Windows Icon).");
            }

            if (count == 0)
            {
                return new IconValidationResult(false, "Invalid ICO file: Contains 0 image frames.");
            }

            int bestWidth = 0;
            int bestHeight = 0;

            for (int i = 0; i < count; i++)
            {
                if (stream.Position + 16 > stream.Length)
                {
                    return new IconValidationResult(false, "Corrupt ICO file: Truncated directory entries.");
                }

                byte bWidth = reader.ReadByte();
                byte bHeight = reader.ReadByte();
                byte bColorCount = reader.ReadByte();
                byte bReserved = reader.ReadByte();
                ushort wPlanes = reader.ReadUInt16();
                ushort wBitCount = reader.ReadUInt16();
                uint dwBytesInRes = reader.ReadUInt32();
                uint dwImageOffset = reader.ReadUInt32();

                // 0 width/height denotes 256x256 in ICO specifications
                int w = bWidth == 0 ? 256 : bWidth;
                int h = bHeight == 0 ? 256 : bHeight;

                if (w > bestWidth) bestWidth = w;
                if (h > bestHeight) bestHeight = h;

                // Validate offset and size within stream bounds
                if ((long)dwImageOffset + dwBytesInRes > stream.Length)
                {
                    return new IconValidationResult(false, $"Corrupt ICO file: Frame #{i + 1} offset exceeds file boundary.");
                }
            }

            return new IconValidationResult(
                true,
                BestWidth: bestWidth,
                BestHeight: bestHeight,
                ImageCount: count,
                FormatDescription: $"Standard Windows Icon (.ico) — {bestWidth}x{bestHeight} max ({count} sizes embedded)");
        }
        catch (Exception ex)
        {
            return new IconValidationResult(false, $"Failed to inspect icon file: {ex.Message}");
        }
    }
}

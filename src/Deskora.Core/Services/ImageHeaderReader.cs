using System.Buffers.Binary;

namespace Deskora.Core.Services;

/// <summary>
/// Fast, zero-allocation binary stream reader for extracting image dimensions from file headers.
/// Supports PNG, JPEG, BMP, and WebP formats without decoding pixel buffers.
/// </summary>
public static class ImageHeaderReader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".webp"
    };

    public static bool IsSupportedFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        var ext = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext);
    }

    public static bool TryGetDimensions(string filePath, out int width, out int height)
    {
        width = 0;
        height = 0;

        try
        {
            if (!File.Exists(filePath)) return false;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 16 || fileInfo.Length > 200_000_000) return false;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".png" => TryReadPng(reader, out width, out height),
                ".jpg" or ".jpeg" => TryReadJpeg(stream, reader, out width, out height),
                ".bmp" => TryReadBmp(reader, out width, out height),
                ".webp" => TryReadWebP(stream, reader, out width, out height),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadPng(BinaryReader reader, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            byte[] header = reader.ReadBytes(8);
            if (header.Length < 8 ||
                header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47 ||
                header[4] != 0x0D || header[5] != 0x0A || header[6] != 0x1A || header[7] != 0x0A)
            {
                return false;
            }

            // Skip chunk length (4 bytes) and chunk type (4 bytes: "IHDR")
            reader.ReadBytes(8);

            byte[] wBytes = reader.ReadBytes(4);
            byte[] hBytes = reader.ReadBytes(4);
            if (wBytes.Length < 4 || hBytes.Length < 4) return false;

            width = BinaryPrimitives.ReadInt32BigEndian(wBytes);
            height = BinaryPrimitives.ReadInt32BigEndian(hBytes);
            return width > 0 && height > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadBmp(BinaryReader reader, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            byte[] magic = reader.ReadBytes(2);
            if (magic.Length < 2 || magic[0] != 0x42 || magic[1] != 0x4D) // 'BM'
            {
                return false;
            }

            // DIB header width is at byte 18, height is at byte 22
            reader.BaseStream.Seek(18, SeekOrigin.Begin);
            width = reader.ReadInt32();
            height = Math.Abs(reader.ReadInt32());
            return width > 0 && height > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadJpeg(Stream stream, BinaryReader reader, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            byte[] magic = reader.ReadBytes(2);
            if (magic.Length < 2 || magic[0] != 0xFF || magic[1] != 0xD8) // SOI
            {
                return false;
            }

            while (stream.Position < stream.Length)
            {
                byte markerPrefix = reader.ReadByte();
                while (markerPrefix != 0xFF && stream.Position < stream.Length)
                {
                    markerPrefix = reader.ReadByte();
                }

                if (stream.Position >= stream.Length) break;
                byte marker = reader.ReadByte();

                // SOF markers: SOF0 (0xC0) to SOF3 (0xC3), SOF5-SOF7 (0xC5-0xC7), SOF9-SOF11 (0xC9-0xCB), SOF13-SOF15 (0xCD-0xCF)
                if ((marker >= 0xC0 && marker <= 0xC3) ||
                    (marker >= 0xC5 && marker <= 0xC7) ||
                    (marker >= 0xC9 && marker <= 0xCB) ||
                    (marker >= 0xCD && marker <= 0xCF))
                {
                    reader.ReadBytes(3); // skip length (2 bytes) and precision (1 byte)
                    byte[] hBytes = reader.ReadBytes(2);
                    byte[] wBytes = reader.ReadBytes(2);
                    if (hBytes.Length < 2 || wBytes.Length < 2) return false;

                    height = BinaryPrimitives.ReadUInt16BigEndian(hBytes);
                    width = BinaryPrimitives.ReadUInt16BigEndian(wBytes);
                    return width > 0 && height > 0;
                }

                if (marker == 0xDA || marker == 0xD9) // SOS or EOI
                {
                    break;
                }

                // Skip segment
                byte[] lenBytes = reader.ReadBytes(2);
                if (lenBytes.Length < 2) break;
                ushort segmentLen = BinaryPrimitives.ReadUInt16BigEndian(lenBytes);
                if (segmentLen < 2) break;
                stream.Seek(segmentLen - 2, SeekOrigin.Current);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadWebP(Stream stream, BinaryReader reader, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            byte[] riff = reader.ReadBytes(4);
            if (riff.Length < 4 || riff[0] != 'R' || riff[1] != 'I' || riff[2] != 'F' || riff[3] != 'F')
                return false;

            reader.ReadInt32(); // File size

            byte[] webp = reader.ReadBytes(4);
            if (webp.Length < 4 || webp[0] != 'W' || webp[1] != 'E' || webp[2] != 'B' || webp[3] != 'P')
                return false;

            byte[] chunkType = reader.ReadBytes(4);
            string type = System.Text.Encoding.ASCII.GetString(chunkType);

            if (type == "VP8X")
            {
                reader.ReadInt32(); // Chunk size
                reader.ReadByte();  // Flags
                reader.ReadBytes(3); // Reserved
                byte[] wBytes = reader.ReadBytes(3);
                byte[] hBytes = reader.ReadBytes(3);
                if (wBytes.Length < 3 || hBytes.Length < 3) return false;

                width = 1 + (wBytes[0] | (wBytes[1] << 8) | (wBytes[2] << 16));
                height = 1 + (hBytes[0] | (hBytes[1] << 8) | (hBytes[2] << 16));
                return width > 0 && height > 0;
            }

            if (type == "VP8 ")
            {
                reader.ReadInt32(); // Chunk size
                stream.Seek(6, SeekOrigin.Current);
                width = reader.ReadUInt16() & 0x3FFF;
                height = reader.ReadUInt16() & 0x3FFF;
                return width > 0 && height > 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

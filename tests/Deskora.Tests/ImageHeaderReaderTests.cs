using System.Buffers.Binary;
using Deskora.Core.Services;
using Xunit;

namespace Deskora.Tests;

public class ImageHeaderReaderTests
{
    [Fact]
    public void IsSupportedFormat_ValidExtensions_ReturnsTrue()
    {
        Assert.True(ImageHeaderReader.IsSupportedFormat("photo.png"));
        Assert.True(ImageHeaderReader.IsSupportedFormat("photo.jpg"));
        Assert.True(ImageHeaderReader.IsSupportedFormat("photo.jpeg"));
        Assert.True(ImageHeaderReader.IsSupportedFormat("photo.bmp"));
        Assert.True(ImageHeaderReader.IsSupportedFormat("photo.webp"));
        Assert.True(ImageHeaderReader.IsSupportedFormat("PHOTO.PNG"));
    }

    [Fact]
    public void IsSupportedFormat_UnsupportedExtensions_ReturnsFalse()
    {
        Assert.False(ImageHeaderReader.IsSupportedFormat("document.pdf"));
        Assert.False(ImageHeaderReader.IsSupportedFormat("video.mp4"));
        Assert.False(ImageHeaderReader.IsSupportedFormat("archive.zip"));
        Assert.False(ImageHeaderReader.IsSupportedFormat(""));
        Assert.False(ImageHeaderReader.IsSupportedFormat(null!));
    }

    [Fact]
    public void TryReadPng_ValidHeader_ReturnsCorrectDimensions()
    {
        // 8 bytes PNG magic + 4 bytes length + 4 bytes 'IHDR' + 4 bytes width + 4 bytes height
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // PNG magic
        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x0D }); // Chunk length (13)
        writer.Write(System.Text.Encoding.ASCII.GetBytes("IHDR"));

        // Width = 1920 (0x00000780), Height = 1080 (0x00000438)
        byte[] w = new byte[4];
        byte[] h = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(w, 1920);
        BinaryPrimitives.WriteInt32BigEndian(h, 1080);
        writer.Write(w);
        writer.Write(h);

        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        bool success = ImageHeaderReader.TryReadPng(reader, out int width, out int height);

        Assert.True(success);
        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    [Fact]
    public void TryReadBmp_ValidHeader_ReturnsCorrectDimensions()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(new byte[] { 0x42, 0x4D }); // 'BM'
        writer.Write(new byte[16]); // padding up to offset 18
        writer.Write(2560); // Width little-endian
        writer.Write(1440); // Height little-endian

        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        bool success = ImageHeaderReader.TryReadBmp(reader, out int width, out int height);

        Assert.True(success);
        Assert.Equal(2560, width);
        Assert.Equal(1440, height);
    }

    [Fact]
    public void TryReadJpeg_ValidSOF0_ReturnsCorrectDimensions()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(new byte[] { 0xFF, 0xD8 }); // SOI
        writer.Write(new byte[] { 0xFF, 0xC0 }); // SOF0 marker
        writer.Write(new byte[] { 0x00, 0x11 }); // Segment length (17)
        writer.Write((byte)8); // Precision (8 bit)

        // Height = 2160, Width = 3840 (big endian)
        byte[] h = new byte[2];
        byte[] w = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(h, 2160);
        BinaryPrimitives.WriteUInt16BigEndian(w, 3840);
        writer.Write(h);
        writer.Write(w);

        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        bool success = ImageHeaderReader.TryReadJpeg(ms, reader, out int width, out int height);

        Assert.True(success);
        Assert.Equal(3840, width);
        Assert.Equal(2160, height);
    }

    [Fact]
    public void TryGetDimensions_NonExistentFile_ReturnsFalse()
    {
        bool result = ImageHeaderReader.TryGetDimensions("C:\\NonExistent_Fake_Dir_9812739\\image.png", out int w, out int h);
        Assert.False(result);
        Assert.Equal(0, w);
        Assert.Equal(0, h);
    }
}

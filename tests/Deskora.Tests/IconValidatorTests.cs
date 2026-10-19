using Deskora.Core.Services;

namespace Deskora.Tests;

public class IconValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public IconValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"Deskora_IconTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public void Validate_WhenPathIsEmptyOrWhitespace_ReturnsInvalid()
    {
        var result1 = IconValidator.Validate("");
        var result2 = IconValidator.Validate("   ");

        Assert.False(result1.IsValid);
        Assert.False(result2.IsValid);
    }

    [Fact]
    public void Validate_WhenFileDoesNotExist_ReturnsInvalid()
    {
        var nonExistent = Path.Combine(_tempDir, "non_existent.ico");
        var result = IconValidator.Validate(nonExistent);

        Assert.False(result.IsValid);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhenUnsupportedExtension_ReturnsInvalidWithExplanation()
    {
        var pngPath = Path.Combine(_tempDir, "image.png");
        File.WriteAllText(pngPath, "fake png content");

        var result = IconValidator.Validate(pngPath);

        Assert.False(result.IsValid);
        Assert.Contains("Unsupported icon format", result.ErrorMessage);
        Assert.Contains(".ico", result.ErrorMessage);
    }

    [Fact]
    public void Validate_WhenValidIcoStructure_ReturnsValidWithDimensions()
    {
        var icoPath = Path.Combine(_tempDir, "valid.ico");
        using (var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            // ICONDIR
            writer.Write((ushort)0); // reserved
            writer.Write((ushort)1); // type = 1 (icon)
            writer.Write((ushort)1); // count = 1 image

            // ICONDIRENTRY (16 bytes)
            writer.Write((byte)48);  // width 48
            writer.Write((byte)48);  // height 48
            writer.Write((byte)0);   // colors
            writer.Write((byte)0);   // reserved
            writer.Write((ushort)1); // planes
            writer.Write((ushort)32);// bpp
            writer.Write((uint)10);  // bytes in res
            writer.Write((uint)22);  // image offset (6 + 16 = 22)

            // Dummy 10 bytes payload
            writer.Write(new byte[10]);
        }

        var result = IconValidator.Validate(icoPath);

        Assert.True(result.IsValid);
        Assert.Equal(48, result.BestWidth);
        Assert.Equal(48, result.BestHeight);
        Assert.Equal(1, result.ImageCount);
        Assert.Contains("48x48", result.FormatDescription);
    }

    [Fact]
    public void Validate_WhenCorruptHeader_ReturnsInvalid()
    {
        var corruptPath = Path.Combine(_tempDir, "corrupt.ico");
        using (var fs = new FileStream(corruptPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)1); // invalid reserved (must be 0)
            writer.Write((ushort)2); // invalid type (must be 1 for ICO)
            writer.Write((ushort)1); // count
        }

        var result = IconValidator.Validate(corruptPath);

        Assert.False(result.IsValid);
        Assert.Contains("Signature mismatch", result.ErrorMessage);
    }

    [Fact]
    public void Validate_WhenTruncatedFile_ReturnsInvalid()
    {
        var truncatedPath = Path.Combine(_tempDir, "truncated.ico");
        File.WriteAllBytes(truncatedPath, new byte[] { 0, 0, 1 }); // only 3 bytes

        var result = IconValidator.Validate(truncatedPath);

        Assert.False(result.IsValid);
        Assert.Contains("too small", result.ErrorMessage);
    }

    [Fact]
    public void Validate_WhenZeroFrameCount_ReturnsInvalid()
    {
        var zeroCountPath = Path.Combine(_tempDir, "zero.ico");
        using (var fs = new FileStream(zeroCountPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0); // reserved
            writer.Write((ushort)1); // type 1
            writer.Write((ushort)0); // 0 frames
        }

        var result = IconValidator.Validate(zeroCountPath);

        Assert.False(result.IsValid);
        Assert.Contains("0 image frames", result.ErrorMessage);
    }
}

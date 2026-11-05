using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ScanlineByteCountValidatorTests
{
    [Test]
    public void CalculateExpectedByteCount_IncludesOneFilterBytePerRow()
    {
        PngIhdr truecolor = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        PngIhdr packed = CreateIhdr(9, 4, PngColorType.Grayscale, PngBitDepth.One);

        Assert.That(PngScanlineByteCountValidator.CalculateExpectedByteCount(truecolor), Is.EqualTo(21));
        Assert.That(PngScanlineByteCountValidator.CalculateExpectedByteCount(packed), Is.EqualTo(12));
    }

    [Test]
    public void ValidateBeforeInflation_AcceptsExpectedByteCountWithinLimit()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 21;

        PngValidationResult result = PngScanlineByteCountValidator.ValidateBeforeInflation(ihdr, limits);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Info.Width, Is.EqualTo(2));
        Assert.That(result.Info.Height, Is.EqualTo(3));
    }

    [Test]
    public void ValidateBeforeInflation_RejectsExpectedByteCountBeyondLimit()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 20;

        PngValidationResult result = PngScanlineByteCountValidator.ValidateBeforeInflation(ihdr, limits);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_DECOMPRESSED_BYTE_LIMIT_EXCEEDED"));
    }

    [Test]
    public void ValidateAfterInflation_AcceptsExactByteCount()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        byte[] decompressed = new byte[21];

        PngValidationResult result = PngScanlineByteCountValidator.ValidateAfterInflation(decompressed, ihdr);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateAfterInflation_RejectsShortAndLongByteCounts()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);

        PngValidationResult shortResult = PngScanlineByteCountValidator.ValidateAfterInflation(new byte[20], ihdr);
        PngValidationResult longResult = PngScanlineByteCountValidator.ValidateAfterInflation(new byte[22], ihdr);

        Assert.That(shortResult.IsValid, Is.False);
        Assert.That(shortResult.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH"));
        Assert.That(longResult.IsValid, Is.False);
        Assert.That(longResult.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH"));
    }

    [Test]
    public void ValidateAfterInflation_AppliesPreInflationLimitBeforeLengthCheck()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 20;

        PngValidationResult result = PngScanlineByteCountValidator.ValidateAfterInflation(new byte[21], ihdr, limits);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_DECOMPRESSED_BYTE_LIMIT_EXCEEDED"));
    }

    [Test]
    public void Validators_ReportAdam7UnsupportedUntilPassGeometryExists()
    {
        PngIhdr ihdr = CreateIhdr(2, 3, PngColorType.Truecolor, PngBitDepth.Eight);
        ihdr.InterlaceMethod = PngInterlaceMethod.Adam7;

        PngValidationResult beforeResult = PngScanlineByteCountValidator.ValidateBeforeInflation(ihdr);
        PngValidationResult afterResult = PngScanlineByteCountValidator.ValidateAfterInflation(new byte[0], ihdr);

        Assert.That(beforeResult.IsValid, Is.False);
        Assert.That(beforeResult.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_INTERLACE_UNSUPPORTED"));
        Assert.That(afterResult.IsValid, Is.False);
        Assert.That(afterResult.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_INTERLACE_UNSUPPORTED"));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngScanlineByteCountValidator.CalculateExpectedByteCount(ihdr));
    }

    [Test]
    public void Validators_ReturnIhdrValidationErrorsBeforeScanlineChecks()
    {
        PngIhdr ihdr = CreateIhdr(0, 3, PngColorType.Truecolor, PngBitDepth.Eight);

        PngValidationResult beforeResult = PngScanlineByteCountValidator.ValidateBeforeInflation(ihdr);
        PngValidationResult afterResult = PngScanlineByteCountValidator.ValidateAfterInflation(new byte[0], ihdr);

        Assert.That(beforeResult.IsValid, Is.False);
        Assert.That(beforeResult.Errors[0].Code, Is.EqualTo("PNG_IHDR_WIDTH_INVALID"));
        Assert.That(afterResult.IsValid, Is.False);
        Assert.That(afterResult.Errors[0].Code, Is.EqualTo("PNG_IHDR_WIDTH_INVALID"));
    }

    [Test]
    public void Validators_RejectNullInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.CalculateExpectedByteCount(null!));
        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.ValidateBeforeInflation(null!));
        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.ValidateBeforeInflation(ihdr, null!));
        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.ValidateAfterInflation(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.ValidateAfterInflation(new byte[0], null!));
        Assert.Throws<ArgumentNullException>(() => PngScanlineByteCountValidator.ValidateAfterInflation(new byte[0], ihdr, null!));
    }

    static PngIhdr CreateIhdr(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.ColorType = colorType;
        ihdr.BitDepth = bitDepth;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }
}

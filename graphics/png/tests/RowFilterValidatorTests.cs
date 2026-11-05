using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class RowFilterValidatorTests
{
    [Test]
    public void IsValidFilterType_AcceptsOnlyPngFilterTypes()
    {
        Assert.That(PngRowFilterValidator.IsValidFilterType(PngConstants.FilterTypeNone), Is.True);
        Assert.That(PngRowFilterValidator.IsValidFilterType(PngConstants.FilterTypeSub), Is.True);
        Assert.That(PngRowFilterValidator.IsValidFilterType(PngConstants.FilterTypeUp), Is.True);
        Assert.That(PngRowFilterValidator.IsValidFilterType(PngConstants.FilterTypeAverage), Is.True);
        Assert.That(PngRowFilterValidator.IsValidFilterType(PngConstants.FilterTypePaeth), Is.True);
        Assert.That(PngRowFilterValidator.IsValidFilterType(-1), Is.False);
        Assert.That(PngRowFilterValidator.IsValidFilterType(5), Is.False);
        Assert.That(PngRowFilterValidator.IsValidFilterType(255), Is.False);
    }

    [Test]
    public void ValidateRow_AcceptsValidFilterByte()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage, 10, 20 };

        PngValidationResult result = PngRowFilterValidator.ValidateRow(filteredRow);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateRow_ReportsInvalidFilterByteWithOffset()
    {
        byte[] filteredRow = new byte[] { 9, 10, 20 };

        PngValidationResult result = PngRowFilterValidator.ValidateRow(filteredRow, 44);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_FILTER_TYPE_INVALID"));
        Assert.That(result.Errors[0].Message, Is.EqualTo("PNG scanline filter type must be 0 through 4."));
        Assert.That(result.Errors[0].ChunkType, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(44));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void ValidateRow_ReportsMissingFilterByte()
    {
        PngValidationResult result = PngRowFilterValidator.ValidateRow(new byte[0], 12);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_ROW_TOO_SHORT"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(12));
    }

    [Test]
    public void Validate_AcceptsEveryPngFilterTypeAcrossRows()
    {
        PngIhdr ihdr = CreateIhdr(1, 5, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 10,
            PngConstants.FilterTypeSub, 20,
            PngConstants.FilterTypeUp, 30,
            PngConstants.FilterTypeAverage, 40,
            PngConstants.FilterTypePaeth, 50
        };

        PngValidationResult result = PngRowFilterValidator.Validate(filteredScanlines, ihdr);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Info.Width, Is.EqualTo(1));
        Assert.That(result.Info.Height, Is.EqualTo(5));
    }

    [Test]
    public void Validate_ReportsInvalidFilterBytesAtRowOffsets()
    {
        PngIhdr ihdr = CreateIhdr(1, 3, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 10,
            9, 20,
            255, 30
        };

        PngValidationResult result = PngRowFilterValidator.Validate(filteredScanlines, ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(2));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_FILTER_TYPE_INVALID"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(2));
        Assert.That(result.Errors[1].Code, Is.EqualTo("PNG_SCANLINE_FILTER_TYPE_INVALID"));
        Assert.That(result.Errors[1].Offset, Is.EqualTo(4));
    }

    [Test]
    public void Validate_ReturnsByteCountErrorsBeforeFilterChecks()
    {
        PngIhdr ihdr = CreateIhdr(1, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[] { 9, 10, PngConstants.FilterTypeNone };

        PngValidationResult result = PngRowFilterValidator.Validate(filteredScanlines, ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH"));
    }

    [Test]
    public void Validate_RejectsNullAndInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngRowFilterValidator.ValidateRow(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRowFilterValidator.ValidateRow(new byte[] { 0 }, -1));
        Assert.Throws<ArgumentNullException>(() => PngRowFilterValidator.Validate(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngRowFilterValidator.Validate(new byte[0], null!));
        Assert.Throws<ArgumentNullException>(() => PngRowFilterValidator.Validate(new byte[0], ihdr, null!));
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

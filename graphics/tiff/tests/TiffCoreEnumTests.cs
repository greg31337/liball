using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCoreEnumTests
{
    [Test]
    public void ByteOrderValues_MatchHeaderMarkers()
    {
        Assert.That((int)TiffByteOrder.Unknown, Is.EqualTo(0));
        Assert.That((int)TiffByteOrder.LittleEndian, Is.EqualTo(0x4949));
        Assert.That((int)TiffByteOrder.BigEndian, Is.EqualTo(0x4D4D));
    }

    [Test]
    public void FileKindValues_MatchTiffVersionFields()
    {
        Assert.That((int)TiffFileKind.Unknown, Is.EqualTo(0));
        Assert.That((int)TiffFileKind.Classic, Is.EqualTo(42));
        Assert.That((int)TiffFileKind.BigTiff, Is.EqualTo(43));
    }

    [Test]
    public void DataTypeValues_MatchTiffAndBigTiffFieldTypes()
    {
        Assert.That((int)TiffDataType.Byte, Is.EqualTo(1));
        Assert.That((int)TiffDataType.Ascii, Is.EqualTo(2));
        Assert.That((int)TiffDataType.Short, Is.EqualTo(3));
        Assert.That((int)TiffDataType.Long, Is.EqualTo(4));
        Assert.That((int)TiffDataType.Rational, Is.EqualTo(5));
        Assert.That((int)TiffDataType.SByte, Is.EqualTo(6));
        Assert.That((int)TiffDataType.Undefined, Is.EqualTo(7));
        Assert.That((int)TiffDataType.SShort, Is.EqualTo(8));
        Assert.That((int)TiffDataType.SLong, Is.EqualTo(9));
        Assert.That((int)TiffDataType.SRational, Is.EqualTo(10));
        Assert.That((int)TiffDataType.Float, Is.EqualTo(11));
        Assert.That((int)TiffDataType.Double, Is.EqualTo(12));
        Assert.That((int)TiffDataType.Ifd, Is.EqualTo(13));
        Assert.That((int)TiffDataType.Long8, Is.EqualTo(16));
        Assert.That((int)TiffDataType.SLong8, Is.EqualTo(17));
        Assert.That((int)TiffDataType.Ifd8, Is.EqualTo(18));
    }

    [Test]
    public void CompressionValues_CoverBaselineCommonAndUnsupportedRegisteredSchemes()
    {
        Assert.That((int)TiffCompression.None, Is.EqualTo(1));
        Assert.That((int)TiffCompression.CcittRle, Is.EqualTo(2));
        Assert.That((int)TiffCompression.CcittGroup3, Is.EqualTo(3));
        Assert.That((int)TiffCompression.CcittGroup4, Is.EqualTo(4));
        Assert.That((int)TiffCompression.Lzw, Is.EqualTo(5));
        Assert.That((int)TiffCompression.OldJpeg, Is.EqualTo(6));
        Assert.That((int)TiffCompression.Jpeg, Is.EqualTo(7));
        Assert.That((int)TiffCompression.Deflate, Is.EqualTo(8));
        Assert.That((int)TiffCompression.PackBits, Is.EqualTo(32773));
        Assert.That((int)TiffCompression.AdobeDeflate, Is.EqualTo(32946));
        Assert.That((int)TiffCompression.Jpeg2000, Is.EqualTo(34712));
        Assert.That((int)TiffCompression.Lzma, Is.EqualTo(34925));
        Assert.That((int)TiffCompression.Zstd, Is.EqualTo(50000));
        Assert.That((int)TiffCompression.WebP, Is.EqualTo(50001));
        Assert.That((int)TiffCompression.Jxl, Is.EqualTo(50002));
    }

    [Test]
    public void PhotometricValues_MatchTiffRegistryValues()
    {
        Assert.That((int)TiffPhotometric.WhiteIsZero, Is.EqualTo(0));
        Assert.That((int)TiffPhotometric.BlackIsZero, Is.EqualTo(1));
        Assert.That((int)TiffPhotometric.Rgb, Is.EqualTo(2));
        Assert.That((int)TiffPhotometric.Palette, Is.EqualTo(3));
        Assert.That((int)TiffPhotometric.TransparencyMask, Is.EqualTo(4));
        Assert.That((int)TiffPhotometric.Cmyk, Is.EqualTo(5));
        Assert.That((int)TiffPhotometric.YCbCr, Is.EqualTo(6));
        Assert.That((int)TiffPhotometric.CieLab, Is.EqualTo(8));
        Assert.That((int)TiffPhotometric.IccLab, Is.EqualTo(9));
        Assert.That((int)TiffPhotometric.ItuLab, Is.EqualTo(10));
        Assert.That((int)TiffPhotometric.ColorFilterArray, Is.EqualTo(32803));
        Assert.That((int)TiffPhotometric.LogL, Is.EqualTo(32844));
        Assert.That((int)TiffPhotometric.LogLuv, Is.EqualTo(32845));
        Assert.That((int)TiffPhotometric.LinearRaw, Is.EqualTo(34892));
    }

    [Test]
    public void LayoutAndSampleEnumValues_MatchTiffTags()
    {
        Assert.That((int)TiffPlanarConfiguration.Contiguous, Is.EqualTo(1));
        Assert.That((int)TiffPlanarConfiguration.Separate, Is.EqualTo(2));
        Assert.That((int)TiffSampleFormat.UnsignedInteger, Is.EqualTo(1));
        Assert.That((int)TiffSampleFormat.SignedInteger, Is.EqualTo(2));
        Assert.That((int)TiffSampleFormat.IeeeFloatingPoint, Is.EqualTo(3));
        Assert.That((int)TiffSampleFormat.Undefined, Is.EqualTo(4));
        Assert.That((int)TiffSampleFormat.ComplexInteger, Is.EqualTo(5));
        Assert.That((int)TiffSampleFormat.ComplexIeeeFloatingPoint, Is.EqualTo(6));
    }

    [Test]
    public void OrientationAndExtraSampleValues_MatchTiffTagValues()
    {
        Assert.That((int)TiffOrientation.TopLeft, Is.EqualTo(1));
        Assert.That((int)TiffOrientation.TopRight, Is.EqualTo(2));
        Assert.That((int)TiffOrientation.BottomRight, Is.EqualTo(3));
        Assert.That((int)TiffOrientation.BottomLeft, Is.EqualTo(4));
        Assert.That((int)TiffOrientation.LeftTop, Is.EqualTo(5));
        Assert.That((int)TiffOrientation.RightTop, Is.EqualTo(6));
        Assert.That((int)TiffOrientation.RightBottom, Is.EqualTo(7));
        Assert.That((int)TiffOrientation.LeftBottom, Is.EqualTo(8));
        Assert.That((int)TiffExtraSample.Unspecified, Is.EqualTo(0));
        Assert.That((int)TiffExtraSample.AssociatedAlpha, Is.EqualTo(1));
        Assert.That((int)TiffExtraSample.UnassociatedAlpha, Is.EqualTo(2));
    }

    [Test]
    public void ErrorCodeValues_AreStableForDiagnostics()
    {
        Assert.That((int)TiffErrorCode.None, Is.EqualTo(0));
        Assert.That((int)TiffErrorCode.UnknownError, Is.EqualTo(1));
        Assert.That((int)TiffErrorCode.UnsupportedStream, Is.EqualTo(2));
        Assert.That((int)TiffErrorCode.NonSeekableStream, Is.EqualTo(3));
        Assert.That((int)TiffErrorCode.InvalidByteOrder, Is.EqualTo(4));
        Assert.That((int)TiffErrorCode.InvalidTiffVersion, Is.EqualTo(5));
        Assert.That((int)TiffErrorCode.InvalidBigTiffHeader, Is.EqualTo(6));
        Assert.That((int)TiffErrorCode.InvalidOffset, Is.EqualTo(7));
        Assert.That((int)TiffErrorCode.InvalidByteRange, Is.EqualTo(8));
        Assert.That((int)TiffErrorCode.IntegerOverflow, Is.EqualTo(9));
        Assert.That((int)TiffErrorCode.AllocationLimitExceeded, Is.EqualTo(10));
        Assert.That((int)TiffErrorCode.ImageDimensionLimitExceeded, Is.EqualTo(11));
        Assert.That((int)TiffErrorCode.IfdCountLimitExceeded, Is.EqualTo(12));
        Assert.That((int)TiffErrorCode.TagCountLimitExceeded, Is.EqualTo(13));
        Assert.That((int)TiffErrorCode.CyclicIfdChain, Is.EqualTo(14));
        Assert.That((int)TiffErrorCode.OverlappingDataBlock, Is.EqualTo(15));
        Assert.That((int)TiffErrorCode.DuplicateTag, Is.EqualTo(16));
        Assert.That((int)TiffErrorCode.MissingRequiredTag, Is.EqualTo(17));
        Assert.That((int)TiffErrorCode.InvalidTagType, Is.EqualTo(18));
        Assert.That((int)TiffErrorCode.InvalidTagCount, Is.EqualTo(19));
        Assert.That((int)TiffErrorCode.InvalidTagValue, Is.EqualTo(20));
        Assert.That((int)TiffErrorCode.InvalidAsciiValue, Is.EqualTo(21));
        Assert.That((int)TiffErrorCode.InvalidRationalValue, Is.EqualTo(22));
        Assert.That((int)TiffErrorCode.UnsupportedCompression, Is.EqualTo(23));
        Assert.That((int)TiffErrorCode.UnsupportedPhotometric, Is.EqualTo(24));
        Assert.That((int)TiffErrorCode.CodecExpansionLimitExceeded, Is.EqualTo(25));
        Assert.That((int)TiffErrorCode.TruncatedStream, Is.EqualTo(26));
    }
}

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCodecTests
{
    [Test]
    public void Decode_DispatchesNoCompressionAsCopy()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.None;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());
        byte[] source = new byte[] { 1, 2, 3 };

        TiffCodecResult result = TiffCodec.Decode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Decode_ReportsUnsupportedCompressionAndPreservesRawBytes()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.Jpeg;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());
        byte[] source = new byte[] { 1, 2 };

        TiffCodecResult result = TiffCodec.Decode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void Decode_DoesNotPreserveRawBytesForUnknownCompression()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.Unknown;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 1, 2 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void Decode_EnforcesExpansionLimit()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.None;
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 6, limits);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Decode_EnforcesCodecExpansionRatio()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.PackBits;
        TiffLimits limits = new TiffLimits();
        limits.MaximumCodecExpansionRatio = 1;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 4, limits);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 253, 6 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Encode_DispatchesNoCompressionAsCopy()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.None;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());
        byte[] source = new byte[] { 4, 5, 6 };

        TiffCodecResult result = TiffCodec.Encode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void Encode_ReportsUnsupportedCompressionAndPreservesRawBytes()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.Jpeg;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());
        byte[] source = new byte[] { 1, 2, 3 };

        TiffCodecResult result = TiffCodec.Encode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void Encode_PreservesRawBytesForPrivateUnsupportedCompression()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = (TiffCompression)65000;
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 7, 8, 9 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 7, 8, 9 }));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void CompressionHelpers_ClassifySupportedAndRawPassThroughValues()
    {
        Assert.That(TiffCodec.IsCompressionSupported(TiffCompression.Deflate), Is.True);
        Assert.That(TiffCodec.IsCompressionSupported(TiffCompression.CcittGroup4), Is.True);
        Assert.That(TiffCodec.CanPassThroughRawCompression(TiffCompression.Jpeg), Is.True);
        Assert.That(TiffCodec.CanPassThroughRawCompression((TiffCompression)65000), Is.True);
        Assert.That(TiffCodec.CanPassThroughRawCompression(TiffCompression.Unknown), Is.False);
        Assert.That(TiffCodec.CanPassThroughRawCompression(TiffCompression.None), Is.False);
    }
}

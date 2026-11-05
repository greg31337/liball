using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffNoneCodecTests
{
    [Test]
    public void Decode_CopiesExactUncompressedBytes()
    {
        TiffCodecContext context = CreateContext(3);
        byte[] source = new byte[] { 1, 2, 3 };

        TiffCodecResult result = TiffNoneCodec.Decode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Encode_CopiesExactDecodedBytes()
    {
        TiffCodecContext context = CreateContext(3);
        byte[] source = new byte[] { 4, 5, 6 };

        TiffCodecResult result = TiffNoneCodec.Encode(context, source);
        source[0] = 9;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void Decode_RejectsSourceByteCountMismatch()
    {
        TiffCodecContext context = CreateContext(3);

        TiffCodecResult result = TiffNoneCodec.Decode(context, new byte[] { 1, 2 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_RejectsSourceByteCountMismatch()
    {
        TiffCodecContext context = CreateContext(3);

        TiffCodecResult result = TiffNoneCodec.Encode(context, new byte[] { 1, 2, 3, 4 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_EnforcesDecodedByteLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 2;
        TiffCodecContext context = CreateContext(3, limits);

        TiffCodecResult result = TiffNoneCodec.Decode(context, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Decode_ReportsMissingContext()
    {
        TiffCodecResult result = TiffNoneCodec.Decode(null!, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount)
    {
        return CreateContext(expectedDecodedByteCount, new TiffLimits());
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount, TiffLimits limits)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.None;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, limits);
    }
}

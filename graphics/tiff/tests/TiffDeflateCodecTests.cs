using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDeflateCodecTests
{
    [Test]
    public void Decode_DecodesZLibWrappedDeflateBytes()
    {
        TiffCodecContext context = CreateContext(6, TiffCompression.Deflate, new TiffLimits());
        byte[] source = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };

        TiffCodecResult result = TiffDeflateCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Decode_DecodesRawDeflateBytes()
    {
        TiffCodecContext context = CreateContext(6, TiffCompression.Deflate, new TiffLimits());
        byte[] source = new byte[] { 99, 100, 98, 102, 97, 101, 3, 0 };

        TiffCodecResult result = TiffDeflateCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Decode_DispatchesAdobeDeflateThroughCodec()
    {
        TiffCodecContext context = CreateContext(17, TiffCompression.AdobeDeflate, new TiffLimits());
        byte[] source = new byte[] { 120, 156, 203, 72, 205, 201, 201, 87, 200, 64, 144, 0, 58, 46, 6, 125 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111 }));
    }

    [Test]
    public void Decode_ReportsTruncatedDecodedOutput()
    {
        TiffCodecContext context = CreateContext(7, TiffCompression.Deflate, new TiffLimits());
        byte[] source = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };

        TiffCodecResult result = TiffDeflateCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Decode_ReportsOutputBeyondExpectedDecodedByteCount()
    {
        TiffCodecContext context = CreateContext(5, TiffCompression.Deflate, new TiffLimits());
        byte[] source = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };

        TiffCodecResult result = TiffDeflateCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_EnforcesDecodedByteLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;
        TiffCodecContext context = CreateContext(6, TiffCompression.Deflate, limits);
        byte[] source = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };

        TiffCodecResult result = TiffDeflateCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Encode_EncodesZLibWrappedDeflateBytes()
    {
        TiffCodecContext context = CreateContext(17, TiffCompression.Deflate, new TiffLimits());
        byte[] source = new byte[] { 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111, 32, 104, 101, 108, 108, 111 };

        TiffCodecResult result = TiffDeflateCodec.Encode(context, source);
        TiffCodecResult decoded = TiffDeflateCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(TiffDeflateCodec.IsZLibWrapped(result.Bytes), Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_DispatchesAdobeDeflateThroughCodec()
    {
        TiffCodecContext context = CreateContext(6, TiffCompression.AdobeDeflate, new TiffLimits());
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };

        TiffCodecResult result = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(TiffDeflateCodec.IsZLibWrapped(result.Bytes), Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_RejectsSourceByteCountMismatch()
    {
        TiffCodecContext context = CreateContext(4, TiffCompression.Deflate, new TiffLimits());

        TiffCodecResult result = TiffDeflateCodec.Encode(context, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_EnforcesAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 28;
        TiffCodecContext context = CreateContext(16, TiffCompression.Deflate, limits);
        byte[] source = new byte[16];

        TiffCodecResult result = TiffDeflateCodec.Encode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount, TiffCompression compression, TiffLimits limits)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = compression;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, limits);
    }
}

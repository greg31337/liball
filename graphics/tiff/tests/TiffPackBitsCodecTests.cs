using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPackBitsCodecTests
{
    [Test]
    public void Decode_HandlesLiteralRepeatedAndNoOpRuns()
    {
        TiffCodecContext context = CreateContext(6);
        byte[] source = new byte[] { 1, 10, 11, 254, 7, 128, 0, 12 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 7, 7, 7, 12 }));
    }

    [Test]
    public void Decode_AllowsTrailingNoOpsAfterExpectedOutput()
    {
        TiffCodecContext context = CreateContext(1);
        byte[] source = new byte[] { 0, 5, 128, 128 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 5 }));
    }

    [Test]
    public void Decode_ReportsTruncatedLiteralRun()
    {
        TiffCodecContext context = CreateContext(3);
        byte[] source = new byte[] { 2, 1, 2 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Decode_ReportsMissingRepeatedByte()
    {
        TiffCodecContext context = CreateContext(3);
        byte[] source = new byte[] { 254 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Decode_ReportsRunBeyondExpectedDecodedByteCount()
    {
        TiffCodecContext context = CreateContext(2);
        byte[] source = new byte[] { 254, 9 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsTrailingProducingCommand()
    {
        TiffCodecContext context = CreateContext(1);
        byte[] source = new byte[] { 0, 1, 0, 2 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_DispatchesPackBitsThroughCodec()
    {
        TiffCodecContext context = CreateContext(4);
        byte[] source = new byte[] { 253, 6 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 6, 6, 6, 6 }));
    }

    [Test]
    public void Decode_EnforcesDecodedByteLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 3;
        TiffCodecContext context = CreateContext(4, limits);
        byte[] source = new byte[] { 253, 6 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Decode_EnforcesCodecExpansionRatio()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumCodecExpansionRatio = 1;
        TiffCodecContext context = CreateContext(4, limits);
        byte[] source = new byte[] { 253, 6 };

        TiffCodecResult result = TiffPackBitsCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void Encode_EncodesRepeatedAndLiteralRuns()
    {
        TiffCodecContext context = CreateContext(6);
        byte[] source = new byte[] { 10, 11, 7, 7, 7, 12 };

        TiffCodecResult result = TiffPackBitsCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 10, 11, 254, 7, 0, 12 }));
    }

    [Test]
    public void Encode_SplitsLiteralRunsAtOneHundredTwentyEightBytes()
    {
        TiffCodecContext context = CreateContext(130);
        byte[] source = new byte[130];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)index;
            index = index + 1;
        }

        TiffCodecResult result = TiffPackBitsCodec.Encode(context, source);
        TiffCodecResult decoded = TiffPackBitsCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes.Length, Is.EqualTo(132));
        Assert.That(result.Bytes[0], Is.EqualTo(127));
        Assert.That(result.Bytes[129], Is.EqualTo(1));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_SplitsLongRepeatedRunsAtOneHundredTwentyEightBytes()
    {
        TiffCodecContext context = CreateContext(130);
        byte[] source = new byte[130];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = 9;
            index = index + 1;
        }

        TiffCodecResult result = TiffPackBitsCodec.Encode(context, source);
        TiffCodecResult decoded = TiffPackBitsCodec.Decode(context, result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 129, 9, 1, 9, 9 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_RejectsSourceByteCountMismatch()
    {
        TiffCodecContext context = CreateContext(4);

        TiffCodecResult result = TiffPackBitsCodec.Encode(context, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_DispatchesPackBitsThroughCodec()
    {
        TiffCodecContext context = CreateContext(4);

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 6, 6, 6, 6 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 253, 6 }));
    }

    [Test]
    public void Encode_EnforcesAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 128;
        TiffCodecContext context = CreateContext(128, limits);
        byte[] source = new byte[128];

        TiffCodecResult result = TiffPackBitsCodec.Encode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount)
    {
        return CreateContext(expectedDecodedByteCount, new TiffLimits());
    }

    static TiffCodecContext CreateContext(ulong expectedDecodedByteCount, TiffLimits limits)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = TiffCompression.PackBits;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, limits);
    }
}

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCcittGroup4CodecTests
{
    [Test]
    public void Decode_DecodesAllWhiteRow()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 128, 8, 0, 128 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void Encode_EncodesAllWhiteRow()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 0 };

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 128, 8, 0, 128 }));
    }

    [Test]
    public void Decode_DecodesHorizontalAndVerticalModes()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 49, 192, 4, 0, 64 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_EncodesHorizontalAndVerticalModes()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 49, 192, 4, 0, 64 }));
    }

    [Test]
    public void Encode_UsesPreviousRowAsReference()
    {
        TiffCodecContext context = CreateContext(8, 2, TiffPhotometric.WhiteIsZero, 2, 0);
        byte[] source = new byte[] { 24, 24 };

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 49, 248, 0, 128, 8 }));
    }

    [Test]
    public void Decode_UsesPreviousRowAsReference()
    {
        TiffCodecContext context = CreateContext(8, 2, TiffPhotometric.WhiteIsZero, 2, 0);
        byte[] source = new byte[] { 49, 248, 0, 128, 8 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24, 24 }));
    }

    [Test]
    public void Encode_HandlesRowsThatStartWithBlackRun()
    {
        TiffCodecContext context = CreateContext(5, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 248 };

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 38, 166, 0, 32, 2 }));
    }

    [Test]
    public void Encode_RoundTripsBlackIsZero()
    {
        TiffCodecContext context = CreateContext(8, 2, TiffPhotometric.BlackIsZero, 2, 0);
        byte[] source = new byte[] { 231, 231 };

        TiffCodecResult encoded = TiffCcittGroup4Codec.Encode(context, source);
        TiffCodecResult decoded = TiffCcittGroup4Codec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Decode_DispatchesGroup4ThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 49, 192, 4, 0, 64 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_DispatchesGroup4ThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 49, 192, 4, 0, 64 }));
    }

    [Test]
    public void Decode_ReportsInvalidModeCode()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 0, 0 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsInvalidT6OptionsBitZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 1);
        byte[] source = new byte[] { 128, 8, 0, 128 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Encode_ReportsUnsupportedUncompressedOption()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, TiffCcittGroup4Codec.UncompressedOption);

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void Encode_EnforcesAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 1;
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0, limits);

        TiffCodecResult result = TiffCcittGroup4Codec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void Decode_EnforcesCodecExpansionRatio()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumCodecExpansionRatio = 1;
        TiffCodecContext context = CreateContext(64, 1, TiffPhotometric.WhiteIsZero, 8, 0, limits);
        byte[] source = new byte[] { 128, 8, 0, 128 };

        TiffCodecResult result = TiffCcittGroup4Codec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void ReadStrip_DecodesCcittGroup4CompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero, 0);
        byte[] fileBytes = new byte[40];
        fileBytes[20] = 49;
        fileBytes[21] = 192;
        fileBytes[22] = 4;
        fileBytes[23] = 0;
        fileBytes[24] = 64;
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 5 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittGroup4));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void WriteStrip_EncodesCcittGroup4CompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero, 0);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 5 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 24 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittGroup4));
        Assert.That(result.BytesWritten, Is.EqualTo(5));
        Assert.That(ExtractBytes(destination, 20, 5), Is.EqualTo(new byte[] { 49, 192, 4, 0, 64 }));
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount, int t6Options)
    {
        return CreateContext(width, length, photometric, expectedByteCount, t6Options, new TiffLimits());
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount, int t6Options, TiffLimits limits)
    {
        TiffImageInfo info = CreateInfo(width, length, photometric, t6Options);
        return TiffCodecContext.Create(info, false, 0, expectedByteCount, limits);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, TiffPhotometric photometric, int t6Options)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.Photometric = photometric;
        info.Compression = TiffCompression.CcittGroup4;
        info.T6Options = t6Options;
        return info;
    }

    static byte[] ExtractBytes(byte[] source, int sourceIndex, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = source[sourceIndex + index];
            index = index + 1;
        }

        return bytes;
    }
}

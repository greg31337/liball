using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCcittGroup3CodecTests
{
    [Test]
    public void Decode_DecodesOneDimensionalRows()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 0, 24, 224 };

        TiffCodecResult result = TiffCcittGroup3Codec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_EncodesOneDimensionalRows()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCcittGroup3Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 24, 224 }));
    }

    [Test]
    public void Decode_DecodesByteAlignedEolRows()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, TiffCcittGroup3Codec.ByteAlignedEolOption);
        byte[] source = new byte[] { 0, 1, 142, 0 };

        TiffCodecResult result = TiffCcittGroup3Codec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_ByteAlignsEolRowsWhenRequested()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, TiffCcittGroup3Codec.ByteAlignedEolOption);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCcittGroup3Codec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 1, 142, 0 }));
    }

    [Test]
    public void Decode_DispatchesGroup3ThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 0, 24, 224 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_DispatchesGroup3ThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 24, 224 }));
    }

    [Test]
    public void Decode_ReportsMissingEol()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, 0);
        byte[] source = new byte[] { 142, 0 };

        TiffCodecResult result = TiffCcittGroup3Codec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsUnsupportedTwoDimensionalOption()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, TiffCcittGroup3Codec.TwoDimensionalOption);
        byte[] source = new byte[] { 0, 24, 224 };

        TiffCodecResult result = TiffCcittGroup3Codec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void Encode_ReportsUnsupportedUncompressedOption()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, TiffCcittGroup3Codec.UncompressedOption);

        TiffCodecResult result = TiffCcittGroup3Codec.Encode(context, new byte[] { 24 });

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

        TiffCodecResult result = TiffCcittGroup3Codec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadStrip_DecodesCcittGroup3CompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero, 0);
        byte[] fileBytes = new byte[40];
        fileBytes[20] = 0;
        fileBytes[21] = 24;
        fileBytes[22] = 224;
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 3 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittGroup3));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void WriteStrip_EncodesCcittGroup3CompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero, 0);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 3 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 24 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittGroup3));
        Assert.That(result.BytesWritten, Is.EqualTo(3));
        Assert.That(ExtractBytes(destination, 20, 3), Is.EqualTo(new byte[] { 0, 24, 224 }));
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount, int t4Options)
    {
        return CreateContext(width, length, photometric, expectedByteCount, t4Options, new TiffLimits());
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount, int t4Options, TiffLimits limits)
    {
        TiffImageInfo info = CreateInfo(width, length, photometric, t4Options);
        return TiffCodecContext.Create(info, false, 0, expectedByteCount, limits);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, TiffPhotometric photometric, int t4Options)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.Photometric = photometric;
        info.Compression = TiffCompression.CcittGroup3;
        info.T4Options = t4Options;
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

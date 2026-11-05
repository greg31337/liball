using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCcittModifiedHuffmanCodecTests
{
    [Test]
    public void Decode_DecodesWhiteAndBlackRunsForWhiteIsZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 142, 0 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Decode_DecodesWhiteAndBlackRunsForBlackIsZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.BlackIsZero, 1);
        byte[] source = new byte[] { 142, 0 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 231 }));
    }

    [Test]
    public void Decode_DecodesRowsThatStartWithBlackRun()
    {
        TiffCodecContext context = CreateContext(5, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 53, 48 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 248 }));
    }

    [Test]
    public void Decode_DecodesMakeupRuns()
    {
        TiffCodecContext context = CreateContext(70, 1, TiffPhotometric.WhiteIsZero, 9);
        byte[] source = new byte[] { 217, 169, 0 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 252 }));
    }

    [Test]
    public void Decode_ResetsToWhiteAtEachRow()
    {
        TiffCodecContext context = CreateContext(4, 2, TiffPhotometric.WhiteIsZero, 2);
        byte[] source = new byte[] { 126, 192 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 48, 0 }));
    }

    [Test]
    public void Decode_DispatchesCcittRleThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 142, 0 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Decode_ReportsRunBeyondRowWidth()
    {
        TiffCodecContext context = CreateContext(4, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 192 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReportsTruncatedCode()
    {
        TiffCodecContext context = CreateContext(1, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 0 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void Decode_ReportsInvalidBitsPerSample()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero);
        info.BitsPerSample = new int[] { 8 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 1, new TiffLimits());

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Decode(context, new byte[] { 142, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Encode_EncodesWhiteAndBlackRunsForWhiteIsZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 142, 0 }));
    }

    [Test]
    public void Encode_EncodesWhiteAndBlackRunsForBlackIsZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.BlackIsZero, 1);
        byte[] source = new byte[] { 231 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 142, 0 }));
    }

    [Test]
    public void Encode_EncodesRowsThatStartWithBlackRun()
    {
        TiffCodecContext context = CreateContext(5, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 248 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 53, 48 }));
    }

    [Test]
    public void Encode_EncodesMakeupRuns()
    {
        TiffCodecContext context = CreateContext(70, 1, TiffPhotometric.WhiteIsZero, 9);
        byte[] source = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 252 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 217, 169, 0 }));
    }

    [Test]
    public void Encode_ResetsToWhiteAtEachRow()
    {
        TiffCodecContext context = CreateContext(4, 2, TiffPhotometric.WhiteIsZero, 2);
        byte[] source = new byte[] { 48, 0 };

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 126, 192 }));
    }

    [Test]
    public void Encode_DispatchesCcittRleThroughCodec()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1);
        byte[] source = new byte[] { 24 };

        TiffCodecResult result = TiffCodec.Encode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 142, 0 }));
    }

    [Test]
    public void Encode_RoundTripsBlackIsZero()
    {
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.BlackIsZero, 1);
        byte[] source = new byte[] { 231 };

        TiffCodecResult encoded = TiffCcittModifiedHuffmanCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCcittModifiedHuffmanCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_ReportsInvalidSourceByteCount()
    {
        TiffCodecContext context = CreateContext(8, 2, TiffPhotometric.WhiteIsZero, 2);

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_ReportsInvalidBitsPerSample()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero);
        info.BitsPerSample = new int[] { 8 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 1, new TiffLimits());

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Encode_EnforcesAllocationLimit()
    {
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 1;
        TiffCodecContext context = CreateContext(8, 1, TiffPhotometric.WhiteIsZero, 1, limits);

        TiffCodecResult result = TiffCcittModifiedHuffmanCodec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void ReadStrip_DecodesCcittRleCompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero);
        byte[] fileBytes = new byte[40];
        fileBytes[20] = 142;
        fileBytes[21] = 0;
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 2 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittRle));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void WriteStrip_EncodesCcittRleCompressedBytes()
    {
        TiffImageInfo info = CreateInfo(8, 1, TiffPhotometric.WhiteIsZero);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 2 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 24 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.CcittRle));
        Assert.That(result.BytesWritten, Is.EqualTo(2));
        Assert.That(ExtractBytes(destination, 20, 2), Is.EqualTo(new byte[] { 142, 0 }));
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount)
    {
        return CreateContext(width, length, photometric, expectedByteCount, new TiffLimits());
    }

    static TiffCodecContext CreateContext(ulong width, ulong length, TiffPhotometric photometric, ulong expectedByteCount, TiffLimits limits)
    {
        TiffImageInfo info = CreateInfo(width, length, photometric);
        return TiffCodecContext.Create(info, false, 0, expectedByteCount, limits);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, TiffPhotometric photometric)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.Photometric = photometric;
        info.Compression = TiffCompression.CcittRle;
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

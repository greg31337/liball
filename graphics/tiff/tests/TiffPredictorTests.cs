using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPredictorTests
{
    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorForEightBitRows()
    {
        TiffImageInfo info = CreateInfo(4, 2, 2, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 10, 5, 1, 4, 3, 4, 255, 3 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 8, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 15, 16, 20, 3, 7, 6, 9 }));
    }

    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorForContiguousRgb()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 3, new int[] { 8, 8, 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 10, 20, 30, 2, 5, 1, 3, 251, 9 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 9, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 20, 30, 12, 25, 31, 15, 20, 40 }));
    }

    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorForLittleEndianSixteenBitSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 16 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 232, 3, 5, 0, 250, 255 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 6, new TiffLimits());
        context.ByteOrder = TiffByteOrder.LittleEndian;

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 232, 3, 237, 3, 231, 3 }));
    }

    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorForBigEndianSixteenBitSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 16 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 3, 232, 0, 5, 255, 250 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 6, new TiffLimits());
        context.ByteOrder = TiffByteOrder.BigEndian;

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 232, 3, 237, 3, 231, 3 }));
    }

    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorForSeparatePlane()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 2, new int[] { 8, 8 }, TiffPlanarConfiguration.Separate, TiffCompression.None);
        byte[] source = new byte[] { 10, 5, 3 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 1, 3, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 15, 18 }));
    }

    [Test]
    public void Decode_ReversesHorizontalIntegerPredictorAfterLzwDecode()
    {
        TiffImageInfo encodedInfo = CreateInfo(4, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Lzw);
        encodedInfo.Predictor = 1;
        TiffCodecContext encodeContext = TiffCodecContext.Create(encodedInfo, false, 0, 4, new TiffLimits());
        TiffCodecResult encoded = TiffLzwCodec.Encode(encodeContext, new byte[] { 10, 5, 1, 4 });

        TiffImageInfo decodeInfo = CreateInfo(4, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Lzw);
        TiffCodecContext decodeContext = TiffCodecContext.Create(decodeInfo, false, 0, 4, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(decodeContext, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 15, 16, 20 }));
    }

    [Test]
    public void Decode_ReportsNonByteAlignedPredictorSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 4 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 18 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 1, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Decode_ReportsPredictorByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 1, 2 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 2, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Encode_AppliesHorizontalIntegerPredictorForEightBitRows()
    {
        TiffImageInfo info = CreateInfo(4, 2, 2, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 10, 15, 16, 20, 3, 7, 6, 9 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 8, new TiffLimits());

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 10, 5, 1, 4, 3, 4, 255, 3 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesHorizontalIntegerPredictorForContiguousRgb()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 3, new int[] { 8, 8, 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 10, 20, 30, 12, 25, 31, 15, 20, 40 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 9, new TiffLimits());

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 10, 20, 30, 2, 5, 1, 3, 251, 9 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesHorizontalIntegerPredictorForLittleEndianSixteenBitSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 16 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 232, 3, 237, 3, 231, 3 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 6, new TiffLimits());
        context.ByteOrder = TiffByteOrder.LittleEndian;

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 232, 3, 5, 0, 250, 255 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesHorizontalIntegerPredictorForBigEndianSixteenBitSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 16 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 232, 3, 237, 3, 231, 3 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 6, new TiffLimits());
        context.ByteOrder = TiffByteOrder.BigEndian;

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 3, 232, 0, 5, 255, 250 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesHorizontalIntegerPredictorBeforeLzwEncode()
    {
        TiffImageInfo info = CreateInfo(4, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Lzw);
        byte[] source = new byte[] { 10, 15, 16, 20 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 4, new TiffLimits());

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        TiffImageInfo noPredictorInfo = CreateInfo(4, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Lzw);
        noPredictorInfo.Predictor = 1;
        TiffCodecContext noPredictorContext = TiffCodecContext.Create(noPredictorInfo, false, 0, 4, new TiffLimits());
        TiffCodecResult differenced = TiffCodec.Decode(noPredictorContext, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
        Assert.That(differenced.Success, Is.True);
        Assert.That(differenced.Bytes, Is.EqualTo(new byte[] { 10, 5, 1, 4 }));
    }

    [Test]
    public void Encode_ReportsPredictorByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 2, new TiffLimits());

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 1, 2 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void Decode_ReversesFloatingPointPredictorForLittleEndianThirtyTwoBitSamples()
    {
        TiffImageInfo info = CreateFloatingInfo(3, 1, 1, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 63, 1, 0, 64, 128, 128, 128, 0, 0, 0, 0, 0 };
        byte[] expected = new byte[] { 0, 0, 128, 63, 0, 0, 0, 64, 0, 0, 128, 64 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 12, new TiffLimits());
        context.ByteOrder = TiffByteOrder.LittleEndian;

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    [Test]
    public void Encode_AppliesFloatingPointPredictorForLittleEndianThirtyTwoBitSamples()
    {
        TiffImageInfo info = CreateFloatingInfo(3, 1, 1, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 0, 0, 128, 63, 0, 0, 0, 64, 0, 0, 128, 64 };
        byte[] expected = new byte[] { 63, 1, 0, 64, 128, 128, 128, 0, 0, 0, 0, 0 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 12, new TiffLimits());
        context.ByteOrder = TiffByteOrder.LittleEndian;

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(expected));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesFloatingPointPredictorForBigEndianThirtyTwoBitSamples()
    {
        TiffImageInfo info = CreateFloatingInfo(3, 1, 1, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[] { 0, 0, 128, 63, 0, 0, 0, 64, 0, 0, 128, 64 };
        byte[] expected = new byte[] { 63, 1, 0, 64, 128, 128, 128, 0, 0, 0, 0, 0 };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 12, new TiffLimits());
        context.ByteOrder = TiffByteOrder.BigEndian;

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(expected));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Encode_AppliesFloatingPointPredictorWithContiguousRgbStride()
    {
        TiffImageInfo info = CreateFloatingInfo(2, 1, 1, 3, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] source = new byte[]
        {
            0, 0, 128, 63, 0, 0, 0, 64, 0, 0, 128, 64,
            0, 0, 192, 63, 0, 0, 32, 64, 0, 0, 144, 64
        };
        byte[] expected = new byte[]
        {
            63, 64, 64, 0, 0, 0, 65, 192, 64, 64, 32, 16,
            64, 224, 112, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 24, new TiffLimits());
        context.ByteOrder = TiffByteOrder.LittleEndian;

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(expected));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void Decode_ReportsFloatingPointPredictorWithNonFloatingSampleFormat()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 32 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        info.Predictor = 3;
        byte[] source = new byte[12];
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 12, new TiffLimits());

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Encode_ReportsFloatingPointPredictorWithUnsupportedBitsPerSample()
    {
        TiffImageInfo info = CreateFloatingInfo(3, 1, 1, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        info.BitsPerSample = new int[] { 8 };
        byte[] source = new byte[3];
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, 3, new TiffLimits());

        TiffCodecResult result = TiffCodec.Encode(context, source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip, int samplesPerPixel, int[] bitsPerSample, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.PlanarConfiguration = planarConfiguration;
        info.Compression = compression;
        info.Predictor = 2;
        return info;
    }

    static TiffImageInfo CreateFloatingInfo(ulong width, ulong length, ulong rowsPerStrip, int samplesPerPixel, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        TiffImageInfo info = CreateInfo(width, length, rowsPerStrip, samplesPerPixel, CreateFilledBitsPerSample(32, samplesPerPixel), planarConfiguration, compression);
        info.Predictor = 3;
        info.SampleFormats = CreateFilledSampleFormats(TiffSampleFormat.IeeeFloatingPoint, samplesPerPixel);
        return info;
    }

    static int[] CreateFilledBitsPerSample(int bitsPerSample, int samplesPerPixel)
    {
        int[] values = new int[samplesPerPixel];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = bitsPerSample;
            index = index + 1;
        }

        return values;
    }

    static TiffSampleFormat[] CreateFilledSampleFormats(TiffSampleFormat sampleFormat, int samplesPerPixel)
    {
        TiffSampleFormat[] values = new TiffSampleFormat[samplesPerPixel];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = sampleFormat;
            index = index + 1;
        }

        return values;
    }
}

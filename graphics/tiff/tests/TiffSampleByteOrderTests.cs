using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSampleByteOrderTests
{
    [Test]
    public void Decode_SwapsBigEndianSixteenBitSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 });
        TiffCodecContext context = CreateContext(info, 4, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 18, 52, 171, 205 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 52, 18, 205, 171 }));
    }

    [Test]
    public void Encode_SwapsSixteenBitSamplesToBigEndian()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 });
        TiffCodecContext context = CreateContext(info, 4, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 52, 18, 205, 171 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 18, 52, 171, 205 }));
    }

    [Test]
    public void Decode_SwapsBigEndianThirtyTwoBitSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 32 });
        TiffCodecContext context = CreateContext(info, 4, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 18, 52, 86, 120 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 120, 86, 52, 18 }));
    }

    [Test]
    public void Decode_SwapsBigEndianSixtyFourBitSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 64 });
        TiffCodecContext context = CreateContext(info, 8, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }));
    }

    [Test]
    public void Decode_SwapsMixedContiguousMultiByteSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 3, new int[] { 16, 8, 32 });
        TiffCodecContext context = CreateContext(info, 7, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 18, 52, 127, 1, 2, 3, 4 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 52, 18, 127, 4, 3, 2, 1 }));
    }

    [Test]
    public void Decode_DoesNotSwapLittleEndianSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 });
        TiffCodecContext context = CreateContext(info, 4, TiffByteOrder.LittleEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 52, 18, 205, 171 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 52, 18, 205, 171 }));
    }

    [Test]
    public void Decode_ReportsUnknownByteOrderForMultiByteSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 16 });
        TiffCodecContext context = CreateContext(info, 2, TiffByteOrder.Unknown);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 18, 52 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void Decode_ReportsNonByteAlignedLayoutWhenSwappingMultiByteSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 2, new int[] { 1, 16 });
        TiffCodecContext context = CreateContext(info, 3, TiffByteOrder.BigEndian);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 0, 18, 52 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Encode_AppliesByteSwapBeforeHorizontalPredictor()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 });
        info.Predictor = 2;
        TiffCodecContext context = CreateContext(info, 4, TiffByteOrder.BigEndian);
        byte[] source = new byte[] { 1, 0, 3, 0 };

        TiffCodecResult encoded = TiffCodec.Encode(context, source);
        TiffCodecResult decoded = TiffCodec.Decode(context, encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 0, 1, 0, 2 }));
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(source));
    }

    static TiffCodecContext CreateContext(TiffImageInfo info, ulong expectedDecodedByteCount, TiffByteOrder byteOrder)
    {
        TiffCodecContext context = TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, new TiffLimits());
        context.ByteOrder = byteOrder;
        return context;
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip, int samplesPerPixel, int[] bitsPerSample)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.Compression = TiffCompression.None;
        info.PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        info.Predictor = 1;
        return info;
    }
}

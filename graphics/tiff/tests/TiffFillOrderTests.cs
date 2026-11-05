using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffFillOrderTests
{
    [Test]
    public void ReverseBits_ReversesByteBitOrder()
    {
        Assert.That(TiffCodec.ReverseBits(72), Is.EqualTo((byte)18));
        Assert.That(TiffCodec.ReverseBits(113), Is.EqualTo((byte)142));
    }

    [Test]
    public void Decode_ReversesDecodedBytesForNoCompression()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.None, 1, 2);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 72 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 18 }));
    }

    [Test]
    public void Encode_ReversesDecodedBytesForNoCompression()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.None, 1, 2);

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 18 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 72 }));
    }

    [Test]
    public void Decode_ReversesDecodedBytesAfterPackBits()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.PackBits, 1, 2);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 0, 72 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 18 }));
    }

    [Test]
    public void Encode_ReversesDecodedBytesBeforePackBits()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.PackBits, 1, 2);

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 18 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 72 }));
    }

    [Test]
    public void Decode_ReversesEncodedBitsBeforeCcittModifiedHuffman()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittRle, 8, 1, 1, 2);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 113, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Encode_ReversesEncodedBitsAfterCcittModifiedHuffman()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittRle, 8, 1, 1, 2);

        TiffCodecResult result = TiffCodec.Encode(context, new byte[] { 24 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 113, 0 }));
    }

    [Test]
    public void Decode_ReportsInvalidFillOrder()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.None, 1, 3);

        TiffCodecResult result = TiffCodec.Decode(context, new byte[] { 72 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffCodecContext CreateByteContext(TiffCompression compression, ulong expectedDecodedByteCount, int fillOrder)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = compression;
        info.BitsPerSample = new int[] { 8 };
        info.FillOrder = fillOrder;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, new TiffLimits());
    }

    static TiffCodecContext CreateBilevelContext(TiffCompression compression, ulong width, ulong length, ulong expectedDecodedByteCount, int fillOrder)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.Photometric = TiffPhotometric.WhiteIsZero;
        info.Compression = compression;
        info.FillOrder = fillOrder;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, new TiffLimits());
    }
}

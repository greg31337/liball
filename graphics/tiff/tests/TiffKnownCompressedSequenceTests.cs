using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffKnownCompressedSequenceTests
{
    [Test]
    public void Decode_KnownPackBitsSequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.PackBits, 5);
        byte[] source = new byte[] { 1, 65, 66, 254, 67 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 66, 67, 67, 67 }));
    }

    [Test]
    public void Decode_KnownZLibDeflateSequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.Deflate, 6);
        byte[] source = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Decode_KnownLzwSequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.Lzw, 3);
        byte[] source = new byte[] { 128, 16, 72, 68, 56, 8 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
    }

    [Test]
    public void Decode_KnownCcittModifiedHuffmanSequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittRle, 8, 1, 1);
        byte[] source = new byte[] { 142, 0 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Decode_KnownCcittGroup3SequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittGroup3, 8, 1, 1);
        byte[] source = new byte[] { 0, 24, 224 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void Decode_KnownCcittGroup4SequenceThroughDispatcher()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittGroup4, 8, 1, 1);
        byte[] source = new byte[] { 49, 192, 4, 0, 64 };

        TiffCodecResult result = TiffCodec.Decode(context, source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    static TiffCodecContext CreateByteContext(TiffCompression compression, ulong expectedDecodedByteCount)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Compression = compression;
        info.BitsPerSample = new int[] { 8 };
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, new TiffLimits());
    }

    static TiffCodecContext CreateBilevelContext(TiffCompression compression, ulong width, ulong length, ulong expectedDecodedByteCount)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.Photometric = TiffPhotometric.WhiteIsZero;
        info.Compression = compression;
        return TiffCodecContext.Create(info, false, 0, expectedDecodedByteCount, new TiffLimits());
    }
}

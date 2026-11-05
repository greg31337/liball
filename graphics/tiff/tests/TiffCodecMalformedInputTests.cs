using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCodecMalformedInputTests
{
    [Test]
    public void PackBitsDecode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.PackBits, 4);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 1, 65 },
            new byte[] { 254 },
            new byte[] { 0 },
            new byte[] { 1, 65, 66, 1 }
        };

        AssertMalformedInputs(context, inputs);
    }

    [Test]
    public void LzwDecode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.Lzw, 1);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 128 },
            new byte[] { 128, 75, 32, 32 },
            new byte[] { 128, 16, 64 }
        };

        AssertMalformedInputs(context, inputs);
    }

    [Test]
    public void DeflateDecode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateByteContext(TiffCompression.Deflate, 6);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 120, 156, 99 },
            new byte[] { 99, 100 },
            new byte[] { 120, 156, 3, 0, 0, 0, 0, 1 }
        };

        AssertMalformedInputs(context, inputs);
    }

    [Test]
    public void CcittModifiedHuffmanDecode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittRle, 4, 1, 1);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 192 },
            new byte[] { 255 }
        };

        AssertMalformedInputs(context, inputs);
    }

    [Test]
    public void CcittGroup3Decode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittGroup3, 8, 1, 1);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 142, 0 },
            new byte[] { 0, 0, 0 }
        };

        AssertMalformedInputs(context, inputs);
    }

    [Test]
    public void CcittGroup4Decode_RejectsMalformedInputs()
    {
        TiffCodecContext context = CreateBilevelContext(TiffCompression.CcittGroup4, 8, 1, 1);
        byte[][] inputs = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 0, 0 },
            new byte[] { 8 }
        };

        AssertMalformedInputs(context, inputs);
    }

    static void AssertMalformedInputs(TiffCodecContext context, byte[][] inputs)
    {
        int index = 0;
        while (index < inputs.Length)
        {
            TiffCodecResult result = TiffCodec.Decode(context, inputs[index]);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
            Assert.That(result.Diagnostics.HasErrors(), Is.True);

            index = index + 1;
        }
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

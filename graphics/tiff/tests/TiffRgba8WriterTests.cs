using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRgba8WriterTests
{
    [Test]
    public void WriteRgba8_WritesRgbSourceSamples()
    {
        TiffImageInfo info = CreateRgbInfo(2, 1, 3);
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 6 }, destination, new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(ExtractBytes(destination, 10, 6), Is.EqualTo(new byte[] { 255, 0, 0, 0, 255, 0 }));
    }

    [Test]
    public void WriteRgba8_PremultipliesAssociatedAlphaTargets()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 4);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 4 }, destination, new byte[] { 128, 64, 32, 128 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 4), Is.EqualTo(new byte[] { 64, 32, 16, 128 }));
    }

    [Test]
    public void WriteRgba8_WritesGrayscaleSamples()
    {
        TiffImageInfo info = CreateGrayInfo(TiffPhotometric.BlackIsZero);
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new byte[] { 255, 0, 0, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(destination[10], Is.EqualTo(76));
    }

    [Test]
    public void WriteRgba8_WritesWhiteIsZeroSamples()
    {
        TiffImageInfo info = CreateGrayInfo(TiffPhotometric.WhiteIsZero);
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new byte[] { 255, 255, 255, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(destination[10], Is.EqualTo(0));
    }

    [Test]
    public void WriteRgba8_WritesNearestPaletteIndex()
    {
        TiffImageInfo info = CreateGrayInfo(TiffPhotometric.Palette);
        info.ColorMap = CreatePaletteColorMap();
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new byte[] { 255, 0, 0, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(destination[10], Is.EqualTo(1));
    }

    [Test]
    public void WriteRgba8_WritesCmykSamples()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.RowsPerStrip = 1;
        info.SamplesPerPixel = 4;
        info.BitsPerSample = new int[] { 8, 8, 8, 8 };
        info.SampleFormats = CreateFormats(4);
        info.Photometric = TiffPhotometric.Cmyk;
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 4 }, destination, new byte[] { 0, 255, 255, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 4), Is.EqualTo(new byte[] { 255, 0, 0, 0 }));
    }

    [Test]
    public void WriteRgba8_WritesYCbCrSamples()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.RowsPerStrip = 1;
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 8, 8, 8 };
        info.SampleFormats = CreateFormats(3);
        info.Photometric = TiffPhotometric.YCbCr;
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 3 }, destination, new byte[] { 255, 0, 0, 255 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 3), Is.EqualTo(new byte[] { 76, 85, 255 }).Within(1));
    }

    [Test]
    public void WriteRgba8_WritesTiledRgbSamples()
    {
        TiffImageInfo info = CreateRgbInfo(3, 2, 3);
        info.IsTiled = true;
        info.TileWidth = 2;
        info.TileLength = 2;
        byte[] destination = new byte[80];
        byte[] rgba = new byte[]
        {
            1, 2, 3, 255,
            4, 5, 6, 255,
            7, 8, 9, 255,
            10, 11, 12, 255,
            13, 14, 15, 255,
            16, 17, 18, 255
        };

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10, 40 }, new ulong[] { 12, 12 }, destination, rgba);

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 12), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13, 14, 15 }));
        Assert.That(ExtractBytes(destination, 40, 12), Is.EqualTo(new byte[] { 7, 8, 9, 0, 0, 0, 16, 17, 18, 0, 0, 0 }));
    }

    [Test]
    public void WriteRgba8_ReportsRgbaByteCountMismatch()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 3);
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 3 }, destination, new byte[] { 255, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteRgba8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 3);
        info.Photometric = TiffPhotometric.CieLab;
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba8(info, new ulong[] { 10 }, new ulong[] { 3 }, destination, new byte[] { 255, 0, 0, 255 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    static TiffImageInfo CreateRgbInfo(ulong width, ulong length, int samplesPerPixel)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = CreateBits(samplesPerPixel);
        info.SampleFormats = CreateFormats(samplesPerPixel);
        info.Photometric = TiffPhotometric.Rgb;
        return info;
    }

    static TiffImageInfo CreateGrayInfo(TiffPhotometric photometric)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.RowsPerStrip = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        info.Photometric = photometric;
        return info;
    }

    static int[] CreateBits(int count)
    {
        int[] bits = new int[count];
        int index = 0;
        while (index < count)
        {
            bits[index] = 8;
            index = index + 1;
        }

        return bits;
    }

    static TiffSampleFormat[] CreateFormats(int count)
    {
        TiffSampleFormat[] formats = new TiffSampleFormat[count];
        int index = 0;
        while (index < count)
        {
            formats[index] = TiffSampleFormat.UnsignedInteger;
            index = index + 1;
        }

        return formats;
    }

    static ushort[] CreatePaletteColorMap()
    {
        ushort[] colorMap = new ushort[256 * 3];
        colorMap[1] = 65535;
        return colorMap;
    }

    static byte[] ExtractBytes(byte[] source, int offset, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = source[offset + index];
            index = index + 1;
        }

        return bytes;
    }
}

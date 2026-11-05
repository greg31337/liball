using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRgba16WriterTests
{
    [Test]
    public void WriteRgba16_WritesRgb16SourceSamples()
    {
        TiffImageInfo info = CreateRgbInfo(2, 1, 3, 16);
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 12 }, destination, new ushort[] { 65535, 0, 0, 65535, 0, 65535, 0, 65535 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(12));
        Assert.That(ExtractBytes(destination, 10, 12), Is.EqualTo(new byte[] { 255, 255, 0, 0, 0, 0, 0, 0, 255, 255, 0, 0 }));
    }

    [Test]
    public void WriteRgba16_PremultipliesAssociatedAlphaTargets()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 4, 16);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 8 }, destination, new ushort[] { 65535, 32768, 16384, 32768 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 8), Is.EqualTo(new byte[] { 0, 128, 0, 64, 0, 32, 0, 128 }));
    }

    [Test]
    public void WriteRgba16_WritesGrayscaleSamples()
    {
        TiffImageInfo info = CreateGrayInfo(TiffPhotometric.BlackIsZero);
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new ushort[] { 65535, 0, 0, 65535 });

        Assert.That(result.Success, Is.True);
        Assert.That(destination[10], Is.EqualTo(76));
    }

    [Test]
    public void WriteRgba16_WritesNearestPaletteIndex()
    {
        TiffImageInfo info = CreateGrayInfo(TiffPhotometric.Palette);
        info.ColorMap = CreatePaletteColorMap();
        byte[] destination = new byte[30];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new ushort[] { 65535, 0, 0, 65535 });

        Assert.That(result.Success, Is.True);
        Assert.That(destination[10], Is.EqualTo(1));
    }

    [Test]
    public void WriteRgba16_WritesCmykSamples()
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

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 4 }, destination, new ushort[] { 0, 65535, 65535, 65535 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 4), Is.EqualTo(new byte[] { 255, 0, 0, 0 }));
    }

    [Test]
    public void WriteRgba16_WritesYCbCrSamples()
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

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 3 }, destination, new ushort[] { 65535, 0, 0, 65535 });

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 3), Is.EqualTo(new byte[] { 76, 85, 255 }).Within(1));
    }

    [Test]
    public void WriteRgba16_WritesTiledRgb16Samples()
    {
        TiffImageInfo info = CreateRgbInfo(3, 2, 3, 16);
        info.IsTiled = true;
        info.TileWidth = 2;
        info.TileLength = 2;
        byte[] destination = new byte[120];
        ushort[] rgba = new ushort[]
        {
            1, 2, 3, 65535,
            4, 5, 6, 65535,
            7, 8, 9, 65535,
            10, 11, 12, 65535,
            13, 14, 15, 65535,
            16, 17, 18, 65535
        };

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10, 70 }, new ulong[] { 24, 24 }, destination, rgba);

        Assert.That(result.Success, Is.True);
        Assert.That(ExtractBytes(destination, 10, 24), Is.EqualTo(new byte[] { 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 10, 0, 11, 0, 12, 0, 13, 0, 14, 0, 15, 0 }));
        Assert.That(ExtractBytes(destination, 70, 24), Is.EqualTo(new byte[] { 7, 0, 8, 0, 9, 0, 0, 0, 0, 0, 0, 0, 16, 0, 17, 0, 18, 0, 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void WriteRgba16_ReportsRgbaSampleCountMismatch()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 3, 16);
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 6 }, destination, new ushort[] { 65535, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteRgba16_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateRgbInfo(1, 1, 3, 16);
        info.Photometric = TiffPhotometric.CieLab;
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteRgba16(info, new ulong[] { 10 }, new ulong[] { 6 }, destination, new ushort[] { 65535, 0, 0, 65535 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    static TiffImageInfo CreateRgbInfo(ulong width, ulong length, int samplesPerPixel, int bitsPerSample)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = CreateBits(samplesPerPixel, bitsPerSample);
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

    static int[] CreateBits(int count, int bitsPerSample)
    {
        int[] bits = new int[count];
        int index = 0;
        while (index < count)
        {
            bits[index] = bitsPerSample;
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

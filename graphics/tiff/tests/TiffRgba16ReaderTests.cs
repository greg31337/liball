using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRgba16ReaderTests
{
    [Test]
    public void ReadRgba16_ReadsContiguousRgb16Strip()
    {
        TiffImageInfo info = CreateRgb16Info(2, 1);
        byte[] fileBytes = WithPrefix(new byte[]
        {
            255, 255, 0, 0, 0, 0,
            0, 0, 255, 255, 0, 0
        });

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 12 }, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Width, Is.EqualTo(2));
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result.RowByteCount, Is.EqualTo(16));
        Assert.That(result.Samples, Is.EqualTo(new ushort[] { 65535, 0, 0, 65535, 0, 65535, 0, 65535 }));
    }

    [Test]
    public void ReadRgba16_ReadsGrayscaleWithUnassociatedAlpha()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 2;
        info.BitsPerSample = new int[] { 8, 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger };
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        info.Photometric = TiffPhotometric.BlackIsZero;
        info.RowsPerStrip = 1;

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 2 }, WithPrefix(new byte[] { 128, 64 }));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Samples, Is.EqualTo(new ushort[] { 32896, 32896, 32896, 16448 }));
    }

    [Test]
    public void ReadRgba16_UnassociatesAssociatedAlpha()
    {
        TiffImageInfo info = CreateRgb16Info(1, 1);
        info.SamplesPerPixel = 4;
        info.BitsPerSample = new int[] { 16, 16, 16, 16 };
        info.SampleFormats = CreateFormats(TiffSampleFormat.UnsignedInteger, 4);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        byte[] fileBytes = WithPrefix(new byte[]
        {
            0, 64,
            0, 32,
            0, 16,
            0, 128
        });

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 8 }, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Samples, Is.EqualTo(new ushort[] { 32768, 16384, 8192, 32768 }));
    }

    [Test]
    public void ReadRgba16_ExpandsPaletteStrip()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 2;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.Palette;
        info.RowsPerStrip = 1;
        info.ColorMap = CreateTwoHundredFiftySixEntryColorMap();

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 2 }, WithPrefix(new byte[] { 0, 1 }));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Samples, Is.EqualTo(new ushort[] { 0, 0, 0, 65535, 65535, 0, 0, 65535 }));
    }

    [Test]
    public void ReadRgba16_InterleavesSeparateRgbPlanes()
    {
        TiffImageInfo info = CreateRgb16Info(2, 1);
        info.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        byte[] fileBytes = WithPrefix(new byte[]
        {
            255, 255, 0, 0,
            0, 0, 255, 255,
            0, 0, 0, 0
        });

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8, 12, 16 }, new ulong[] { 4, 4, 4 }, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Samples, Is.EqualTo(new ushort[] { 65535, 0, 0, 65535, 0, 65535, 0, 65535 }));
    }

    [Test]
    public void ReadRgba16_AppliesOrientation()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 2;
        info.Length = 2;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.BlackIsZero;
        info.RowsPerStrip = 2;
        info.Orientation = TiffOrientation.BottomRight;

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 4 }, WithPrefix(new byte[] { 1, 2, 3, 4 }));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Samples, Is.EqualTo(new ushort[]
        {
            1028, 1028, 1028, 65535,
            771, 771, 771, 65535,
            514, 514, 514, 65535,
            257, 257, 257, 65535
        }));
    }

    [Test]
    public void ReadRgba16_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateRgb16Info(1, 1);
        info.Photometric = TiffPhotometric.ColorFilterArray;

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 6 }, WithPrefix(new byte[] { 1, 0, 2, 0, 3, 0 }));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Samples, Is.EqualTo(new ushort[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ReadRgba16_ReportsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateRgb16Info(1, 1);

        TiffRgba16ReadResult result = TiffCodec.ReadRgba16(info, new ulong[] { 8 }, new ulong[] { 5 }, WithPrefix(new byte[] { 1, 0, 2, 0, 3 }));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Samples, Is.EqualTo(new ushort[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffImageInfo CreateRgb16Info(ulong width, ulong length)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 16, 16, 16 };
        info.SampleFormats = CreateFormats(TiffSampleFormat.UnsignedInteger, 3);
        info.Photometric = TiffPhotometric.Rgb;
        info.RowsPerStrip = length;
        return info;
    }

    static TiffSampleFormat[] CreateFormats(TiffSampleFormat sampleFormat, int count)
    {
        TiffSampleFormat[] values = new TiffSampleFormat[count];
        int index = 0;
        while (index < count)
        {
            values[index] = sampleFormat;
            index = index + 1;
        }

        return values;
    }

    static ushort[] CreateTwoHundredFiftySixEntryColorMap()
    {
        ushort[] colorMap = new ushort[256 * 3];
        colorMap[1] = 65535;
        return colorMap;
    }

    static byte[] WithPrefix(byte[] data)
    {
        byte[] bytes = new byte[data.Length + 8];
        int index = 0;
        while (index < data.Length)
        {
            bytes[index + 8] = data[index];
            index = index + 1;
        }

        return bytes;
    }
}

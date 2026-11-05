using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSourceRasterWriterTests
{
    [Test]
    public void WriteSourceRaster_WritesContiguousRowsAcrossStrips()
    {
        TiffImageInfo info = CreateGrayStripInfo(3, 2, 1);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 10, 20 };
        ulong[] byteCounts = new ulong[] { 3, 3 };

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, offsets, byteCounts, destination, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.SegmentCount, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(destination[10], Is.EqualTo(1));
        Assert.That(destination[12], Is.EqualTo(3));
        Assert.That(destination[20], Is.EqualTo(4));
        Assert.That(destination[22], Is.EqualTo(6));
    }

    [Test]
    public void WriteSourceRaster_WritesContiguousTilesWithEdgePadding()
    {
        TiffImageInfo info = CreateGrayTileInfo(3, 2, 2, 2);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 10, 20 };
        ulong[] byteCounts = new ulong[] { 4, 4 };

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, offsets, byteCounts, destination, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.SegmentCount, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(8));
        Assert.That(ExtractBytes(destination, 10, 4), Is.EqualTo(new byte[] { 1, 2, 4, 5 }));
        Assert.That(ExtractBytes(destination, 20, 4), Is.EqualTo(new byte[] { 3, 0, 6, 0 }));
    }

    [Test]
    public void WriteSourceRaster_DeinterleavesSeparatePlanarStrips()
    {
        TiffImageInfo info = CreateRgbSeparateStripInfo(2, 1);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 10, 20, 30 };
        ulong[] byteCounts = new ulong[] { 2, 2, 2 };

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, offsets, byteCounts, destination, new byte[] { 255, 0, 0, 0, 255, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.SegmentCount, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(ExtractBytes(destination, 10, 2), Is.EqualTo(new byte[] { 255, 0 }));
        Assert.That(ExtractBytes(destination, 20, 2), Is.EqualTo(new byte[] { 0, 255 }));
        Assert.That(ExtractBytes(destination, 30, 2), Is.EqualTo(new byte[] { 0, 0 }));
    }

    [Test]
    public void WriteSourceRaster_DelegatesCompressionToSegmentWriter()
    {
        TiffImageInfo info = CreateGrayStripInfo(6, 1, 1);
        info.Compression = TiffCompression.PackBits;
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 10 };
        ulong[] byteCounts = new ulong[] { 7 };

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, offsets, byteCounts, destination, new byte[] { 10, 11, 7, 7, 7, 12 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(7));
        Assert.That(ExtractBytes(destination, 10, 7), Is.EqualTo(new byte[] { 1, 10, 11, 254, 7, 0, 12 }));
    }

    [Test]
    public void WriteSourceRaster_ReportsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateGrayStripInfo(3, 2, 2);
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, new ulong[] { 10 }, new ulong[] { 6 }, destination, new byte[] { 1, 2, 3, 4, 5 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteSourceRaster_ReportsSubByteSamplesUnsupported()
    {
        TiffImageInfo info = CreateGrayStripInfo(8, 1, 1);
        info.BitsPerSample = new int[] { 1 };
        byte[] destination = new byte[20];

        TiffSourceRasterWriteResult result = TiffCodec.WriteSourceRaster(info, new ulong[] { 10 }, new ulong[] { 1 }, destination, new byte[] { 170 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateGrayStripInfo(ulong width, ulong length, ulong rowsPerStrip)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.BlackIsZero;
        return info;
    }

    static TiffImageInfo CreateGrayTileInfo(ulong width, ulong length, ulong tileWidth, ulong tileLength)
    {
        TiffImageInfo info = CreateGrayStripInfo(width, length, length);
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
        return info;
    }

    static TiffImageInfo CreateRgbSeparateStripInfo(ulong width, ulong length)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 8, 8, 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.Rgb;
        info.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        return info;
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

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSourceRasterReaderTests
{
    [Test]
    public void ReadSourceRaster_ReadsContiguousRgb16Strip()
    {
        TiffImageInfo info = CreateRgb16Info(2, 1);
        byte[] source = new byte[]
        {
            255, 255, 0, 0, 0, 0,
            0, 0, 255, 255, 0, 0
        };

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8 }, new ulong[] { 12 }, WithPrefix(source));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Width, Is.EqualTo(2));
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result.SamplesPerPixel, Is.EqualTo(3));
        Assert.That(result.BitsPerSample, Is.EqualTo(new int[] { 16, 16, 16 }));
        Assert.That(result.SampleFormats, Is.EqualTo(CreateFormats(TiffSampleFormat.UnsignedInteger, 3)));
        Assert.That(result.RowByteCount, Is.EqualTo(12));
        Assert.That(result.TotalByteCount, Is.EqualTo(12));
        Assert.That(result.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void ReadSourceRaster_FromDirectoryExtractsInfoAndOffsets()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] source = new byte[] { 1, 10, 100, 2, 20, 101 };
        byte[] fileBytes = WithPrefix(source);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 1, byteOrder),
            CreateShortEntry(TiffTagId.SamplesPerPixel, 3, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.Rgb, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 1, byteOrder),
            CreateShortEntry(TiffTagId.StripOffsets, 8, byteOrder),
            CreateShortEntry(TiffTagId.StripByteCounts, 6, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(directory, fileBytes, byteOrder);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Width, Is.EqualTo(2));
        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result.SamplesPerPixel, Is.EqualTo(3));
        Assert.That(result.BitsPerSample, Is.EqualTo(new int[] { 8, 8, 8 }));
        Assert.That(result.Photometric, Is.EqualTo(TiffPhotometric.Rgb));
        Assert.That(result.RowByteCount, Is.EqualTo(6));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void ReadSourceRaster_InterleavesSeparateRgbPlanes()
    {
        TiffImageInfo info = CreateRgb8Info(2, 1);
        info.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        byte[] fileBytes = WithPrefix(new byte[] { 255, 0, 0, 255, 0, 0 });

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8, 10, 12 }, new ulong[] { 2, 2, 2 }, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RowByteCount, Is.EqualTo(6));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 255, 0, 0, 0, 255, 0 }));
    }

    [Test]
    public void ReadSourceRaster_ReadsTiledRgbImage()
    {
        TiffImageInfo info = CreateRgb8Info(2, 2);
        info.IsTiled = true;
        info.TileWidth = 2;
        info.TileLength = 2;
        byte[] source = new byte[]
        {
            1, 2, 3,
            4, 5, 6,
            7, 8, 9,
            10, 11, 12
        };

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8 }, new ulong[] { 12 }, WithPrefix(source));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(source));
    }

    [Test]
    public void ReadSourceRaster_ReadsTiledRgbImageWithPartialEdgeTiles()
    {
        TiffImageInfo info = CreateRgb8Info(3, 3);
        info.IsTiled = true;
        info.TileWidth = 2;
        info.TileLength = 2;
        byte[] topLeftTile = new byte[]
        {
            1, 10, 100,
            2, 20, 101,
            4, 40, 103,
            5, 50, 104
        };
        byte[] topRightTile = new byte[]
        {
            3, 30, 102,
            200, 200, 200,
            6, 60, 105,
            201, 201, 201
        };
        byte[] bottomLeftTile = new byte[]
        {
            7, 70, 106,
            8, 80, 107,
            202, 202, 202,
            203, 203, 203
        };
        byte[] bottomRightTile = new byte[]
        {
            9, 90, 108,
            204, 204, 204,
            205, 205, 205,
            206, 206, 206
        };
        byte[] fileBytes = WithPrefix(Concat(topLeftTile, topRightTile, bottomLeftTile, bottomRightTile));

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(
            info,
            new ulong[] { 8, 20, 32, 44 },
            new ulong[] { 12, 12, 12, 12 },
            fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result.RowByteCount, Is.EqualTo(9));
        Assert.That(result.TotalByteCount, Is.EqualTo(27));
        Assert.That(result.Bytes, Is.EqualTo(new byte[]
        {
            1, 10, 100,
            2, 20, 101,
            3, 30, 102,
            4, 40, 103,
            5, 50, 104,
            6, 60, 105,
            7, 70, 106,
            8, 80, 107,
            9, 90, 108
        }));
    }

    [Test]
    public void ReadSourceRaster_AppliesOrientationWithNativePixelWidth()
    {
        TiffImageInfo info = CreateRgb8Info(2, 2);
        info.Orientation = TiffOrientation.BottomRight;
        byte[] source = new byte[]
        {
            1, 10, 100,
            2, 20, 101,
            3, 30, 102,
            4, 40, 103
        };

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8 }, new ulong[] { 12 }, WithPrefix(source));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[]
        {
            4, 40, 103,
            3, 30, 102,
            2, 20, 101,
            1, 10, 100
        }));
    }

    [Test]
    public void ReadSourceRaster_ReportsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateRgb8Info(2, 1);

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8 }, new ulong[] { 5 }, WithPrefix(new byte[] { 1, 2, 3, 4, 5 }));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadSourceRaster_ReportsTiledSeparatePlaneOffsetCountMismatch()
    {
        TiffImageInfo info = CreateRgb8Info(3, 3);
        info.IsTiled = true;
        info.TileWidth = 2;
        info.TileLength = 2;
        info.PlanarConfiguration = TiffPlanarConfiguration.Separate;

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(
            info,
            new ulong[] { 8, 20, 32, 44 },
            new ulong[] { 12, 12, 12, 12 },
            WithPrefix(new byte[48]));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result.RowByteCount, Is.EqualTo(0));
        Assert.That(result.TotalByteCount, Is.EqualTo(0));
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.TileOffsets));
    }

    [Test]
    public void ReadSourceRaster_ReadsSubByteRasterAsUnpackedSamples()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 8;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 1 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.BlackIsZero;
        info.RowsPerStrip = 1;

        TiffSourceRasterReadResult result = TiffCodec.ReadSourceRaster(info, new ulong[] { 8 }, new ulong[] { 1 }, WithPrefix(new byte[] { 170 }));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 0, 1, 0, 1, 0, 1, 0 }));
    }

    static TiffImageInfo CreateRgb8Info(ulong width, ulong length)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 8, 8, 8 };
        info.SampleFormats = CreateFormats(TiffSampleFormat.UnsignedInteger, 3);
        info.Photometric = TiffPhotometric.Rgb;
        info.RowsPerStrip = length;
        return info;
    }

    static TiffImageInfo CreateRgb16Info(ulong width, ulong length)
    {
        TiffImageInfo info = CreateRgb8Info(width, length);
        info.BitsPerSample = new int[] { 16, 16, 16 };
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

    static TiffIfdEntry CreateShortEntry(int tagId, ushort value, TiffByteOrder byteOrder)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Short;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueByteCount = 2;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt16(entry.ValueOffsetBytes.AsSpan(0, 2), value, byteOrder);
        return entry;
    }

    static byte[] Concat(params byte[][] arrays)
    {
        int length = 0;
        int arrayIndex = 0;
        while (arrayIndex < arrays.Length)
        {
            length = length + arrays[arrayIndex].Length;
            arrayIndex = arrayIndex + 1;
        }

        byte[] bytes = new byte[length];
        int destinationIndex = 0;
        arrayIndex = 0;
        while (arrayIndex < arrays.Length)
        {
            byte[] source = arrays[arrayIndex];
            int sourceIndex = 0;
            while (sourceIndex < source.Length)
            {
                bytes[destinationIndex] = source[sourceIndex];
                destinationIndex = destinationIndex + 1;
                sourceIndex = sourceIndex + 1;
            }

            arrayIndex = arrayIndex + 1;
        }

        return bytes;
    }
}

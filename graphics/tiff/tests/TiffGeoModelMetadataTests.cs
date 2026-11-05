using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeoModelMetadataTests
{
    [Test]
    public void ReadGeoModel_ParsesModelTags()
    {
        byte[] fileBytes = new byte[512];
        WriteDoubles(fileBytes, 64, new double[] { 30, 30, 0 }, TiffByteOrder.LittleEndian);
        WriteDoubles(fileBytes, 96, new double[] { 0, 0, 0, 100, 200, 0, 10, 20, 0, 400, 500, 0 }, TiffByteOrder.LittleEndian);
        WriteDoubles(fileBytes, 224, new double[] { 1, 0, 0, 100, 0, -1, 0, 200, 0, 0, 1, 0, 0, 0, 0, 1 }, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ModelPixelScale, TiffDataType.Double, 3, 64),
            CreateEntry(TiffTagId.ModelTiepoint, TiffDataType.Double, 12, 96),
            CreateEntry(TiffTagId.ModelTransformation, TiffDataType.Double, 16, 224)
        });

        TiffGeoModelReadResult result = TiffCodec.ReadGeoModel(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasPixelScale, Is.True);
        Assert.That(result.Metadata.PixelScale, Is.EqualTo(new double[] { 30, 30, 0 }));
        Assert.That(result.Metadata.HasTiepoints, Is.True);
        Assert.That(result.Metadata.TiepointCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.Tiepoints[0].RasterI, Is.EqualTo(0));
        Assert.That(result.Metadata.Tiepoints[0].ModelX, Is.EqualTo(100));
        Assert.That(result.Metadata.Tiepoints[1].RasterJ, Is.EqualTo(20));
        Assert.That(result.Metadata.Tiepoints[1].ModelY, Is.EqualTo(500));
        Assert.That(result.Metadata.HasTransformation, Is.True);
        Assert.That(result.Metadata.Transformation[5], Is.EqualTo(-1));
        Assert.That(result.Metadata.Transformation[15], Is.EqualTo(1));
    }

    [Test]
    public void ReadGeoModel_ReturnsEmptyWhenTagsAreMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffGeoModelReadResult result = TiffCodec.ReadGeoModel(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasPixelScale, Is.False);
        Assert.That(result.Metadata.HasTiepoints, Is.False);
        Assert.That(result.Metadata.HasTransformation, Is.False);
        Assert.That(result.Metadata.TiepointCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadGeoModel_ReportsInvalidPixelScaleType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ModelPixelScale, TiffDataType.Long, 3, 64) });

        TiffGeoModelReadResult result = TiffCodec.ReadGeoModel(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ModelPixelScale));
    }

    [Test]
    public void ReadGeoModel_ReportsInvalidTiepointCount()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ModelTiepoint, TiffDataType.Double, 5, 64) });

        TiffGeoModelReadResult result = TiffCodec.ReadGeoModel(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ModelTiepoint));
    }

    [Test]
    public void ReadGeoModel_ReportsExternalValueRangeOutsideFile()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ModelTransformation, TiffDataType.Double, 16, 64) });

        TiffGeoModelReadResult result = TiffCodec.ReadGeoModel(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static void WriteDoubles(byte[] bytes, int offset, double[] values, TiffByteOrder byteOrder)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset + (index * 8), 8), values[index], byteOrder);
            index = index + 1;
        }
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeoKeyDirectoryTests
{
    [Test]
    public void ReadGeoKeyDirectory_ParsesHeaderAndKeys()
    {
        byte[] fileBytes = new byte[128];
        ushort[] values = new ushort[]
        {
            1, 1, 1, 3,
            1024, 0, 1, 1,
            3072, 0, 1, 32633,
            2049, 34737, 12, 0
        };
        WriteShorts(fileBytes, 64, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Short, values.Length, 64) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Directory.HasDirectory, Is.True);
        Assert.That(result.Directory.KeyDirectoryVersion, Is.EqualTo(1));
        Assert.That(result.Directory.KeyRevision, Is.EqualTo(1));
        Assert.That(result.Directory.MinorRevision, Is.EqualTo(1));
        Assert.That(result.Directory.NumberOfKeys, Is.EqualTo(3));
        Assert.That(result.Directory.KeyCount(), Is.EqualTo(3));
        Assert.That(result.Directory.Keys[0].KeyId, Is.EqualTo(1024));
        Assert.That(result.Directory.Keys[0].IsInlineValue(), Is.True);
        Assert.That(result.Directory.Keys[0].ValueOffset, Is.EqualTo(1));
        Assert.That(result.Directory.Keys[1].KeyId, Is.EqualTo(3072));
        Assert.That(result.Directory.Keys[1].ValueOffset, Is.EqualTo(32633));
        Assert.That(result.Directory.Keys[2].TiffTagLocation, Is.EqualTo(TiffTagId.GeoAsciiParams));
        Assert.That(result.Directory.Keys[2].Count, Is.EqualTo(12));
    }

    [Test]
    public void ReadGeoKeyDirectory_ReturnsEmptyWhenTagIsMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Directory.HasDirectory, Is.False);
        Assert.That(result.Directory.KeyCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadGeoKeyDirectory_ReportsInvalidTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Long, 4, 64) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    [Test]
    public void ReadGeoKeyDirectory_ReportsInvalidDirectoryShape()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Short, 7, 64) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    [Test]
    public void ReadGeoKeyDirectory_ReportsHeaderKeyCountMismatch()
    {
        byte[] fileBytes = new byte[128];
        ushort[] values = new ushort[]
        {
            1, 1, 1, 2,
            1024, 0, 1, 1
        };
        WriteShorts(fileBytes, 64, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Short, values.Length, 64) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    [Test]
    public void ReadGeoKeyDirectory_ReportsExternalValueRangeOutsideFile()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Short, 8, 124) });

        TiffGeoKeyDirectoryReadResult result = TiffCodec.ReadGeoKeyDirectory(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static void WriteShorts(byte[] bytes, int offset, ushort[] values, TiffByteOrder byteOrder)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + (index * 2), 2), values[index], byteOrder);
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

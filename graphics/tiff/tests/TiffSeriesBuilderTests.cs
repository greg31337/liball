using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSeriesBuilderTests
{
    [Test]
    public void GroupSeries_UsesImageJHyperstackPlaneCount()
    {
        string description = "ImageJ=1.54f\nimages=6\nchannels=2\nslices=3\nframes=1\nhyperstack=true\n";
        byte[] fileBytes = new byte[256];
        byte[] encoded = TiffAscii.EncodeString(description);
        WriteBytes(fileBytes, 128, encoded);
        TiffDirectory[] directories = new TiffDirectory[]
        {
            CreateDirectory(0, 100, 50, true, 128, encoded.Length),
            CreateDirectory(1, 100, 50, false, 0, 0),
            CreateDirectory(2, 100, 50, false, 0, 0),
            CreateDirectory(3, 100, 50, false, 0, 0),
            CreateDirectory(4, 100, 50, false, 0, 0),
            CreateDirectory(5, 100, 50, false, 0, 0)
        };

        TiffSeriesGroupResult result = TiffCodec.GroupSeries(directories, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SeriesCount(), Is.EqualTo(1));
        Assert.That(result.Series[0].HasMultidimensionalMetadata, Is.True);
        Assert.That(result.Series[0].SourceMetadata, Is.EqualTo("ImageJ"));
        Assert.That(result.Series[0].SizeX, Is.EqualTo(100));
        Assert.That(result.Series[0].SizeY, Is.EqualTo(50));
        Assert.That(result.Series[0].SizeC, Is.EqualTo(2));
        Assert.That(result.Series[0].SizeZ, Is.EqualTo(3));
        Assert.That(result.Series[0].SizeT, Is.EqualTo(1));
        Assert.That(result.Series[0].PlaneCount(), Is.EqualTo(6));
        Assert.That(result.Series[0].Planes[5].ImageIndex, Is.EqualTo(5));
    }

    [Test]
    public void GroupSeries_StartsNewSeriesWhenDimensionsChange()
    {
        TiffDirectory[] directories = new TiffDirectory[]
        {
            CreateDirectory(0, 100, 50, false, 0, 0),
            CreateDirectory(1, 100, 50, false, 0, 0),
            CreateDirectory(2, 25, 25, false, 0, 0),
            CreateDirectory(3, 25, 25, false, 0, 0),
            CreateDirectory(4, 100, 50, false, 0, 0)
        };

        TiffSeriesGroupResult result = TiffCodec.GroupSeries(directories, new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SeriesCount(), Is.EqualTo(3));
        Assert.That(result.Series[0].PlaneCount(), Is.EqualTo(2));
        Assert.That(result.Series[0].SizeX, Is.EqualTo(100));
        Assert.That(result.Series[1].PlaneCount(), Is.EqualTo(2));
        Assert.That(result.Series[1].SizeX, Is.EqualTo(25));
        Assert.That(result.Series[2].PlaneCount(), Is.EqualTo(1));
        Assert.That(result.Series[2].SizeX, Is.EqualTo(100));
    }

    [Test]
    public void GroupSeries_UsesImageJDimensionProductWhenImageCountIsMissing()
    {
        string description = "ImageJ=1.54f\nchannels=2\nslices=2\nframes=2\nhyperstack=true\n";
        byte[] fileBytes = new byte[256];
        byte[] encoded = TiffAscii.EncodeString(description);
        WriteBytes(fileBytes, 128, encoded);
        TiffDirectory[] directories = new TiffDirectory[]
        {
            CreateDirectory(0, 100, 50, true, 128, encoded.Length),
            CreateDirectory(1, 100, 50, false, 0, 0),
            CreateDirectory(2, 100, 50, false, 0, 0),
            CreateDirectory(3, 100, 50, false, 0, 0),
            CreateDirectory(4, 100, 50, false, 0, 0),
            CreateDirectory(5, 100, 50, false, 0, 0),
            CreateDirectory(6, 100, 50, false, 0, 0),
            CreateDirectory(7, 100, 50, false, 0, 0)
        };

        TiffSeriesGroupResult result = TiffCodec.GroupSeries(directories, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SeriesCount(), Is.EqualTo(1));
        Assert.That(result.Series[0].PlaneCount(), Is.EqualTo(8));
    }

    [Test]
    public void GroupSeries_ReportsInvalidByteOrder()
    {
        TiffSeriesGroupResult result = TiffCodec.GroupSeries(new TiffDirectory[0], new byte[0], TiffByteOrder.Unknown);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void GroupSeries_ReportsMissingDirectoryArray()
    {
        TiffSeriesGroupResult result = TiffCodec.GroupSeries(null!, new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static TiffDirectory CreateDirectory(int imageIndex, uint width, uint length, bool includeDescription, uint descriptionOffset, int descriptionCount)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[2];
        if (includeDescription)
        {
            entries = new TiffIfdEntry[3];
        }

        entries[0] = CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        entries[1] = CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        if (includeDescription)
        {
            entries[2] = CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, descriptionCount, descriptionOffset);
        }

        return TiffDirectory.Create(imageIndex, (ulong)(8 + (imageIndex * 32)), 0, entries);
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
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static void WriteBytes(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
    }
}

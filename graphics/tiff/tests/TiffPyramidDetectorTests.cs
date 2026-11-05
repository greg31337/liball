using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPyramidDetectorTests
{
    [Test]
    public void DetectPyramid_UsesReducedSubIfdLevels()
    {
        TiffDirectory parent = CreateDirectory(0, 1024, 1024, false);
        TiffSubIfd[] subIfds = new TiffSubIfd[]
        {
            TiffSubIfd.Create(0, 8, 0, 1, 128, new TiffDirectory[]
            {
                CreateDirectory(1, 512, 512, true),
                CreateDirectory(2, 256, 256, true)
            })
        };

        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(parent, subIfds, new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pyramid.HasPyramid, Is.True);
        Assert.That(result.Pyramid.ParentImageIndex, Is.EqualTo(0));
        Assert.That(result.Pyramid.BaseWidth, Is.EqualTo(1024));
        Assert.That(result.Pyramid.BaseLength, Is.EqualTo(1024));
        Assert.That(result.Pyramid.LevelCount(), Is.EqualTo(2));
        Assert.That(result.Pyramid.Levels[0].Width, Is.EqualTo(512));
        Assert.That(result.Pyramid.Levels[0].IsReducedResolution, Is.True);
        Assert.That(result.Pyramid.Levels[1].Width, Is.EqualTo(256));
        Assert.That(result.Pyramid.Levels[1].SourceImageIndex, Is.EqualTo(2));
    }

    [Test]
    public void DetectPyramid_ReadsSubIfdsFromParentDirectory()
    {
        byte[] fileBytes = new byte[256];
        WriteClassicDirectory(fileBytes, 128, 512, 512, true);
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 1024),
            CreateEntry(TiffTagId.SubIFDs, TiffDataType.Long, 1, 128)
        });

        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(parent, fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pyramid.HasPyramid, Is.True);
        Assert.That(result.Pyramid.LevelCount(), Is.EqualTo(1));
        Assert.That(result.Pyramid.Levels[0].Width, Is.EqualTo(512));
    }

    [Test]
    public void DetectPyramid_TreatsSmallerSubIfdAsOverviewWithoutFlag()
    {
        TiffDirectory parent = CreateDirectory(0, 1024, 1024, false);
        TiffSubIfd[] subIfds = new TiffSubIfd[]
        {
            TiffSubIfd.Create(0, 8, 0, 1, 128, new TiffDirectory[] { CreateDirectory(1, 512, 512, false) })
        };

        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(parent, subIfds, new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pyramid.HasPyramid, Is.True);
        Assert.That(result.Pyramid.LevelCount(), Is.EqualTo(1));
    }

    [Test]
    public void DetectPyramid_IgnoresSubIfdWithFullResolutionAndNoFlag()
    {
        TiffDirectory parent = CreateDirectory(0, 1024, 1024, false);
        TiffSubIfd[] subIfds = new TiffSubIfd[]
        {
            TiffSubIfd.Create(0, 8, 0, 1, 128, new TiffDirectory[] { CreateDirectory(1, 1024, 1024, false) })
        };

        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(parent, subIfds, new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pyramid.HasPyramid, Is.False);
        Assert.That(result.Pyramid.LevelCount(), Is.EqualTo(0));
    }

    [Test]
    public void DetectPyramid_ReportsInvalidByteOrder()
    {
        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(CreateDirectory(0, 1024, 1024, false), new TiffSubIfd[0], new byte[0], TiffByteOrder.Unknown);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void DetectPyramid_ReportsMissingParentDirectory()
    {
        TiffPyramidDetectionResult result = TiffCodec.DetectPyramid(null!, new TiffSubIfd[0], new byte[0], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static TiffDirectory CreateDirectory(int imageIndex, uint width, uint length, bool reduced)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[2];
        if (reduced)
        {
            entries = new TiffIfdEntry[3];
        }

        entries[0] = CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        entries[1] = CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        if (reduced)
        {
            entries[2] = CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Long, 1, 1);
        }

        return TiffDirectory.Create(imageIndex, (ulong)(8 + (imageIndex * 32)), 0, entries);
    }

    static void WriteClassicDirectory(byte[] bytes, int offset, uint width, uint length, bool reduced)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 3, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        WriteClassicEntry(bytes, offset + 14, TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        WriteClassicEntry(bytes, offset + 26, TiffTagId.NewSubfileType, TiffDataType.Long, 1, reduced ? 1U : 0U);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 38, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicEntry(byte[] bytes, int offset, int tagId, TiffDataType dataType, uint count, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)dataType, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), count, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), valueOffset, TiffByteOrder.LittleEndian);
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
}

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffBigTiffLargeOffsetTests
{
    const ulong LargeOffset = 4294971392UL;

    [Test]
    public void OpenRead_PreservesLargeFirstIfdOffsetAndReportsInvalidRange()
    {
        byte[] fileBytes = new byte[16];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, LargeOffset);

        TiffFile file = TiffFile.OpenRead(fileBytes);

        Assert.That(file.Success, Is.False);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(file.GetHeader().FirstIfdOffset, Is.EqualTo(LargeOffset));
        Assert.That(file.GetImageCount(), Is.EqualTo(0));
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(file.GetDiagnostics().Get(0).Offset, Is.EqualTo((long)LargeOffset));
    }

    [Test]
    public void ReadBigTiff_PreservesLargeNextIfdOffsetAndReportsInvalidRange()
    {
        byte[] fileBytes = CreateBigTiffFile(new TiffIfdEntry[] { CreateBigEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 32) }, LargeOffset);

        TiffDirectoryReadResult result = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(result.Success, Is.False);
        Assert.That(result.DirectoryCount(), Is.EqualTo(1));
        Assert.That(result.Directories[0].NextIfdOffset, Is.EqualTo(LargeOffset));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(result.Diagnostics.Get(0).Offset, Is.EqualTo((long)LargeOffset));
    }

    [Test]
    public void LoadBytes_PreservesLargeExternalTagOffsetAndRejectsTinyBuffer()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateBigEntry(TiffTagId.WhitePoint, TiffDataType.Double, 2, LargeOffset)
        };
        byte[] fileBytes = CreateBigTiffFile(entries, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);
        TiffIfdEntry entry = directoryResult.Directories[0].Entries[0];

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes);

        Assert.That(directoryResult.Success, Is.True);
        Assert.That(entry.ValueOffset, Is.EqualTo(LargeOffset));
        Assert.That(entry.HasExternalValue(), Is.True);
        Assert.That(loadResult.Success, Is.False);
        Assert.That(loadResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(loadResult.Bytes.Length, Is.EqualTo(0));
    }

    [Test]
    public void ReadSubIfds_PreservesLargeSubIfdOffsetAndRejectsTinyBuffer()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateBigEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 256),
            CreateBigEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 256),
            CreateBigEntry(TiffTagId.SubIFDs, TiffDataType.Long8, 1, LargeOffset)
        };
        byte[] fileBytes = CreateBigTiffFile(entries, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);

        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.BigTiff);

        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.Directories[0].FindEntry(TiffTagId.SubIFDs)!.ValueOffset, Is.EqualTo(LargeOffset));
        Assert.That(subIfdResult.Success, Is.False);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(1));
        Assert.That(subIfdResult.SubIfds[0].Offset, Is.EqualTo(LargeOffset));
        Assert.That(subIfdResult.SubIfds[0].Directories.Length, Is.EqualTo(0));
        Assert.That(subIfdResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(subIfdResult.Diagnostics.Get(0).Offset, Is.EqualTo((long)LargeOffset));
    }

    [Test]
    public void ReadRawStrip_PreservesLargeStripOffsetAndRejectsTinyBuffer()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateBigEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 2),
            CreateBigEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
            CreateBigEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
            CreateBigEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
            CreateBigEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
            CreateBigEntry(TiffTagId.StripOffsets, TiffDataType.Long8, 1, LargeOffset),
            CreateBigEntry(TiffTagId.StripByteCounts, TiffDataType.Long8, 1, 4)
        };
        byte[] fileBytes = CreateBigTiffFile(entries, 0);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);

        TiffRawStripReadResult stripResult = image.ReadRawStrip(0);
        TiffValidationResult validationResult = TiffValidator.Validate(fileBytes);

        Assert.That(file.Success, Is.True);
        Assert.That(image.GetTag(TiffTagId.StripOffsets)!.ValueOffset, Is.EqualTo(LargeOffset));
        Assert.That(stripResult.Success, Is.False);
        Assert.That(stripResult.Range.Offset, Is.EqualTo(LargeOffset));
        Assert.That(stripResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(validationResult.Success, Is.False);
        Assert.That(validationResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static byte[] CreateBigTiffFile(TiffIfdEntry[] entries, ulong nextIfdOffset)
    {
        int directoryByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(entries.Length);
        byte[] fileBytes = new byte[16 + directoryByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);
        TiffDirectoryWriter.WriteBigTiff(fileBytes.AsSpan(16, directoryByteCount), entries, TiffByteOrder.LittleEndian, nextIfdOffset);
        return fileBytes;
    }

    static TiffIfdEntry CreateBigEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[8];
        TiffBinaryWriter.WriteUInt64(entry.ValueOffsetBytes, valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 8;
        return entry;
    }
}

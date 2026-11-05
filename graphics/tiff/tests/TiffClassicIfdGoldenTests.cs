using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffClassicIfdGoldenTests
{
    [Test]
    public void ClassicLittleEndianBaselineIfdGolden_ParsesDirectoryEntries()
    {
        byte[] golden = new byte[]
        {
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x08, 0x00,
            0x00, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x01, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x02, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x03, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x06, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x11, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x6E, 0x00, 0x00, 0x00,
            0x16, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x17, 0x01, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult headerResult = TiffHeader.ReadClassic(golden);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.Read(headerResult.Header, golden);
        TiffRequiredTagValidationResult requiredResult = TiffRequiredTagValidator.ValidateEntries(directoryResult.Directories[0].Entries);

        Assert.That(headerResult.Success, Is.True);
        Assert.That(headerResult.Header.FirstIfdOffset, Is.EqualTo(8));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].Offset, Is.EqualTo(8));
        Assert.That(directoryResult.Directories[0].NextIfdOffset, Is.EqualTo(0));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(8));
        AssertEntry(directoryResult.Directories[0].Entries[0], TiffTagId.ImageWidth, TiffDataType.Long, 1, 4, 4, false);
        AssertEntry(directoryResult.Directories[0].Entries[1], TiffTagId.ImageLength, TiffDataType.Long, 1, 3, 4, false);
        AssertEntry(directoryResult.Directories[0].Entries[2], TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8, 2, false);
        AssertEntry(directoryResult.Directories[0].Entries[5], TiffTagId.StripOffsets, TiffDataType.Long, 1, 110, 4, false);
        AssertEntry(directoryResult.Directories[0].Entries[7], TiffTagId.StripByteCounts, TiffDataType.Long, 1, 12, 4, false);
        Assert.That(directoryResult.Directories[0].Entries[2].ValueOffsetBytes, Is.EqualTo(new byte[] { 0x08, 0x00, 0x00, 0x00 }));
        Assert.That(requiredResult.Success, Is.True);
    }

    [Test]
    public void ClassicBigEndianIfdGolden_ParsesExternalShortArray()
    {
        byte[] golden = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x02,
            0x01, 0x02, 0x00, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x26,
            0x01, 0x11, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x08, 0x00, 0x08, 0x00, 0x08
        };

        TiffHeaderReadResult headerResult = TiffHeader.ReadClassic(golden);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.Read(headerResult.Header, golden);
        TiffTagValueLoadResult bitsPerSampleBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[0], golden);

        Assert.That(headerResult.Success, Is.True);
        Assert.That(headerResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(2));
        AssertEntry(directoryResult.Directories[0].Entries[0], TiffTagId.BitsPerSample, TiffDataType.Short, 3, 38, 6, true);
        AssertEntry(directoryResult.Directories[0].Entries[1], TiffTagId.StripOffsets, TiffDataType.Long, 1, 256, 4, false);
        Assert.That(bitsPerSampleBytes.Success, Is.True);
        Assert.That(bitsPerSampleBytes.WasInline, Is.False);
        Assert.That(bitsPerSampleBytes.Bytes, Is.EqualTo(new byte[] { 0x00, 0x08, 0x00, 0x08, 0x00, 0x08 }));
    }

    static void AssertEntry(TiffIfdEntry entry, int tagId, TiffDataType dataType, long count, ulong valueOffset, long valueByteCount, bool hasExternalValue)
    {
        Assert.That(entry.TagId, Is.EqualTo(tagId));
        Assert.That(entry.DataType, Is.EqualTo(dataType));
        Assert.That(entry.Count, Is.EqualTo(count));
        Assert.That(entry.ValueOffset, Is.EqualTo(valueOffset));
        Assert.That(entry.ValueByteCount, Is.EqualTo(valueByteCount));
        Assert.That(entry.HasExternalValue(), Is.EqualTo(hasExternalValue));
    }
}

using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffBigTiffIfdGoldenTests
{
    [Test]
    public void BigTiffLittleEndianIfdGolden_ParsesInlineValues()
    {
        byte[] golden = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x04, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x01, 0x03, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x11, 0x01, 0x10, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult headerResult = TiffHeader.ReadBigTiff(golden);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.Read(headerResult.Header, golden);
        TiffTagValueLoadResult bitsPerSampleBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[1], golden);

        Assert.That(headerResult.Success, Is.True);
        Assert.That(headerResult.Header.FirstIfdOffset, Is.EqualTo(16));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].Offset, Is.EqualTo(16));
        Assert.That(directoryResult.Directories[0].NextIfdOffset, Is.EqualTo(0));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(3));
        AssertEntry(directoryResult.Directories[0].Entries[0], TiffTagId.ImageWidth, TiffDataType.Long, 1, 1024, 4, false);
        AssertEntry(directoryResult.Directories[0].Entries[1], TiffTagId.BitsPerSample, TiffDataType.Short, 3, 0x0000000800080008UL, 6, false);
        AssertEntry(directoryResult.Directories[0].Entries[2], TiffTagId.StripOffsets, TiffDataType.Long8, 1, 0x1122334455667788UL, 8, false);
        Assert.That(bitsPerSampleBytes.Success, Is.True);
        Assert.That(bitsPerSampleBytes.WasInline, Is.True);
        Assert.That(bitsPerSampleBytes.Bytes, Is.EqualTo(new byte[] { 0x08, 0x00, 0x08, 0x00, 0x08, 0x00 }));
    }

    [Test]
    public void BigTiffBigEndianIfdGolden_ParsesExternalDoubleArray()
    {
        byte[] golden = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x01, 0x3E, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x34,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult headerResult = TiffHeader.ReadBigTiff(golden);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.Read(headerResult.Header, golden);
        TiffTagValueLoadResult whitePointBytes = TiffTagValueLoader.LoadBytes(directoryResult.Directories[0].Entries[0], golden);

        Assert.That(headerResult.Success, Is.True);
        Assert.That(headerResult.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(1));
        AssertEntry(directoryResult.Directories[0].Entries[0], TiffTagId.WhitePoint, TiffDataType.Double, 2, 52, 16, true);
        Assert.That(whitePointBytes.Success, Is.True);
        Assert.That(whitePointBytes.WasInline, Is.False);
        Assert.That(whitePointBytes.Bytes, Is.EqualTo(new byte[]
        {
            0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        }));
    }

    static void AssertEntry(TiffIfdEntry entry, int tagId, TiffDataType dataType, long count, ulong valueOffset, long valueByteCount, bool hasExternalValue)
    {
        Assert.That(entry.TagId, Is.EqualTo(tagId));
        Assert.That(entry.DataType, Is.EqualTo(dataType));
        Assert.That(entry.Count, Is.EqualTo(count));
        Assert.That(entry.RawCount, Is.EqualTo((ulong)count));
        Assert.That(entry.ValueOffset, Is.EqualTo(valueOffset));
        Assert.That(entry.ValueByteCount, Is.EqualTo(valueByteCount));
        Assert.That(entry.ValueSlotByteCount, Is.EqualTo(8));
        Assert.That(entry.HasExternalValue(), Is.EqualTo(hasExternalValue));
    }
}

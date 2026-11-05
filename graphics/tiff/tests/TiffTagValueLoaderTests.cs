using System;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTagValueLoaderTests
{
    [Test]
    public void LoadBytes_ReturnsClassicInlineValueSlotBytes()
    {
        TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadClassic(new byte[]
        {
            0x02, 0x01,
            0x03, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x10, 0x00
        }, TiffByteOrder.LittleEndian);

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entryResult.Entry, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.WasInline, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x08, 0x00, 0x10, 0x00 }));
    }

    [Test]
    public void LoadBytes_ReturnsBigTiffInlineValueSlotBytes()
    {
        TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadBigTiff(new byte[]
        {
            0x00, 0x01,
            0x04, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x02, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00
        }, TiffByteOrder.LittleEndian);

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entryResult.Entry, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.WasInline, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x80, 0x02, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00 }));
    }

    [Test]
    public void LoadBytes_ReturnsExternalValueFromMemory()
    {
        TiffIfdEntry entry = CreateExternalEntry(6, 4, 2);
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 4, 5, 10, 11, 12, 13, 99 };

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.WasInline, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
    }

    [Test]
    public void LoadBytes_ReportsExternalMemoryRangeOutsideBuffer()
    {
        TiffIfdEntry entry = CreateExternalEntry(8, 4, 2);

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsAllocationLimitExceeded()
    {
        TiffIfdEntry entry = CreateExternalEntry(0, 5, 4);
        TiffLimits limits = new TiffLimits();
        limits.MaximumAllocationBytes = 4;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[] { 1, 2, 3, 4, 5 }, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.AllocationLimitExceeded), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsNegativeValueSlotByteCount()
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.ValueByteCount = 0;
        entry.ValueSlotByteCount = -1;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsNullInlineValueSlotBytes()
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.ValueByteCount = 1;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = null!;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.WasInline, Is.True);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsInlineValueByteCountLargerThanSlot()
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = 8;
        entry.ValueOffsetBytes = new byte[] { 1, 2 };

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.WasInline, Is.True);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void LoadBytes_ReturnsExternalValueFromStreamAndRestoresPosition()
    {
        TiffIfdEntry entry = CreateExternalEntry(6, 4, 2);
        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 10, 11, 12, 13, 99 });
        stream.Position = 2;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, stream);

        Assert.That(result.Success, Is.True);
        Assert.That(result.WasInline, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void LoadBytes_ReportsExternalStreamRangeOutsideStream()
    {
        TiffIfdEntry entry = CreateExternalEntry(8, 4, 2);
        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsNullStreamForExternalValue()
    {
        TiffIfdEntry entry = CreateExternalEntry(0, 4, 2);
        Stream stream = null!;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsNonSeekableStreamForExternalValue()
    {
        TiffIfdEntry entry = CreateExternalEntry(0, 4, 2);
        Stream stream = new NonSeekableMemoryStream(new byte[] { 1, 2, 3, 4 });

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.NonSeekableStream), Is.True);
    }

    [Test]
    public void LoadBytes_ReportsNullEntry()
    {
        TiffIfdEntry entry = null!;

        TiffTagValueLoadResult result = TiffTagValueLoader.LoadBytes(entry, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static TiffIfdEntry CreateExternalEntry(ulong offset, long byteCount, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.StripOffsets;
        entry.DataType = TiffDataType.Byte;
        entry.Count = byteCount;
        entry.RawCount = (ulong)byteCount;
        entry.ValueOffset = offset;
        entry.ValueOffsetBytes = new byte[slotByteCount];
        entry.ValueByteCount = byteCount;
        entry.ValueSlotByteCount = slotByteCount;
        return entry;
    }

    public class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] bytes)
            : base(bytes)
        {
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
    }
}

using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffIfdEntryTests
{
    [Test]
    public void ReadClassic_ReadsLittleEndianInlineLongEntry()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x04, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x80, 0x02, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(result.Entry.Count, Is.EqualTo(1));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(640));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(4));
        Assert.That(result.Entry.ValueSlotByteCount, Is.EqualTo(4));
        Assert.That(result.Entry.FitsClassicInline(), Is.True);
        Assert.That(result.Entry.HasExternalValue(), Is.False);
        Assert.That(result.Entry.ValueOffsetBytes, Is.EqualTo(new byte[] { 0x80, 0x02, 0x00, 0x00 }));
    }

    [Test]
    public void ReadClassic_ReadsLittleEndianExternalLongArrayEntry()
    {
        byte[] bytes = new byte[]
        {
            0x11, 0x01,
            0x04, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0xC8, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.StripOffsets));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(result.Entry.Count, Is.EqualTo(2));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(200));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(8));
        Assert.That(result.Entry.FitsClassicInline(), Is.False);
        Assert.That(result.Entry.HasExternalValue(), Is.True);
    }

    [Test]
    public void ReadClassic_ReadsBigEndianExternalShortArrayEntry()
    {
        byte[] bytes = new byte[]
        {
            0x01, 0x02,
            0x00, 0x03,
            0x00, 0x00, 0x00, 0x03,
            0x00, 0x00, 0x01, 0x2C
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.BigEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.BitsPerSample));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(result.Entry.Count, Is.EqualTo(3));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(300));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(6));
        Assert.That(result.Entry.FitsClassicInline(), Is.False);
        Assert.That(result.Entry.HasExternalValue(), Is.True);
    }

    [Test]
    public void ReadClassic_AllowsZeroCountEntry()
    {
        byte[] bytes = new byte[]
        {
            0x02, 0x01,
            0x03, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.Count, Is.EqualTo(0));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(0));
        Assert.That(result.Entry.FitsClassicInline(), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsShortEntry()
    {
        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(new byte[] { 0x00, 0x01 }, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsInvalidByteOrder()
    {
        byte[] bytes = new byte[12];

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.Unknown);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void ReadClassic_ReportsUnknownDataType()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x63, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That((int)result.Entry.DataType, Is.EqualTo(99));
        Assert.That(result.Entry.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadClassic_StreamReadsOneEntryAndAdvancesPosition()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x04, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x80, 0x02, 0x00, 0x00,
            0xFF
        };
        MemoryStream stream = new MemoryStream(bytes);

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(stream.Position, Is.EqualTo(12));
    }

    [Test]
    public void ReadClassic_StreamReportsTruncationWithoutThrowing()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x00, 0x01 });

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadClassic_NullStreamReportsUnsupportedStream()
    {
        Stream stream = null!;

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadClassic(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReadsLittleEndianInlineLongArrayEntry()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x04, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x02, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(result.Entry.Count, Is.EqualTo(2));
        Assert.That(result.Entry.RawCount, Is.EqualTo(2));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(0x0000032000000280UL));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(8));
        Assert.That(result.Entry.ValueSlotByteCount, Is.EqualTo(8));
        Assert.That(result.Entry.FitsClassicInline(), Is.False);
        Assert.That(result.Entry.FitsBigTiffInline(), Is.True);
        Assert.That(result.Entry.HasExternalValue(), Is.False);
        Assert.That(result.Entry.ValueOffsetBytes, Is.EqualTo(new byte[] { 0x80, 0x02, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00 }));
    }

    [Test]
    public void ReadBigTiff_ReadsBigEndianExternalDoubleArrayEntry()
    {
        byte[] bytes = new byte[]
        {
            0x01, 0x3E,
            0x00, 0x0C,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.BigEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.WhitePoint));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Double));
        Assert.That(result.Entry.Count, Is.EqualTo(2));
        Assert.That(result.Entry.RawCount, Is.EqualTo(2));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(256));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(16));
        Assert.That(result.Entry.FitsBigTiffInline(), Is.False);
        Assert.That(result.Entry.HasExternalValue(), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReadsLong8InlineEntry()
    {
        byte[] bytes = new byte[]
        {
            0x11, 0x01,
            0x10, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.StripOffsets));
        Assert.That(result.Entry.DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(result.Entry.Count, Is.EqualTo(1));
        Assert.That(result.Entry.ValueOffset, Is.EqualTo(0x1122334455667788UL));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(8));
        Assert.That(result.Entry.FitsBigTiffInline(), Is.True);
        Assert.That(result.Entry.HasExternalValue(), Is.False);
    }

    [Test]
    public void ReadBigTiff_ReportsCountLargerThanSignedRange()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Entry.RawCount, Is.EqualTo(0x8000000000000000UL));
        Assert.That(result.Entry.Count, Is.EqualTo(0));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(0));
    }

    [Test]
    public void ReadBigTiff_ReportsValueByteCountOverflow()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x0C, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
        Assert.That(result.Entry.RawCount, Is.EqualTo(0x7FFFFFFFFFFFFFFFUL));
        Assert.That(result.Entry.ValueByteCount, Is.EqualTo(0));
    }

    [Test]
    public void ReadBigTiff_ReportsShortEntry()
    {
        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(new byte[] { 0x00, 0x01 }, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReportsInvalidByteOrder()
    {
        byte[] bytes = new byte[20];

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.Unknown);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReportsUnknownDataType()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x63, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(bytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That((int)result.Entry.DataType, Is.EqualTo(99));
        Assert.That(result.Entry.RawCount, Is.EqualTo(1));
    }

    [Test]
    public void ReadBigTiff_StreamReadsOneEntryAndAdvancesPosition()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x01,
            0x04, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x02, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00,
            0xFF
        };
        MemoryStream stream = new MemoryStream(bytes);

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entry.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(stream.Position, Is.EqualTo(20));
    }

    [Test]
    public void ReadBigTiff_StreamReportsTruncationWithoutThrowing()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x00, 0x01 });

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadBigTiff_NullStreamReportsUnsupportedStream()
    {
        Stream stream = null!;

        TiffIfdEntryReadResult result = TiffIfdEntry.ReadBigTiff(stream, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }
}

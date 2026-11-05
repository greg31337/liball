using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class SfntWriterTests
{
    [Test]
    public void WritesCanonicalDirectoryLayoutChecksumsAndAdjustment()
    {
        byte[] name = new byte[] { 9, 8, 7 };
        byte[] head = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(
            head.AsSpan(8, 4),
            0x12345678U);
        byte[] cmap = new byte[] { 1, 2, 3, 4, 5 };
        byte[] output = SfntWriter.Write(
            0x00010000U,
            new SfntWriteTable[]
            {
                new SfntWriteTable(FontTags.Name, name),
                new SfntWriteTable(FontTags.Head, head),
                new SfntWriteTable(FontTags.Cmap, cmap)
            },
            FontLimits.CreateCompatible());
        SfntDirectory directory = SfntDirectoryParser.Parse(
            FontData.Create(output).GetSlice(0, output.Length));
        SfntTableRecord cmapRecord = directory.GetTableRecord(0);
        SfntTableRecord headRecord = directory.GetTableRecord(1);
        SfntTableRecord nameRecord = directory.GetTableRecord(2);

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(directory.GetTableCount(), Is.EqualTo(3));
            Assert.That(directory.GetStoredSearchRange(), Is.EqualTo(32));
            Assert.That(directory.GetStoredEntrySelector(), Is.EqualTo(1));
            Assert.That(directory.GetStoredRangeShift(), Is.EqualTo(16));
            Assert.That(cmapRecord.GetTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(headRecord.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(nameRecord.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(cmapRecord.GetOffset(), Is.EqualTo(60));
            Assert.That(headRecord.GetOffset(), Is.EqualTo(68));
            Assert.That(nameRecord.GetOffset(), Is.EqualTo(80));
            Assert.That(cmapRecord.GetLength(), Is.EqualTo(5));
            Assert.That(headRecord.GetLength(), Is.EqualTo(12));
            Assert.That(nameRecord.GetLength(), Is.EqualTo(3));
            Assert.That(CopyTable(output, cmapRecord), Is.EqualTo(cmap));
            Assert.That(CopyTable(output, nameRecord), Is.EqualTo(name));
            Assert.That(cmapRecord.GetChecksum(),
                Is.EqualTo(FontChecksum.CalculateTable(cmap)));
            Assert.That(headRecord.GetChecksum(),
                Is.EqualTo(FontChecksum.CalculateHeadTable(head)));
            Assert.That(nameRecord.GetChecksum(),
                Is.EqualTo(FontChecksum.CalculateTable(name)));
            Assert.That(output.Length, Is.EqualTo(84));
            Assert.That(output[65], Is.Zero);
            Assert.That(output[66], Is.Zero);
            Assert.That(output[67], Is.Zero);
            Assert.That(output[83], Is.Zero);
            Assert.That(FontChecksum.VerifyFont(output), Is.True);
            Assert.That(ReadUInt32(output, 76), Is.Not.Zero);
            Assert.That(ReadUInt32(head, 8), Is.EqualTo(0x12345678U));
        });
    }

    [Test]
    public void SortsUnsignedTagsAndIgnoresInputOrderAndSourceMutation()
    {
        byte[] highTagData = new byte[] { 9 };
        SfntWriteTable highTag = new SfntWriteTable(
            -1,
            highTagData);
        SfntWriteTable cmap = new SfntWriteTable(
            FontTags.Cmap,
            new byte[] { 2 });
        highTagData[0] = 1;

        byte[] first = SfntWriter.Write(
            0x4F54544FU,
            new SfntWriteTable[] { highTag, cmap },
            FontLimits.CreateCompatible());
        byte[] second = SfntWriter.Write(
            0x4F54544FU,
            new SfntWriteTable[] { cmap, highTag },
            FontLimits.CreateCompatible());
        SfntDirectory directory = SfntDirectoryParser.Parse(
            FontData.Create(first).GetSlice(0, first.Length));
        SfntTableRecord highTagRecord = directory.GetTableRecord(1);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(directory.GetTableRecord(0).GetTag(),
                Is.EqualTo(FontTags.Cmap));
            Assert.That((uint)highTagRecord.GetTag(), Is.EqualTo(uint.MaxValue));
            Assert.That(CopyTable(first, highTagRecord),
                Is.EqualTo(new byte[] { 9 }));
        });
    }

    [Test]
    public void RejectsDuplicateTableTags()
    {
        SfntWriteTable[] tables = new SfntWriteTable[]
        {
            new SfntWriteTable(FontTags.Name, new byte[] { 1 }),
            new SfntWriteTable(FontTags.Cmap, new byte[] { 2 }),
            new SfntWriteTable(FontTags.Name, new byte[] { 3 })
        };

        Assert.Throws<FontFormatException>(() => SfntWriter.Write(
            0x00010000U,
            tables,
            FontLimits.CreateCompatible()));
    }

    [Test]
    public void RejectsHeadWithoutCompleteChecksumAdjustmentField()
    {
        SfntWriteTable[] tables = new SfntWriteTable[]
        {
            new SfntWriteTable(FontTags.Head, new byte[11])
        };

        Assert.Throws<FontFormatException>(() => SfntWriter.Write(
            0x00010000U,
            tables,
            FontLimits.CreateCompatible()));
    }

    static byte[] CopyTable(byte[] font, SfntTableRecord record)
    {
        byte[] output = new byte[(int)record.GetLength()];
        Array.Copy(
            font,
            (int)record.GetOffset(),
            output,
            0,
            output.Length);
        return output;
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }
}

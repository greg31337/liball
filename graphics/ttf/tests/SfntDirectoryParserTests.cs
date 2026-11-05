using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class SfntDirectoryParserTests
{
    [TestCase(0x00010000U)]
    [TestCase(0x4F54544FU)]
    [TestCase(0x74727565U)]
    [TestCase(0x74797031U)]
    public void ParsesEverySupportedScalerType(uint scalerType)
    {
        byte[] bytes = BuildDirectory(scalerType, 0);

        SfntDirectory directory = Parse(bytes);

        Assert.That(directory.GetScalerType(), Is.EqualTo(scalerType));
    }

    [Test]
    public void ParsesTableRecordsInDirectoryOrder()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16(2)
            .WriteUInt16(32)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteTag("head")
            .WriteUInt32(0x12345678U)
            .WriteUInt32(44)
            .WriteUInt32(54)
            .WriteTag("name")
            .WriteUInt32(0xFEDCBA98U)
            .WriteUInt32(100)
            .WriteUInt32(21);

        SfntDirectory directory = Parse(builder.CopyBytes());
        SfntTableRecord first = directory.GetTableRecord(0);
        SfntTableRecord second = directory.GetTableRecord(1);

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetTableCount(), Is.EqualTo(2));
            Assert.That(first.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(first.GetChecksum(), Is.EqualTo(0x12345678U));
            Assert.That(first.GetOffset(), Is.EqualTo(44));
            Assert.That(first.GetLength(), Is.EqualTo(54));
            Assert.That(second.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(second.GetChecksum(), Is.EqualTo(0xFEDCBA98U));
            Assert.That(second.GetOffset(), Is.EqualTo(100));
            Assert.That(second.GetLength(), Is.EqualTo(21));
        });
    }

    [Test]
    public void DerivesSearchFieldsInsteadOfTrustingStoredValues()
    {
        byte[] bytes = BuildDirectory(0x00010000U, 3);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(6, 2), 0xFFFF);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(8, 2), 0x1234);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(10, 2), 0xABCD);

        SfntDirectory directory = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetStoredSearchRange(), Is.EqualTo(0xFFFF));
            Assert.That(directory.GetStoredEntrySelector(), Is.EqualTo(0x1234));
            Assert.That(directory.GetStoredRangeShift(), Is.EqualTo(0xABCD));
            Assert.That(directory.GetSearchRange(), Is.EqualTo(32));
            Assert.That(directory.GetEntrySelector(), Is.EqualTo(1));
            Assert.That(directory.GetRangeShift(), Is.EqualTo(16));
            Assert.That(directory.GetTableCount(), Is.EqualTo(3));
        });
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(1, 16, 0, 0)]
    [TestCase(2, 32, 1, 0)]
    [TestCase(3, 32, 1, 16)]
    [TestCase(4, 64, 2, 0)]
    [TestCase(5, 64, 2, 16)]
    [TestCase(7, 64, 2, 48)]
    [TestCase(8, 128, 3, 0)]
    [TestCase(9, 128, 3, 16)]
    public void DerivesSearchFieldsAtPowerOfTwoBoundaries(
        int tableCount,
        int expectedSearchRange,
        int expectedEntrySelector,
        int expectedRangeShift)
    {
        SfntDirectory directory = Parse(BuildDirectory(0x00010000U, tableCount));

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetSearchRange(), Is.EqualTo(expectedSearchRange));
            Assert.That(directory.GetEntrySelector(), Is.EqualTo(expectedEntrySelector));
            Assert.That(directory.GetRangeShift(), Is.EqualTo(expectedRangeShift));
        });
    }

    [Test]
    public void PreservesUnsignedRecordOffsetsAndLengths()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x4F54544FU)
            .WriteUInt16(1)
            .WriteUInt16(16)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteTag("CFF ")
            .WriteUInt32(uint.MaxValue)
            .WriteUInt32(uint.MaxValue)
            .WriteUInt32(uint.MaxValue);

        SfntTableRecord record = Parse(builder.CopyBytes()).GetTableRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(record.GetChecksum(), Is.EqualTo(uint.MaxValue));
            Assert.That(record.GetOffset(), Is.EqualTo(4294967295L));
            Assert.That(record.GetLength(), Is.EqualTo(4294967295L));
        });
    }

    [Test]
    public void TracksAbsoluteDirectoryOffsetAndIgnoresFollowingTableBytes()
    {
        byte[] directoryBytes = BuildDirectory(0x00010000U, 1);
        byte[] source = new byte[5 + directoryBytes.Length + 4];
        directoryBytes.CopyTo(source, 5);
        source[source.Length - 1] = 0xA5;
        FontData data = FontData.Create(source);
        FontDataSlice slice = data.GetSlice(5, source.Length - 5);

        SfntDirectory directory = SfntDirectoryParser.Parse(slice);

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetDirectoryOffset(), Is.EqualTo(5));
            Assert.That(directory.GetTableCount(), Is.EqualTo(1));
            Assert.That(source[source.Length - 1], Is.EqualTo(0xA5));
        });
    }

    [TestCase(0x00000000U)]
    [TestCase(0x74746366U)]
    [TestCase(0x774F4646U)]
    [TestCase(0x774F4632U)]
    public void RejectsUnsupportedScalerTypes(uint scalerType)
    {
        Assert.Throws<FontFormatException>(() => Parse(BuildDirectory(scalerType, 0)));
    }

    [Test]
    public void RejectsNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => SfntDirectoryParser.Parse(null!));
    }

    [Test]
    public void RejectsEveryTruncationOfHeaderAndRecords()
    {
        byte[] complete = BuildDirectory(0x00010000U, 2);

        for (int length = 0; length < complete.Length; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(() => Parse(truncated), "Length {0}", length);
        }
    }

    [Test]
    public void EnforcesTableLimitBeforeReadingOrAllocatingRecords()
    {
        byte[] bytes = new byte[6];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(4, 2), 257);
        FontData data = FontData.Create(bytes);

        Assert.Throws<FontLimitException>(() => SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()),
            FontLimits.CreateStrict()));
    }

    [Test]
    public void BoundedParserRejectsNullLimits()
    {
        FontData data = FontData.Create(BuildDirectory(0x00010000U, 0));

        Assert.Throws<ArgumentNullException>(() => SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()),
            null!));
    }

    [Test]
    public void RecordAccessRejectsIndicesOutsideDirectory()
    {
        SfntDirectory directory = Parse(BuildDirectory(0x00010000U, 1));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => directory.GetTableRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => directory.GetTableRecord(1));
        });
    }

    private static SfntDirectory Parse(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return SfntDirectoryParser.Parse(data.GetSlice(0, data.GetLength()));
    }

    private static byte[] BuildDirectory(uint scalerType, int tableCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(scalerType)
            .WriteUInt16((ushort)tableCount)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0);

        for (int index = 0; index < tableCount; index += 1)
        {
            builder
                .WriteUInt32((uint)index)
                .WriteUInt32((uint)(index + 1))
                .WriteUInt32((uint)(index + 2))
                .WriteUInt32((uint)(index + 3));
        }

        return builder.CopyBytes();
    }
}

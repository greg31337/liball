using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class Woff1ParserTests
{
    [Test]
    public void ParsesTablesAndReconstructsAValidSfntInPhysicalTableOrder()
    {
        byte[] nameData = Enumerable.Repeat((byte)0x41, 512).ToArray();
        byte[] headData = new byte[12];
        byte[] woff = new Woff1Builder()
            .AddTable("name", nameData, true)
            .AddTable("head", headData)
            .Build();

        Woff1Font font = Parse(woff, FontLoadOptions.CreateStrict());
        Woff1Table head = font.GetTable(0);
        Woff1Table name = font.GetTable(1);
        byte[] sfnt = font.CopyReconstructedSfnt();
        SfntDirectory directory = SfntDirectoryParser.Parse(
            FontData.Create(sfnt).GetSlice(0, sfnt.Length));

        Assert.Multiple(() =>
        {
            Assert.That(font.GetFlavor(), Is.EqualTo(0x00010000U));
            Assert.That(font.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(font.GetMinorVersion(), Is.Zero);
            Assert.That(font.GetTableCount(), Is.EqualTo(2));
            Assert.That(head.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(name.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(name.IsCompressed(), Is.True);
            Assert.That(name.CopyData(), Is.EqualTo(nameData));
            Assert.That(name.GetSfntOffset(), Is.LessThan(head.GetSfntOffset()));
            Assert.That(directory.GetStoredSearchRange(), Is.EqualTo(32));
            Assert.That(directory.GetStoredEntrySelector(), Is.EqualTo(1));
            Assert.That(directory.GetStoredRangeShift(), Is.Zero);
            Assert.That(FontChecksum.VerifyFont(sfnt), Is.True);
            Assert.That(font.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void DecompressesMetadataAndPreservesPrivateData()
    {
        const string xml = "<metadata version=\"1.0\"></metadata>";
        byte[] privateBytes = new byte[] { 9, 8, 7, 6, 5 };
        byte[] woff = new Woff1Builder()
            .AddTable("name", new byte[] { 1, 2, 3 })
            .AddMetadata(xml)
            .AddPrivateData(privateBytes)
            .Build();

        Woff1Font font = Parse(woff, FontLoadOptions.CreateStrict());
        byte[] metadataCopy = font.GetMetadata().CopyData();
        byte[] privateCopy = font.GetPrivateData().CopyData();
        metadataCopy[0] = 0;
        privateCopy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(font.GetMetadata().IsPresent(), Is.True);
            Assert.That(font.GetMetadata().GetCompressedOffset() % 4, Is.Zero);
            Assert.That(font.GetMetadata().GetOriginalLength(), Is.EqualTo(Encoding.UTF8.GetByteCount(xml)));
            Assert.That(Encoding.UTF8.GetString(font.GetMetadata().CopyData()), Is.EqualTo(xml));
            Assert.That(font.GetPrivateData().IsPresent(), Is.True);
            Assert.That(font.GetPrivateData().GetOffset() % 4, Is.Zero);
            Assert.That(font.GetPrivateData().CopyData(), Is.EqualTo(privateBytes));
        });
    }

    [Test]
    public void RepresentsAbsentOptionalBlocksWithoutExposingData()
    {
        Woff1Font font = Parse(
            new Woff1Builder().AddTable("name", new byte[] { 1 }).Build(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetMetadata().IsPresent(), Is.False);
            Assert.That(font.GetPrivateData().IsPresent(), Is.False);
            Assert.Throws<InvalidOperationException>(() => font.GetMetadata().CopyData());
            Assert.Throws<InvalidOperationException>(() => font.GetPrivateData().CopyData());
        });
    }

    [Test]
    public void PreservesAnOtherwiseUnknownSfntFlavor()
    {
        byte[] woff = BuildSingleByteTable();
        WriteUInt32(woff, 4, 0x12345678U);

        Woff1Font font = Parse(woff, FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetFlavor(), Is.EqualTo(0x12345678U));
            Assert.That(ReadUInt32(font.CopyReconstructedSfnt(), 0), Is.EqualTo(0x12345678U));
        });
    }

    [Test]
    public void AcceptsAnEmptyUncompressedTable()
    {
        Woff1Font font = Parse(
            new Woff1Builder().AddTable("name", Array.Empty<byte>()).Build(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetTable(0).GetOriginalLength(), Is.Zero);
            Assert.That(font.GetTable(0).GetCompressedLength(), Is.Zero);
            Assert.That(font.GetTable(0).CopyData(), Is.Empty);
        });
    }

    [TestCase(0, 0x00000000U)]
    [TestCase(8, 0x00000001U)]
    [TestCase(14, 0x00000001U)]
    [TestCase(16, 0x00000001U)]
    public void RejectsInvalidCoreHeaderFields(int fieldOffset, uint value)
    {
        byte[] woff = new Woff1Builder()
            .AddTable("name", new byte[] { 1, 2, 3, 4 })
            .Build();

        if (fieldOffset == 14)
        {
            WriteUInt16(woff, fieldOffset, (ushort)value);
        }
        else
        {
            WriteUInt32(woff, fieldOffset, value);
        }

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsZeroTablesAndTableLimitViolations()
    {
        byte[] zeroTables = new byte[44];
        WriteUInt32(zeroTables, 0, 0x774F4646U);
        WriteUInt32(zeroTables, 4, 0x00010000U);
        WriteUInt32(zeroTables, 8, 44);
        WriteUInt32(zeroTables, 16, 12);
        byte[] twoTables = new Woff1Builder()
            .AddTable("head", new byte[12])
            .AddTable("name", new byte[] { 1 })
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(zeroTables, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontLimitException>(() => Parse(twoTables, CreateOptions(CreateLimits(1, 1_000_000, 100))));
        });
    }

    [Test]
    public void RejectsUnsortedAndDuplicateDirectoryTags()
    {
        byte[] unsorted = new Woff1Builder()
            .AddTable("head", new byte[12])
            .AddTable("name", new byte[] { 1 })
            .Build();
        uint firstTag = ReadUInt32(unsorted, 44);
        uint secondTag = ReadUInt32(unsorted, 64);
        WriteUInt32(unsorted, 44, secondTag);
        WriteUInt32(unsorted, 64, firstTag);
        byte[] duplicate = new Woff1Builder()
            .AddTable("head", new byte[12])
            .AddTable("name", new byte[] { 1 })
            .Build();
        WriteUInt32(duplicate, 64, ReadUInt32(duplicate, 44));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(unsorted, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(duplicate, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsInvalidTableLengthsOffsetsGapsAndPadding()
    {
        byte[] tooLarge = BuildSingleByteTable();
        WriteUInt32(tooLarge, 52, 2);
        byte[] unaligned = BuildSingleByteTable();
        WriteUInt32(unaligned, 48, ReadUInt32(unaligned, 48) + 1);
        byte[] gap = BuildSingleByteTable();
        WriteUInt32(gap, 48, ReadUInt32(gap, 48) + 4);
        byte[] nonzeroPadding = BuildSingleByteTable();
        int tableOffset = (int)ReadUInt32(nonzeroPadding, 48);
        nonzeroPadding[tableOffset + 1] = 7;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(tooLarge, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(unaligned, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(gap, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(nonzeroPadding, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsExtraneousTrailingData()
    {
        byte[] valid = BuildSingleByteTable();
        byte[] trailing = new byte[valid.Length + 1];
        valid.CopyTo(trailing, 0);
        WriteUInt32(trailing, 8, (uint)trailing.Length);

        Assert.Throws<FontFormatException>(() => Parse(trailing, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsInconsistentOptionalBlockFieldsAndBadPrivateAlignment()
    {
        byte[] inconsistentMetadata = BuildSingleByteTable();
        WriteUInt32(inconsistentMetadata, 28, 1);
        byte[] inconsistentPrivate = BuildSingleByteTable();
        WriteUInt32(inconsistentPrivate, 40, 1);
        byte[] badPrivateOffset = new Woff1Builder()
            .AddTable("name", new byte[] { 1 })
            .AddPrivateData(new byte[] { 2, 3 })
            .Build();
        WriteUInt32(badPrivateOffset, 36, ReadUInt32(badPrivateOffset, 36) + 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(inconsistentMetadata, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(inconsistentPrivate, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(badPrivateOffset, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsCompressedTablesThatExpandToTheWrongLength()
    {
        byte[] woff = new Woff1Builder()
            .AddTable("name", Enumerable.Repeat((byte)1, 128).ToArray(), true)
            .Build();
        WriteUInt32(woff, 56, 129);
        WriteUInt32(woff, 16, ReadUInt32(woff, 16) + 4);

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsAnInvalidZlibPayload()
    {
        byte[] woff = new Woff1Builder()
            .AddTable("name", Enumerable.Repeat((byte)1, 128).ToArray(), true)
            .Build();
        int tableOffset = (int)ReadUInt32(woff, 48);
        woff[tableOffset] = 0;

        Assert.Throws<InvalidDataException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void EnforcesDecompressionRatioAndCumulativeOutputLimits()
    {
        byte[] compressed = new Woff1Builder()
            .AddTable("name", Enumerable.Repeat((byte)0, 4096).ToArray(), true)
            .Build();
        byte[] withMetadata = new Woff1Builder()
            .AddTable("name", new byte[64])
            .AddMetadata(new string('x', 64))
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(compressed, CreateOptions(CreateLimits(10, 1_000_000, 1))));
            Assert.Throws<FontLimitException>(() => Parse(withMetadata, CreateOptions(CreateLimits(10, 150, 100))));
        });
    }

    [Test]
    public void AppliesChecksumPoliciesToTableMismatches()
    {
        byte[] woff = new Woff1Builder()
            .AddTable("name", new byte[] { 1, 2, 3, 4 })
            .Build();
        int tableOffset = (int)ReadUInt32(woff, 48);
        woff[tableOffset] ^= 0xFF;

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
        Woff1Font reported = Parse(woff, FontLoadOptions.CreateCompatible());
        Woff1Font ignored = Parse(woff, CreateOptions(FontLimits.CreateCompatible(), FontChecksumPolicy.Ignore));

        Assert.Multiple(() =>
        {
            Assert.That(reported.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(reported.GetDiagnostic(0).GetCode(), Is.EqualTo(FontDiagnosticCode.TableChecksumMismatch));
            Assert.That(reported.GetDiagnostic(0).GetTableTag(), Is.EqualTo(FontTags.Name));
            Assert.That(reported.GetDiagnostic(0).GetAbsoluteOffset(), Is.EqualTo(60));
            Assert.That(ignored.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void AppliesChecksumPoliciesToWholeFontMismatch()
    {
        byte[] woff = new Woff1Builder()
            .AddTable("head", new byte[12])
            .Build();
        int headOffset = (int)ReadUInt32(woff, 48);
        woff[headOffset + 8] ^= 1;

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
        Woff1Font reported = Parse(woff, FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(reported.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(reported.GetDiagnostic(0).GetCode(), Is.EqualTo(FontDiagnosticCode.FontChecksumMismatch));
            Assert.That(reported.GetDiagnostic(0).GetAbsoluteOffset(), Is.EqualTo(headOffset + 8));
        });
    }

    [Test]
    public void ObservesCancellationAndValidatesArgumentsAndIndexes()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        byte[] woff = BuildSingleByteTable();
        FontLoadOptions cancelled = FontLoadOptions.CreateStrict(source.Token);
        Woff1Font font = Parse(woff, FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => Parse(woff, cancelled));
            Assert.Throws<ArgumentNullException>(() => Woff1Parser.Parse(null!, FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() => Woff1Parser.Parse(FontData.Create(woff), null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => font.GetTable(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => font.GetTable(1));
            Assert.That(font.FindTable(FontTags.Head), Is.Null);
        });
    }

    [Test]
    public void ReturnedTableAndSfntCopiesAreIndependent()
    {
        Woff1Font font = Parse(BuildSingleByteTable(), FontLoadOptions.CreateStrict());
        byte[] firstTable = font.GetTable(0).CopyData();
        byte[] secondTable = font.GetTable(0).CopyData();
        byte[] firstSfnt = font.CopyReconstructedSfnt();
        byte[] secondSfnt = font.CopyReconstructedSfnt();
        firstTable[0] = 9;
        firstSfnt[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(secondTable, Is.EqualTo(new byte[] { 1 }));
            Assert.That(secondSfnt[0], Is.Zero);
            Assert.That(firstTable, Is.Not.SameAs(secondTable));
            Assert.That(firstSfnt, Is.Not.SameAs(secondSfnt));
        });
    }

    static byte[] BuildSingleByteTable()
    {
        return new Woff1Builder().AddTable("name", new byte[] { 1 }).Build();
    }

    static Woff1Font Parse(byte[] data, FontLoadOptions options)
    {
        return Woff1Parser.Parse(FontData.Create(data), options);
    }

    static FontLoadOptions CreateOptions(FontLimits limits)
    {
        return CreateOptions(limits, FontChecksumPolicy.ReportMismatch);
    }

    static FontLoadOptions CreateOptions(FontLimits limits, FontChecksumPolicy checksumPolicy)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            checksumPolicy,
            limits,
            CancellationToken.None);
    }

    static FontLimits CreateLimits(int maxTables, int maxDecompressedBytes, int maxRatio)
    {
        return FontLimits.Create(
            1_000_000,
            1_000_000,
            10,
            maxTables,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            100,
            100,
            maxDecompressedBytes,
            maxRatio,
            100,
            10,
            100);
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}

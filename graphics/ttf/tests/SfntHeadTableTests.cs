using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class SfntHeadTableTests
{
    [Test]
    public void ParsesEveryVersionOneField()
    {
        SfntHeadTable? table = Parse(BuildValidHead());

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetFontRevisionRaw(), Is.EqualTo(0x00028000));
            Assert.That(table.GetFontRevision(), Is.EqualTo(2.5));
            Assert.That(table.GetChecksumAdjustment(), Is.EqualTo(0x12345678U));
            Assert.That(table.GetMagicNumber(), Is.EqualTo(0x5F0F3CF5U));
            Assert.That(table.GetFlags(), Is.EqualTo(0x781F));
            Assert.That(table.GetUnitsPerEm(), Is.EqualTo(2048));
            Assert.That(table.GetCreatedSeconds(), Is.Zero);
            Assert.That(table.GetCreated(), Is.EqualTo(
                new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(table.GetModifiedSeconds(), Is.EqualTo(3661));
            Assert.That(table.GetModified(), Is.EqualTo(
                new DateTime(1904, 1, 1, 1, 1, 1, DateTimeKind.Utc)));
            Assert.That(table.GetXMin(), Is.EqualTo(-20));
            Assert.That(table.GetYMin(), Is.EqualTo(-30));
            Assert.That(table.GetXMax(), Is.EqualTo(1000));
            Assert.That(table.GetYMax(), Is.EqualTo(1100));
            Assert.That(table.GetMacStyle(), Is.EqualTo(0x7F));
            Assert.That(table.GetLowestRecommendedPpem(), Is.EqualTo(9));
            Assert.That(table.GetFontDirectionHint(), Is.EqualTo(2));
            Assert.That(table.GetLocaFormat(), Is.EqualTo(SfntLocaFormat.Long));
            Assert.That(table.GetGlyphDataFormat(), Is.Zero);
            Assert.That(table.GetExtensionLength(), Is.Zero);
        });
    }

    [Test]
    public void ExposesDefinedFlagAndMacStyleBits()
    {
        SfntHeadTable? table = Parse(BuildValidHead());

        Assert.Multiple(() =>
        {
            for (int bit = 0; bit <= 4; bit += 1)
            {
                Assert.That(table!.HasFlag(bit), Is.True, $"flags bit {bit}");
            }

            for (int bit = 5; bit <= 10; bit += 1)
            {
                Assert.That(table!.HasFlag(bit), Is.False, $"flags bit {bit}");
            }

            for (int bit = 11; bit <= 14; bit += 1)
            {
                Assert.That(table!.HasFlag(bit), Is.True, $"flags bit {bit}");
            }

            Assert.That(table!.HasFlag(15), Is.False);

            for (int bit = 0; bit <= 6; bit += 1)
            {
                Assert.That(table.HasMacStyle(bit), Is.True, $"macStyle bit {bit}");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => table.HasFlag(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.HasFlag(16));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.HasMacStyle(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.HasMacStyle(7));
        });
    }

    [Test]
    public void RejectsEveryTruncationPoint()
    {
        byte[] complete = BuildValidHead();

        for (int length = 0; length < complete.Length; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(truncated),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void AppliesVersionGatingAndExtensionLengthRules()
    {
        byte[] exactWithExtension = Append(BuildValidHead(), new byte[] { 4, 5 });
        byte[] compatibleExtension = exactWithExtension.ToArray();
        WriteUInt16(compatibleExtension, 2, 1);
        byte[] unknownMajor = BuildValidHead();
        WriteUInt16(unknownMajor, 0, 2);

        Assert.Throws<FontFormatException>(() => Parse(exactWithExtension));

        SfntTableCache compatibleCache = CreateCache(compatibleExtension, FontLoadMode.Compatible);
        SfntParsedTableView? compatibleView = SfntHeadTableParser.FindView(compatibleCache);
        SfntHeadTable? compatible = SfntHeadTableParser.FindTable(compatibleCache);
        SfntTableCache strictCache = CreateCache(compatibleExtension, FontLoadMode.Strict);
        SfntParsedTableView? strictView = SfntHeadTableParser.FindView(strictCache);
        SfntTableCache unknownCache = CreateCache(unknownMajor, FontLoadMode.Compatible);
        SfntParsedTableView? unknownView = SfntHeadTableParser.FindView(unknownCache);

        Assert.Multiple(() =>
        {
            Assert.That(compatibleView!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible!.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(compatible.GetExtensionLength(), Is.EqualTo(2));
            Assert.That(strictView!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(SfntHeadTableParser.FindTable(strictCache), Is.Null);
            Assert.That(unknownView!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(SfntHeadTableParser.FindTable(unknownCache), Is.Null);
        });
    }

    [Test]
    public void RejectsInvalidMagicNumber()
    {
        byte[] bytes = BuildValidHead();
        WriteUInt32(bytes, 12, 0x5F0F3CF4U);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(0x0020)]
    [TestCase(0x0040)]
    [TestCase(0x0400)]
    [TestCase(0x8000)]
    public void RejectsReservedFlags(int reservedBit)
    {
        byte[] bytes = BuildValidHead();
        WriteUInt16(bytes, 16, reservedBit);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(16)]
    [TestCase(2048)]
    [TestCase(16384)]
    public void AcceptsValidUnitsPerEmRange(int unitsPerEm)
    {
        byte[] bytes = BuildValidHead();
        WriteUInt16(bytes, 18, unitsPerEm);

        Assert.That(Parse(bytes)!.GetUnitsPerEm(), Is.EqualTo(unitsPerEm));
    }

    [TestCase(0)]
    [TestCase(15)]
    [TestCase(16385)]
    [TestCase(65535)]
    public void RejectsInvalidUnitsPerEm(int unitsPerEm)
    {
        byte[] bytes = BuildValidHead();
        WriteUInt16(bytes, 18, unitsPerEm);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void RejectsInvertedHorizontalOrVerticalBounds()
    {
        byte[] horizontal = BuildValidHead();
        WriteInt16(horizontal, 36, 1001);
        byte[] vertical = BuildValidHead();
        WriteInt16(vertical, 38, 1101);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(horizontal));
            Assert.Throws<FontFormatException>(() => Parse(vertical));
        });
    }

    [TestCase(0x0080)]
    [TestCase(0x8000)]
    public void RejectsReservedMacStyleBits(int reservedBit)
    {
        byte[] bytes = BuildValidHead();
        WriteUInt16(bytes, 44, reservedBit);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(-2)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void AcceptsPublishedFontDirectionHints(int directionHint)
    {
        byte[] bytes = BuildValidHead();
        WriteInt16(bytes, 48, directionHint);

        Assert.That(Parse(bytes)!.GetFontDirectionHint(), Is.EqualTo(directionHint));
    }

    [TestCase(-3)]
    [TestCase(3)]
    [TestCase(32767)]
    public void RejectsInvalidFontDirectionHints(int directionHint)
    {
        byte[] bytes = BuildValidHead();
        WriteInt16(bytes, 48, directionHint);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(0, SfntLocaFormat.Short)]
    [TestCase(1, SfntLocaFormat.Long)]
    public void AcceptsBothLocaFormats(int encodedFormat, SfntLocaFormat expected)
    {
        byte[] bytes = BuildValidHead();
        WriteInt16(bytes, 50, encodedFormat);

        Assert.That(Parse(bytes)!.GetLocaFormat(), Is.EqualTo(expected));
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(32767)]
    public void RejectsInvalidLocaFormats(int encodedFormat)
    {
        byte[] bytes = BuildValidHead();
        WriteInt16(bytes, 50, encodedFormat);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(32767)]
    public void RejectsNonzeroGlyphDataFormats(int glyphDataFormat)
    {
        byte[] bytes = BuildValidHead();
        WriteInt16(bytes, 52, glyphDataFormat);

        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [TestCase(-60052752000L)]
    [TestCase(255485145599L)]
    public void AcceptsRepresentableTimestampBoundaries(long seconds)
    {
        byte[] bytes = BuildValidHead();
        WriteInt64(bytes, 20, seconds);
        WriteInt64(bytes, 28, seconds);

        SfntHeadTable? table = Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetCreatedSeconds(), Is.EqualTo(seconds));
            Assert.That(table.GetModifiedSeconds(), Is.EqualTo(seconds));
            Assert.That(table.GetCreated().Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(table.GetModified().Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public void RejectsUnrepresentableCreatedOrModifiedTimestamp()
    {
        byte[] invalidCreated = BuildValidHead();
        WriteInt64(invalidCreated, 20, long.MinValue);
        byte[] invalidModified = BuildValidHead();
        WriteInt64(invalidModified, 28, long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(invalidCreated));
            Assert.Throws<FontFormatException>(() => Parse(invalidModified));
        });
    }

    [Test]
    public void MissingHeadReturnsNoViewOrParsedTable()
    {
        SfntTableStore store = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache cache = SfntTableCache.Create(store, FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(SfntHeadTableParser.FindView(cache), Is.Null);
            Assert.That(SfntHeadTableParser.FindTable(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntHeadTableParser.FindView(null!));
        });
    }

    [Test]
    public void FontFaceUsesItsSemanticCacheForHeadReads()
    {
        byte[] font = AddRequiredTables(new TinySfntBuilder())
            .AddTable("head", BuildValidHead())
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .Build();
        FontFace face = FontLoader.Load(
            font,
            FontLoadOptions.CreateCompatible()).GetFace(0);

        SfntHeadTable? first = face.GetHeadTable();
        SfntHeadTable? second = face.GetHeadTable();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first!.GetUnitsPerEm(), Is.EqualTo(2048));
            Assert.That(face.GetHeadTableView()!.GetParsedData(), Is.SameAs(first));
        });
    }

    static TinySfntBuilder AddRequiredTables(TinySfntBuilder builder)
    {
        return builder
            .AddTable("cmap", new byte[4])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4]);
    }

    static SfntHeadTable? Parse(byte[] bytes)
    {
        return SfntHeadTableParser.FindTable(CreateCache(
            bytes,
            FontLoadMode.Compatible));
    }

    static SfntTableCache CreateCache(byte[] bytes, FontLoadMode mode)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Head,
            FontChecksum.CalculateHeadTable(bytes.Length >= 12 ? bytes : new byte[12]),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableStore store = new SfntTableStore(
            new SfntTableData[] { table },
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, mode);
    }

    static byte[] BuildValidHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0x00028000)
            .WriteUInt32(0x12345678U)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0x781F)
            .WriteUInt16(2048)
            .WriteInt64(0)
            .WriteInt64(3661)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(1000)
            .WriteInt16(1100)
            .WriteUInt16(0x007F)
            .WriteUInt16(9)
            .WriteInt16(2)
            .WriteInt16(1)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), (ushort)value);
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(offset, 2), (short)value);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }

    static void WriteInt64(byte[] bytes, int offset, long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(offset, 8), value);
    }
}

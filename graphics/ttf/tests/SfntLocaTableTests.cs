using ttf;

namespace ttf.Tests;

internal class SfntLocaTableTests
{
    [Test]
    public void ParsesShortOffsetsDetectsEmptyGlyphsAndBoundsRecords()
    {
        byte[] glyf = CreateSequentialBytes(12);
        SfntLocaTable? table = Parse(
            BuildHead(SfntLocaFormat.Short),
            BuildTrueTypeMaxp(3),
            BuildShortLoca(2, 6, 6, 10),
            glyf);

        byte[] firstCopy = table!.GetGlyphRecord(0).CopyData();
        firstCopy[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(table.GetFormat(), Is.EqualTo(SfntLocaFormat.Short));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(table.GetOffsetCount(), Is.EqualTo(4));
            Assert.That(table.GetGlyfLength(), Is.EqualTo(12));
            Assert.That(table.GetEmptyGlyphCount(), Is.EqualTo(1));
            Assert.That(table.GetOffset(0), Is.EqualTo(2));
            Assert.That(table.GetOffset(3), Is.EqualTo(10));
            Assert.That(table.GetGlyphRecord(0).GetGlyphId(), Is.Zero);
            Assert.That(table.GetGlyphRecord(0).GetOffset(), Is.EqualTo(2));
            Assert.That(table.GetGlyphRecord(0).GetLength(), Is.EqualTo(4));
            Assert.That(table.GetGlyphRecord(0).IsEmpty(), Is.False);
            Assert.That(table.GetGlyphRecord(0).CopyData(),
                Is.EqualTo(new byte[] { 2, 3, 4, 5 }));
            Assert.That(table.GetGlyphRecord(0).GetDataSlice().GetAbsoluteOffset(),
                Is.EqualTo(2));
            Assert.That(table.GetGlyphRecord(1).GetOffset(), Is.EqualTo(6));
            Assert.That(table.GetGlyphRecord(1).GetLength(), Is.Zero);
            Assert.That(table.GetGlyphRecord(1).IsEmpty(), Is.True);
            Assert.That(table.GetGlyphRecord(1).CopyData(), Is.Empty);
            Assert.That(table.GetGlyphRecord(2).CopyData(),
                Is.EqualTo(new byte[] { 6, 7, 8, 9 }));
        });
    }

    [Test]
    public void ParsesLongOffsetsWithOddByteBoundaries()
    {
        SfntLocaTable? table = Parse(
            BuildHead(SfntLocaFormat.Long),
            BuildTrueTypeMaxp(3),
            BuildLongLoca(0, 3, 3, 8),
            CreateSequentialBytes(8));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntLocaFormat.Long));
            Assert.That(table.GetOffset(1), Is.EqualTo(3));
            Assert.That(table.GetGlyphRecord(0).CopyData(),
                Is.EqualTo(new byte[] { 0, 1, 2 }));
            Assert.That(table.GetGlyphRecord(1).IsEmpty(), Is.True);
            Assert.That(table.GetGlyphRecord(2).CopyData(),
                Is.EqualTo(new byte[] { 3, 4, 5, 6, 7 }));
        });
    }

    [TestCase(SfntLocaFormat.Short)]
    [TestCase(SfntLocaFormat.Long)]
    public void RejectsEveryTruncatedLocaAndTrailingEntries(SfntLocaFormat format)
    {
        byte[] complete = format == SfntLocaFormat.Short
            ? BuildShortLoca(0, 2, 4)
            : BuildLongLoca(0, 2, 4);

        for (int length = 0; length < complete.Length; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(
                    BuildHead(format),
                    BuildTrueTypeMaxp(2),
                    truncated,
                    new byte[4]),
                "Length {0}",
                length);
        }

        byte[] trailing = new byte[complete.Length + 1];
        complete.CopyTo(trailing, 0);
        Assert.Throws<FontFormatException>(() => Parse(
            BuildHead(format),
            BuildTrueTypeMaxp(2),
            trailing,
            new byte[4]));
    }

    [TestCase(SfntLocaFormat.Short)]
    [TestCase(SfntLocaFormat.Long)]
    public void RejectsNonMonotonicOffsets(SfntLocaFormat format)
    {
        byte[] loca = format == SfntLocaFormat.Short
            ? BuildShortLoca(0, 6, 4)
            : BuildLongLoca(0, 6, 4);

        Assert.Throws<FontFormatException>(() => Parse(
            BuildHead(format),
            BuildTrueTypeMaxp(2),
            loca,
            new byte[8]));
    }

    [TestCase(SfntLocaFormat.Short)]
    [TestCase(SfntLocaFormat.Long)]
    public void RejectsOffsetsOutsideBoundedGlyfData(SfntLocaFormat format)
    {
        byte[] loca = format == SfntLocaFormat.Short
            ? BuildShortLoca(0, 4, 10)
            : BuildLongLoca(0, 4, 10);

        Assert.Throws<FontFormatException>(() => Parse(
            BuildHead(format),
            BuildTrueTypeMaxp(2),
            loca,
            new byte[8]));
    }

    [Test]
    public void RejectsLongOffsetOutsideSupportedRange()
    {
        byte[] loca = new TestBinaryBuilder()
            .WriteUInt32(0)
            .WriteUInt32(uint.MaxValue)
            .CopyBytes();

        Assert.Throws<FontFormatException>(() => Parse(
            BuildHead(SfntLocaFormat.Long),
            BuildTrueTypeMaxp(1),
            loca,
            Array.Empty<byte>()));
    }

    [Test]
    public void RequiresHeadTrueTypeMaxpAndGlyfDependencies()
    {
        byte[] head = BuildHead(SfntLocaFormat.Short);
        byte[] maxp = BuildTrueTypeMaxp(1);
        byte[] loca = BuildShortLoca(0, 0);
        byte[] glyf = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntLocaTableParser.FindTable(
                CreateCache(null, maxp, loca, glyf)));
            Assert.Throws<FontFormatException>(() => SfntLocaTableParser.FindTable(
                CreateCache(head, null, loca, glyf)));
            Assert.Throws<FontFormatException>(() => SfntLocaTableParser.FindTable(
                CreateCache(head, maxp, loca, null)));
            Assert.Throws<FontFormatException>(() => SfntLocaTableParser.FindTable(
                CreateCache(head, BuildCffMaxp(1), loca, glyf)));
        });
    }

    [Test]
    public void EmptyGlyphRecordsRemainBoundedWhenGlyfHasUnreferencedBytes()
    {
        SfntLocaTable? table = Parse(
            BuildHead(SfntLocaFormat.Short),
            BuildTrueTypeMaxp(2),
            BuildShortLoca(0, 0, 0),
            new byte[] { 10, 20, 30 });

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetEmptyGlyphCount(), Is.EqualTo(2));
            Assert.That(table.GetGlyphRecord(0).GetDataSlice().GetLength(), Is.Zero);
            Assert.That(table.GetGlyphRecord(1).GetDataSlice().GetLength(), Is.Zero);
            Assert.That(table.GetGlyfLength(), Is.EqualTo(3));
        });
    }

    [Test]
    public void AccessorsRejectOffsetsAndGlyphIdsOutsideDeclaredRanges()
    {
        SfntLocaTable? table = Parse(
            BuildHead(SfntLocaFormat.Short),
            BuildTrueTypeMaxp(1),
            BuildShortLoca(0, 0),
            Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetOffset(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetOffset(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetGlyphRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetGlyphRecord(1));
        });
    }

    [Test]
    public void FontFaceUsesCachedLocaViewAndProvidesBoundedGlyphRecords()
    {
        SfntTableCache cache = CreateCache(
            BuildHead(SfntLocaFormat.Long),
            BuildTrueTypeMaxp(2),
            BuildLongLoca(0, 1, 4),
            new byte[] { 1, 2, 3, 4 });
        FontFace face = CreateFace(FontOutlineKind.TrueType, cache);

        SfntLocaTable? first = face.GetLocaTable();
        SfntLocaTable? second = face.GetLocaTable();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(face.GetLocaTableView()!.GetParsedData(), Is.SameAs(first));
            Assert.That(face.GetGlyfRecord(0), Is.SameAs(first!.GetGlyphRecord(0)));
            Assert.That(face.GetGlyfRecord(1).CopyData(),
                Is.EqualTo(new byte[] { 2, 3, 4 }));
        });
    }

    [Test]
    public void FontFaceEnforcesOutlineKindAndMissingLoca()
    {
        SfntTableCache cache = CreateCache(
            BuildHead(SfntLocaFormat.Short),
            BuildTrueTypeMaxp(1),
            BuildShortLoca(0, 0),
            Array.Empty<byte>());
        FontFace cffFace = CreateFace(FontOutlineKind.Cff1, cache);
        FontFace missingLocaFace = CreateFace(
            FontOutlineKind.TrueType,
            CreateCache(
                BuildHead(SfntLocaFormat.Short),
                BuildTrueTypeMaxp(1),
                null,
                Array.Empty<byte>()));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => cffFace.GetLocaTable());
            Assert.Throws<FontFormatException>(() => missingLocaFace.GetGlyfRecord(0));
        });
    }

    static SfntLocaTable? Parse(
        byte[] head,
        byte[] maxp,
        byte[] loca,
        byte[] glyf)
    {
        return SfntLocaTableParser.FindTable(CreateCache(head, maxp, loca, glyf));
    }

    static FontFace CreateFace(FontOutlineKind outlineKind, SfntTableCache cache)
    {
        SfntValidationResult validation = new SfntValidationResult(
            outlineKind,
            Array.Empty<FontDiagnostic>());
        uint scalerType = outlineKind == FontOutlineKind.TrueType
            ? 0x00010000U
            : 0x4F54544FU;
        return new FontFace(0, scalerType, validation, cache);
    }

    static SfntTableCache CreateCache(
        byte[]? head,
        byte[]? maxp,
        byte[]? loca,
        byte[]? glyf)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, head);
        AddTable(tables, FontTags.Maxp, maxp);
        AddTable(tables, FontTags.Loca, loca);
        AddTable(tables, FontTags.Glyf, glyf);
        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, FontLoadMode.Compatible);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[]? bytes)
    {
        if (bytes == null)
        {
            return;
        }

        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildHead(SfntLocaFormat format)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0)
            .WriteUInt16(1000)
            .WriteInt64(0)
            .WriteInt64(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(1000)
            .WriteInt16(1000)
            .WriteUInt16(0)
            .WriteUInt16(8)
            .WriteInt16(2)
            .WriteInt16((short)format)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildTrueTypeMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildCffMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildShortLoca(params int[] offsets)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();

        foreach (int offset in offsets)
        {
            builder.WriteUInt16((ushort)(offset / 2));
        }

        return builder.CopyBytes();
    }

    static byte[] BuildLongLoca(params int[] offsets)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();

        foreach (int offset in offsets)
        {
            builder.WriteUInt32((uint)offset);
        }

        return builder.CopyBytes();
    }

    static byte[] CreateSequentialBytes(int length)
    {
        byte[] bytes = new byte[length];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            bytes[index] = (byte)index;
        }

        return bytes;
    }
}

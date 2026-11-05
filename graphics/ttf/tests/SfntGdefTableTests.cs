using ttf;

namespace ttf.Tests;

internal class SfntGdefTableTests
{
    [Test]
    public void ParsesAllVersion13StructuresAndQueriesGlyphData()
    {
        SfntGdefTable? table = Parse(BuildVersion13(), true);
        SfntGdefAttachmentPoint? attachment = table!.GetAttachmentList()!
            .FindAttachmentPoint(1);
        SfntGdefLigatureGlyph? ligature = table.GetLigatureCaretList()!
            .FindLigatureGlyph(4);
        SfntGdefCaretValue adjustedCaret = ligature!.GetCaretValue(2);
        SfntVariationIndex? variationIndex = adjustedCaret.GetAdjustment()!
            .GetVariationIndex();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.EqualTo(3));
            Assert.That(table.GetGlyphClass(1), Is.EqualTo(1));
            Assert.That(table.GetGlyphClass(4), Is.EqualTo(2));
            Assert.That(table.GetGlyphClass(6), Is.EqualTo(3));
            Assert.That(table.GetGlyphClass(8), Is.EqualTo(4));
            Assert.That(table.GetGlyphClass(9), Is.Zero);
            Assert.That(table.GetMarkAttachmentClass(6), Is.EqualTo(1));
            Assert.That(table.GetMarkAttachmentClass(7), Is.EqualTo(2));
            Assert.That(attachment!.GetPointCount(), Is.EqualTo(2));
            Assert.That(attachment.GetPointIndex(0), Is.EqualTo(2));
            Assert.That(attachment.GetPointIndex(1), Is.EqualTo(5));
            Assert.That(table.GetAttachmentList()!.FindAttachmentPoint(2), Is.Null);
            Assert.That(ligature.GetCaretCount(), Is.EqualTo(3));
            Assert.That(ligature.GetCaretValue(0).GetFormat(), Is.EqualTo(1));
            Assert.That(ligature.GetCaretValue(0).GetCoordinate(), Is.EqualTo(-40));
            Assert.That(ligature.GetCaretValue(1).HasContourPointIndex(), Is.True);
            Assert.That(ligature.GetCaretValue(1).GetContourPointIndex(), Is.EqualTo(9));
            Assert.That(adjustedCaret.GetFormat(), Is.EqualTo(3));
            Assert.That(adjustedCaret.GetCoordinate(), Is.EqualTo(120));
            Assert.That(adjustedCaret.GetAdjustment()!.IsVariationIndex(), Is.True);
            Assert.That(variationIndex!.GetOuterIndex(), Is.Zero);
            Assert.That(variationIndex.GetInnerIndex(), Is.Zero);
            Assert.That(table.GetMarkGlyphSets()!.GetSetCount(), Is.EqualTo(2));
            Assert.That(table.GetMarkGlyphSets()!.Covers(0, 7), Is.True);
            Assert.That(table.GetMarkGlyphSets()!.Covers(1, 9), Is.True);
            Assert.That(table.GetMarkGlyphSets()!.Covers(1, 7), Is.False);
            Assert.That(table.GetItemVariationStore()!.GetAxisCount(), Is.EqualTo(2));
            Assert.That(table.GetItemVariationStore()!.GetRegionCount(), Is.EqualTo(1));
            Assert.That(table.GetItemVariationStore()!.GetVariationData(0)
                .GetDelta(0, 0), Is.EqualTo(100));
        });
    }

    [Test]
    public void AppliesVersionGatesAndCompatibleVersionPolicy()
    {
        SfntGdefTable? version10 = Parse(BuildEmpty(0), false);
        SfntGdefTable? version12 = Parse(BuildEmpty(2), false);
        byte[] futureBytes = BuildEmpty(4);
        SfntParsedTableView? compatible = SfntGdefTableParser.FindView(
            CreateCache(
                futureBytes,
                true,
                false,
                FontLoadMode.Compatible,
                FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntGdefTableParser.FindView(
            CreateCache(
                futureBytes,
                true,
                false,
                FontLoadMode.Strict,
                FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(version10!.GetMinorVersion(), Is.Zero);
            Assert.That(version10.GetMarkGlyphSets(), Is.Null);
            Assert.That(version10.GetItemVariationStore(), Is.Null);
            Assert.That(version12!.GetMinorVersion(), Is.EqualTo(2));
            Assert.That(version12.GetMarkGlyphSets(), Is.Null);
            Assert.That(version12.GetItemVariationStore(), Is.Null);
            Assert.That(compatible!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
        });
    }

    [Test]
    public void AllowsFormat3CaretWithoutAnAdjustmentTable()
    {
        byte[] bytes = BuildVersion13();
        PatchUInt16(bytes, 104, 0);
        SfntGdefCaretValue caret = Parse(bytes, true)!
            .GetLigatureCaretList()!
            .FindLigatureGlyph(4)!
            .GetCaretValue(2);

        Assert.Multiple(() =>
        {
            Assert.That(caret.GetCoordinate(), Is.EqualTo(120));
            Assert.That(caret.GetAdjustmentOffset(), Is.Zero);
            Assert.That(caret.GetAdjustmentTableOffset(), Is.Zero);
            Assert.That(caret.GetAdjustment(), Is.Null);
        });
    }

    [TestCaseSource(nameof(InvalidGdefTables))]
    public void RejectsMalformedGdefStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, true));
    }

    [Test]
    public void RequiresDependentTablesAndEnforcesLimits()
    {
        byte[] version13 = BuildVersion13();
        SfntTableCache missingMaxp = CreateCache(
            version13,
            false,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache missingFvar = CreateCache(
            version13,
            true,
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache limited = CreateCache(
            version13,
            true,
            true,
            FontLoadMode.Compatible,
            CreateLimits(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntGdefTableParser.FindTable(missingMaxp));
            Assert.Throws<FontFormatException>(() =>
                SfntGdefTableParser.FindTable(missingFvar));
            Assert.Throws<FontLimitException>(() =>
                SfntGdefTableParser.FindTable(limited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndHandlesAbsentOrUnknownTables()
    {
        SfntTableCache cache = CreateCache(
            BuildVersion13(),
            true,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknown = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .CopyBytes();
        SfntParsedTableView? raw = SfntGdefTableParser.FindView(CreateCache(
            unknown,
            true,
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            true,
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetGdefTable(), Is.Not.Null);
            Assert.That(face.GetGdefTableView()!.GetParsedData(),
                Is.SameAs(face.GetGdefTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntGdefTableParser.FindView(absent), Is.Null);
            Assert.That(SfntGdefTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntGdefTableParser.FindView(null!));
        });
    }

    static IEnumerable<TestCaseData> InvalidGdefTables()
    {
        byte[] topLevelOffset = BuildVersion13();
        PatchUInt16(topLevelOffset, 4, 2);
        yield return InvalidCase(topLevelOffset, "TopLevelOffset");

        byte[] invalidGlyphClass = BuildVersion13();
        PatchUInt16(invalidGlyphClass, 26, 5);
        yield return InvalidCase(invalidGlyphClass, "GlyphClass");

        byte[] attachmentCount = BuildVersion13();
        PatchUInt16(attachmentCount, 48, 1);
        yield return InvalidCase(attachmentCount, "AttachmentCoverageCount");

        byte[] unorderedPoints = BuildVersion13();
        PatchUInt16(unorderedPoints, 66, 1);
        yield return InvalidCase(unorderedPoints, "AttachmentPointOrder");

        byte[] ligatureCount = BuildVersion13();
        PatchUInt16(ligatureCount, 80, 0);
        yield return InvalidCase(ligatureCount, "LigatureCoverageCount");

        byte[] caretFormat = BuildVersion13();
        PatchUInt16(caretFormat, 92, 4);
        yield return InvalidCase(caretFormat, "CaretFormat");

        byte[] caretAdjustment = BuildVersion13();
        PatchUInt16(caretAdjustment, 104, 2);
        yield return InvalidCase(caretAdjustment, "CaretAdjustmentOffset");

        byte[] markClass = BuildVersion13();
        PatchUInt16(markClass, 120, 256);
        yield return InvalidCase(markClass, "MarkAttachmentClass");

        byte[] markSetFormat = BuildVersion13();
        PatchUInt16(markSetFormat, 122, 2);
        yield return InvalidCase(markSetFormat, "MarkSetFormat");

        byte[] markSetOffset = BuildVersion13();
        PatchUInt32(markSetOffset, 126, 0);
        yield return InvalidCase(markSetOffset, "MarkSetOffset");

        byte[] storeFormat = BuildVersion13();
        PatchUInt16(storeFormat, 152, 2);
        yield return InvalidCase(storeFormat, "VariationStoreFormat");

        byte[] variationIndex = BuildVersion13();
        PatchUInt16(variationIndex, 106, 1);
        yield return InvalidCase(variationIndex, "VariationIndex");

        byte[] missingStore = BuildVersion13();
        PatchUInt32(missingStore, 14, 0);
        yield return InvalidCase(missingStore, "MissingVariationStore");

        byte[] undefinedVersion = BuildEmpty(0);
        PatchUInt16(undefinedVersion, 2, 1);
        yield return InvalidCase(undefinedVersion, "UndefinedVersion11");

        yield return InvalidCase(BuildVersion13()[..17], "TruncatedHeader");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntGdefTable? Parse(byte[] bytes, bool includeFvar)
    {
        return SfntGdefTableParser.FindTable(CreateCache(
            bytes,
            true,
            includeFvar,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? gdef,
        bool includeMaxp,
        bool includeFvar,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (gdef != null)
        {
            tables.Add(CreateTable(FontTags.Gdef, gdef));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(12)
                .CopyBytes()));
        }

        if (includeFvar)
        {
            tables.Add(CreateTable(FontTags.Fvar, BuildFvar()));
            tables.Add(CreateTable(FontTags.Name, BuildName()));
        }

        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            mode,
            limits);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildEmpty(int minorVersion)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)minorVersion)
            .WriteZeros(8);

        if (minorVersion >= 2)
        {
            builder.WriteUInt16(0);
        }

        if (minorVersion >= 3)
        {
            builder.WriteUInt32(0);
        }

        return builder.CopyBytes();
    }

    internal static byte[] BuildVersion13()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteZeros(14);

        int glyphClasses = builder.GetLength();
        builder.PatchUInt16(4, (ushort)glyphClasses);
        builder.WriteUInt16(2).WriteUInt16(4);
        WriteClassRange(builder, 1, 2, 1);
        WriteClassRange(builder, 4, 4, 2);
        WriteClassRange(builder, 6, 6, 3);
        WriteClassRange(builder, 8, 8, 4);

        int attachments = builder.GetLength();
        builder.PatchUInt16(6, (ushort)attachments);
        builder.WriteUInt16(0).WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);
        int attachmentCoverage = WriteCoverage1(builder, new int[] { 1, 6 });
        builder.PatchUInt16(attachments,
            (ushort)(attachmentCoverage - attachments));
        int firstAttachment = builder.GetLength();
        builder.PatchUInt16(attachments + 4,
            (ushort)(firstAttachment - attachments));
        builder.WriteUInt16(2).WriteUInt16(2).WriteUInt16(5);
        int secondAttachment = builder.GetLength();
        builder.PatchUInt16(attachments + 6,
            (ushort)(secondAttachment - attachments));
        builder.WriteUInt16(1).WriteUInt16(7);

        int ligatures = builder.GetLength();
        builder.PatchUInt16(8, (ushort)ligatures);
        builder.WriteUInt16(0).WriteUInt16(1).WriteUInt16(0);
        int ligatureCoverage = WriteCoverage1(builder, new int[] { 4 });
        builder.PatchUInt16(ligatures,
            (ushort)(ligatureCoverage - ligatures));
        int ligatureGlyph = builder.GetLength();
        builder.PatchUInt16(ligatures + 4,
            (ushort)(ligatureGlyph - ligatures));
        builder.WriteUInt16(3).WriteUInt16(0).WriteUInt16(0).WriteUInt16(0);
        int firstCaret = builder.GetLength();
        builder.PatchUInt16(ligatureGlyph + 2,
            (ushort)(firstCaret - ligatureGlyph));
        builder.WriteUInt16(1).WriteInt16(-40);
        int secondCaret = builder.GetLength();
        builder.PatchUInt16(ligatureGlyph + 4,
            (ushort)(secondCaret - ligatureGlyph));
        builder.WriteUInt16(2).WriteUInt16(9);
        int thirdCaret = builder.GetLength();
        builder.PatchUInt16(ligatureGlyph + 6,
            (ushort)(thirdCaret - ligatureGlyph));
        builder.WriteUInt16(3).WriteInt16(120).WriteUInt16(6);
        builder.WriteUInt16(0).WriteUInt16(0).WriteUInt16(0x8000);

        int markClasses = builder.GetLength();
        builder.PatchUInt16(10, (ushort)markClasses);
        builder.WriteUInt16(1).WriteUInt16(6).WriteUInt16(2);
        builder.WriteUInt16(1).WriteUInt16(2);

        int markSets = builder.GetLength();
        builder.PatchUInt16(12, (ushort)markSets);
        builder.WriteUInt16(1).WriteUInt16(2).WriteUInt32(0).WriteUInt32(0);
        int firstSet = WriteCoverage1(builder, new int[] { 6, 7 });
        builder.PatchUInt32(markSets + 4, (uint)(firstSet - markSets));
        int secondSet = builder.GetLength();
        builder.PatchUInt32(markSets + 8, (uint)(secondSet - markSets));
        builder.WriteUInt16(2).WriteUInt16(1);
        builder.WriteUInt16(8).WriteUInt16(10).WriteUInt16(0);

        int variationStore = builder.GetLength();
        builder.PatchUInt32(14, (uint)variationStore);
        WriteVariationStore(builder);
        return builder.CopyBytes();
    }

    static int WriteCoverage1(TestBinaryBuilder builder, int[] glyphIds)
    {
        int offset = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16((ushort)glyphIds.Length);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }

        return offset;
    }

    static void WriteClassRange(
        TestBinaryBuilder builder,
        int startGlyph,
        int endGlyph,
        int classValue)
    {
        builder.WriteUInt16((ushort)startGlyph);
        builder.WriteUInt16((ushort)endGlyph);
        builder.WriteUInt16((ushort)classValue);
    }

    static void WriteVariationStore(TestBinaryBuilder builder)
    {
        builder.WriteUInt16(1).WriteUInt32(12).WriteUInt16(1).WriteUInt32(28);
        builder.WriteUInt16(2).WriteUInt16(1);
        builder.WriteInt16(0).WriteInt16(0x4000).WriteInt16(0x4000);
        builder.WriteInt16(0).WriteInt16(0).WriteInt16(0);
        builder.WriteUInt16(1).WriteUInt16(1).WriteUInt16(1);
        builder.WriteUInt16(0).WriteInt16(100);
    }

    internal static byte[] BuildFvar()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(12);
        WriteAxis(builder, "wght", 256);
        WriteAxis(builder, "wdth", 257);
        return builder.CopyBytes();
    }

    internal static byte[] BuildName()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(30)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(256)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(257)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(0x0057)
            .WriteUInt16(0x0044)
            .CopyBytes();
    }

    static void WriteAxis(
        TestBinaryBuilder builder,
        string tag,
        int nameId)
    {
        builder.WriteTag(tag);
        builder.WriteInt32(0);
        builder.WriteInt32(0);
        builder.WriteInt32(0x00010000);
        builder.WriteUInt16(0);
        builder.WriteUInt16((ushort)nameId);
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static FontLimits CreateLimits(int maximumEntries)
    {
        return FontLimits.Create(
            100000,
            100000,
            10,
            10,
            65535,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            maximumEntries,
            100,
            100000,
            10,
            100,
            10,
            10);
    }
}

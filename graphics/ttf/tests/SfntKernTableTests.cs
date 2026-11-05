namespace ttf.Tests;

internal class SfntKernTableTests
{
    [Test]
    public void ParsesWindowsOrderedPairsAndCoverageFlags()
    {
        SfntKernTable? table = Parse(BuildWindows(WindowsFormat0()));
        SfntKernSubtable subtable = table!.GetSubtable(0);
        SfntKernFormat0Data? format = subtable.GetFormat0();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersionRaw(), Is.Zero);
            Assert.That(table.IsAppleFormat(), Is.False);
            Assert.That(table.GetSubtableCount(), Is.EqualTo(1));
            Assert.That(subtable.GetFormat(), Is.Zero);
            Assert.That(subtable.IsHorizontal(), Is.True);
            Assert.That(subtable.IsVertical(), Is.False);
            Assert.That(subtable.IsCrossStream(), Is.True);
            Assert.That(subtable.OverridesPreviousValues(), Is.True);
            Assert.That(subtable.ContainsMinimumValues(), Is.False);
            Assert.That(subtable.IsVariationData(), Is.False);
            Assert.That(format!.GetPairCount(), Is.EqualTo(2));
            Assert.That(format.GetSearchRange(), Is.EqualTo(12));
            Assert.That(format.GetEntrySelector(), Is.EqualTo(1));
            Assert.That(format.GetRangeShift(), Is.Zero);
            Assert.That(format.GetPair(0).GetLeftGlyphId(), Is.EqualTo(1));
            Assert.That(format.GetPair(0).GetRightGlyphId(), Is.EqualTo(2));
            Assert.That(format.GetPair(0).GetValue(), Is.EqualTo(-80));
            Assert.That(format.GetKerningValue(1, 2), Is.EqualTo(-80));
            Assert.That(format.GetKerningValue(3, 4), Is.EqualTo(25));
            Assert.That(format.GetKerningValue(2, 1), Is.Zero);
        });
    }

    [Test]
    public void ParsesWindowsVerticalFormat2ClassesAndValues()
    {
        SfntKernSubtable subtable = Parse(
            BuildWindows(WindowsFormat2()))!.GetSubtable(0);
        SfntKernFormat2Data? format = subtable.GetFormat2();

        Assert.Multiple(() =>
        {
            Assert.That(subtable.GetFormat(), Is.EqualTo(2));
            Assert.That(subtable.IsVertical(), Is.True);
            Assert.That(subtable.IsHorizontal(), Is.False);
            Assert.That(subtable.ContainsMinimumValues(), Is.True);
            Assert.That(format!.GetRowWidth(), Is.EqualTo(4));
            Assert.That(format.GetKerningArrayOffset(), Is.EqualTo(30));
            Assert.That(format.GetLeftClassTable().GetFirstGlyphId(), Is.EqualTo(1));
            Assert.That(format.GetLeftClassTable().GetOffsetForGlyph(2), Is.EqualTo(4));
            Assert.That(format.GetRightClassTable().GetOffsetForGlyph(4), Is.EqualTo(2));
            Assert.That(format.GetValueCount(), Is.EqualTo(4));
            Assert.That(format.GetKerningValue(2, 4), Is.EqualTo(-60));
            Assert.That(format.GetKerningValue(1, 4), Is.Zero);
            Assert.That(format.GetKerningValue(9, 9), Is.Zero);
        });
    }

    [Test]
    public void ParsesAppleFormat2SubtableRelativeLeftOffsets()
    {
        SfntKernSubtable subtable = Parse(
            BuildApple(AppleFormat2()))!.GetSubtable(0);
        SfntKernFormat2Data? format = subtable.GetFormat2();

        Assert.Multiple(() =>
        {
            Assert.That(subtable.IsAppleFormat(), Is.True);
            Assert.That(subtable.IsHorizontal(), Is.True);
            Assert.That(format!.GetLeftClassTable().GetDefaultOffset(), Is.EqualTo(32));
            Assert.That(format.GetLeftClassTable().GetOffset(1), Is.EqualTo(36));
            Assert.That(format.GetKerningValue(2, 4), Is.EqualTo(-60));
            Assert.That(format.GetKerningValue(9, 9), Is.Zero);
        });
    }

    [Test]
    public void ExposesAppleCrossStreamStateTableForInspection()
    {
        SfntKernTable? table = Parse(BuildApple(AppleFormat1()));
        SfntKernSubtable subtable = table!.GetSubtable(0);
        SfntKernFormat1Data? format = subtable.GetFormat1();
        SfntKernStateEntry action = format!.GetEntry(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersionRaw(), Is.EqualTo(0x00010000U));
            Assert.That(table.IsAppleFormat(), Is.True);
            Assert.That(subtable.GetFormat(), Is.EqualTo(1));
            Assert.That(subtable.IsCrossStream(), Is.True);
            Assert.That(subtable.IsHorizontal(), Is.True);
            Assert.That(subtable.GetTupleIndex(), Is.Zero);
            Assert.That(format.GetClassCount(), Is.EqualTo(5));
            Assert.That(format.GetClassTableOffset(), Is.EqualTo(10));
            Assert.That(format.GetStateArrayOffset(), Is.EqualTo(16));
            Assert.That(format.GetStateDataOffset(), Is.EqualTo(16));
            Assert.That(format.GetEntryTableOffset(), Is.EqualTo(26));
            Assert.That(format.GetValueTableOffset(), Is.EqualTo(34));
            Assert.That(format.GetClassTable().GetClassForGlyph(2), Is.EqualTo(4));
            Assert.That(format.GetClassTable().GetClassForGlyph(9), Is.EqualTo(1));
            Assert.That(format.GetStateCount(), Is.EqualTo(2));
            Assert.That(format.GetInitialStateIndex(), Is.Zero);
            Assert.That(format.GetStateEntryIndex(0, 4), Is.EqualTo(1));
            Assert.That(format.GetEntryCount(), Is.EqualTo(2));
            Assert.That(action.GetNewStateOffset(), Is.EqualTo(21));
            Assert.That(action.GetNewStateIndex(), Is.EqualTo(1));
            Assert.That(action.PushesGlyph(), Is.True);
            Assert.That(action.DoesNotAdvance(), Is.False);
            Assert.That(action.GetValueOffset(), Is.EqualTo(34));
            Assert.That(format.GetValueCount(), Is.EqualTo(2));
            Assert.That(format.GetValue(0), Is.EqualTo(683));
            Assert.That(format.GetValue(1), Is.EqualTo(-1));
        });
    }

    [Test]
    public void RecognizesAppleStateArrayOffsetUsedAsInitialState()
    {
        byte[] subtable = AppleFormat1();
        subtable[12] = 0;
        subtable[13] = 21;
        SfntKernFormat1Data? format = Parse(
            BuildApple(subtable))!.GetSubtable(0).GetFormat1();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetStateArrayOffset(), Is.EqualTo(21));
            Assert.That(format.GetStateDataOffset(), Is.EqualTo(16));
            Assert.That(format.GetInitialStateIndex(), Is.EqualTo(1));
            Assert.That(format.GetStateEntryIndex(0, 4), Is.EqualTo(1));
        });
    }

    [Test]
    public void ParsesAppleCompactClassesAndVariationMetadata()
    {
        SfntKernSubtable subtable = Parse(
            BuildApple(AppleFormat3()))!.GetSubtable(0);
        SfntKernFormat3Data? format = subtable.GetFormat3();

        Assert.Multiple(() =>
        {
            Assert.That(subtable.GetFormat(), Is.EqualTo(3));
            Assert.That(subtable.IsVertical(), Is.True);
            Assert.That(subtable.IsVariationData(), Is.True);
            Assert.That(subtable.GetTupleIndex(), Is.EqualTo(7));
            Assert.That(format!.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(format.GetValueCount(), Is.EqualTo(3));
            Assert.That(format.GetLeftClassCount(), Is.EqualTo(2));
            Assert.That(format.GetRightClassCount(), Is.EqualTo(2));
            Assert.That(format.GetLeftClass(2), Is.EqualTo(1));
            Assert.That(format.GetRightClass(2), Is.EqualTo(1));
            Assert.That(format.GetValueIndex(1, 1), Is.EqualTo(1));
            Assert.That(format.GetKerningValue(2, 2), Is.EqualTo(-40));
            Assert.That(format.GetKerningValue(0, 2), Is.Zero);
        });
    }

    [Test]
    public void ParsesMultipleAppleSubtablesInOrder()
    {
        SfntKernTable? table = Parse(BuildApple(
            AppleFormat0(),
            AppleFormat3()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetSubtableCount(), Is.EqualTo(2));
            Assert.That(table.GetSubtable(0).GetFormat(), Is.Zero);
            Assert.That(table.GetSubtable(0).GetFormat0()!.GetPairCount(), Is.EqualTo(3));
            Assert.That(table.GetSubtable(0).GetFormat0()!.GetPair(2).GetKey(), Is.EqualTo(uint.MaxValue));
            Assert.That(table.GetSubtable(1).GetFormat(), Is.EqualTo(3));
        });
    }

    [TestCaseSource(nameof(InvalidKernTables))]
    public void RejectsMalformedKernStructures(byte[] kern)
    {
        Assert.Throws<FontFormatException>(() => Parse(kern));
    }

    [Test]
    public void RequiresMaxpAndReturnsNullWhenKernIsAbsent()
    {
        SfntTableCache missingMaxp = CreateCache(
            BuildWindows(WindowsFormat0()),
            false);
        SfntTableCache missingKern = CreateCache(null, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntKernTableParser.FindTable(missingMaxp));
            Assert.That(SfntKernTableParser.FindView(missingKern), Is.Null);
            Assert.That(SfntKernTableParser.FindTable(missingKern), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntKernTableParser.FindView(null!));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndPreservesUnknownVersionsAsRawData()
    {
        SfntTableCache cache = CreateCache(BuildWindows(WindowsFormat0()), true);
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(FontOutlineKind.Unknown, Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknown = new TestBinaryBuilder()
            .WriteUInt32(0x00020000U)
            .CopyBytes();
        SfntParsedTableView? rawView = SfntKernTableParser.FindView(
            CreateCache(unknown, true));

        Assert.Multiple(() =>
        {
            Assert.That(face.GetKernTable(), Is.Not.Null);
            Assert.That(face.GetKernTableView()!.GetParsedData(), Is.SameAs(face.GetKernTable()));
            Assert.That(rawView!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(rawView.HasParsedData(), Is.False);
        });
    }

    static IEnumerable<byte[]> InvalidKernTables()
    {
        byte[] unordered = WindowsFormat0();
        unordered[20] = 0;
        unordered[21] = 0;
        yield return BuildWindows(unordered);

        byte[] badSearch = WindowsFormat0();
        badSearch[10] = 0;
        badSearch[11] = 6;
        yield return BuildWindows(badSearch);

        byte[] badReserved = WindowsFormat0();
        badReserved[5] |= 0x10;
        yield return BuildWindows(badReserved);

        byte[] badClassOffset = WindowsFormat2();
        badClassOffset[16] = 0;
        badClassOffset[17] = 3;
        yield return BuildWindows(badClassOffset);

        byte[] badStateEntry = AppleFormat1();
        badStateEntry[24] = 2;
        yield return BuildApple(badStateEntry);

        byte[] badActionOffset = AppleFormat1();
        badActionOffset[40] = 0x80;
        badActionOffset[41] = 0x21;
        yield return BuildApple(badActionOffset);

        byte[] badCompactClass = AppleFormat3();
        badCompactClass[20] = 2;
        yield return BuildApple(badCompactClass);

        byte[] trailing = BuildWindows(WindowsFormat0());
        yield return trailing.Concat(new byte[] { 0 }).ToArray();
    }

    static SfntKernTable? Parse(byte[] kern)
    {
        return SfntKernTableParser.FindTable(CreateCache(kern, true));
    }

    static SfntTableCache CreateCache(byte[]? kern, bool includeMaxp)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (kern != null)
        {
            tables.Add(CreateTable(FontTags.Kern, kern));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(5)
                .CopyBytes()));
        }

        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(
            store,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
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

    static byte[] BuildWindows(byte[] subtable)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteBytes(subtable)
            .CopyBytes();
    }

    static byte[] BuildApple(params byte[][] subtables)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt32((uint)subtables.Length);

        foreach (byte[] subtable in subtables)
        {
            builder.WriteBytes(subtable);
        }

        return builder.CopyBytes();
    }

    static byte[] WindowsFormat0()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(26)
            .WriteUInt16(0x000D)
            .WriteUInt16(2)
            .WriteUInt16(12)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteInt16(-80)
            .WriteUInt16(3)
            .WriteUInt16(4)
            .WriteInt16(25)
            .CopyBytes();
    }

    static byte[] AppleFormat0()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(34)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(12)
            .WriteUInt16(1)
            .WriteUInt16(6)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteInt16(-80)
            .WriteUInt16(3)
            .WriteUInt16(4)
            .WriteInt16(25)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(0xFFFF)
            .WriteInt16(0)
            .CopyBytes();
    }

    static byte[] WindowsFormat2()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(38)
            .WriteUInt16(0x0202)
            .WriteUInt16(4)
            .WriteUInt16(14)
            .WriteUInt16(22)
            .WriteUInt16(30)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(3)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(-30)
            .WriteInt16(-60)
            .CopyBytes();
    }

    static byte[] AppleFormat2()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(40)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(16)
            .WriteUInt16(24)
            .WriteUInt16(32)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(32)
            .WriteUInt16(36)
            .WriteUInt16(3)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(-30)
            .WriteInt16(-60)
            .CopyBytes();
    }

    static byte[] AppleFormat1()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(46)
            .WriteUInt16(0x4001)
            .WriteUInt16(0)
            .WriteUInt16(5)
            .WriteUInt16(10)
            .WriteUInt16(16)
            .WriteUInt16(26)
            .WriteUInt16(34)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt8(4)
            .WriteUInt8(1)
            .WriteBytes(new byte[] { 0, 0, 0, 0, 1 })
            .WriteBytes(new byte[] { 0, 0, 0, 0, 1 })
            .WriteUInt16(16)
            .WriteUInt16(0)
            .WriteUInt16(21)
            .WriteUInt16(0x8022)
            .WriteInt16(683)
            .WriteInt16(-1)
            .CopyBytes();
    }

    static byte[] AppleFormat3()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(34)
            .WriteUInt16(0xA003)
            .WriteUInt16(7)
            .WriteUInt16(5)
            .WriteUInt8(3)
            .WriteUInt8(2)
            .WriteUInt8(2)
            .WriteUInt8(0)
            .WriteInt16(0)
            .WriteInt16(-40)
            .WriteInt16(20)
            .WriteBytes(new byte[] { 0, 1, 1, 0, 1 })
            .WriteBytes(new byte[] { 0, 0, 1, 1, 1 })
            .WriteBytes(new byte[] { 0, 0, 0, 1 })
            .CopyBytes();
    }
}

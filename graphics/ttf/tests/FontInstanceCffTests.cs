using ttf;

namespace ttf.Tests;

internal class FontInstanceCffTests
{
    [Test]
    public void Cff1UsesUnifiedOutlineBoundsMetricsNamesAndMissingGlyph()
    {
        FontInstance instance = CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            BuildCff1(),
            2).CreateDefaultInstance();
        GlyphOutline missing = instance.GetGlyphOutline(0);
        GlyphOutline outline = instance.GetGlyphOutline(1);
        GlyphMetrics missingMetrics = instance.GetGlyphMetrics(0);
        GlyphMetrics metrics = instance.GetGlyphMetrics(1);
        CffStemHint horizontalStem = outline.GetCffStemHint(0);
        CffStemHint verticalStem = outline.GetCffStemHint(1);
        CffHintMask hintMask = outline.GetCffHintMask(0);
        CffHintMask counterMask = outline.GetCffHintMask(1);
        CffStemHint?[] stemHints = outline.GetCffStemHints();
        CffHintMask?[] hintMasks = outline.GetCffHintMasks();
        byte[] hintMaskBytes = hintMask.CopyBytes();
        stemHints[0] = null;
        hintMasks[0] = null;
        hintMaskBytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsEmpty(), Is.True);
            Assert.That(instance.GetGlyphBounds(0)!.IsEmpty(), Is.True);
            Assert.That(instance.GetGlyphName(0), Is.EqualTo(".notdef"));
            Assert.That(missingMetrics.GetGlyphId(), Is.Zero);
            Assert.That(missingMetrics.GetAdvanceWidth(), Is.EqualTo(600));
            Assert.That(missingMetrics.GetTopSideBearing(), Is.EqualTo(1000));
            Assert.That(instance.GetGlyphName(1), Is.EqualTo("A"));
            Assert.That(outline.GetGlyphId(), Is.EqualTo(1));
            Assert.That(outline.IsComposite(), Is.False);
            Assert.That(outline.GetCommandCount(), Is.EqualTo(3));
            Assert.That(outline.GetCommand(1).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Line));
            Assert.That(outline.GetCommand(1).GetEndX(), Is.EqualTo(20));
            Assert.That(outline.GetCommand(1).GetEndY(), Is.EqualTo(30));
            Assert.That(instance.GetGlyphBounds(1), Is.SameAs(outline.GetBounds()));
            Assert.That(outline.GetBounds()!.GetXMaximum(), Is.EqualTo(20));
            Assert.That(outline.GetBounds()!.GetYMaximum(), Is.EqualTo(30));
            Assert.That(outline.GetComponentCount(), Is.Zero);
            Assert.That(outline.GetInstructionLength(), Is.Zero);
            Assert.That(instance.GetTrueTypeHintingData(), Is.Null);
            Assert.That(outline.GetCffStemHintCount(), Is.EqualTo(2));
            Assert.That(outline.GetCffStemHint(0), Is.SameAs(horizontalStem));
            Assert.That(horizontalStem.IsVertical(), Is.False);
            Assert.That(horizontalStem.GetPositionDelta(), Is.EqualTo(10));
            Assert.That(horizontalStem.GetWidth(), Is.EqualTo(20));
            Assert.That(verticalStem.IsVertical(), Is.True);
            Assert.That(verticalStem.GetPositionDelta(), Is.EqualTo(30));
            Assert.That(verticalStem.GetWidth(), Is.EqualTo(40));
            Assert.That(outline.GetCffHintMaskCount(), Is.EqualTo(2));
            Assert.That(outline.GetCffHintMask(0), Is.SameAs(hintMask));
            Assert.That(hintMask.IsCounterMask(), Is.False);
            Assert.That(hintMask.GetLength(), Is.EqualTo(1));
            Assert.That(hintMask.GetCommandIndex(), Is.Zero);
            Assert.That(hintMask.CopyBytes(), Is.EqualTo(new byte[] { 0xC0 }));
            Assert.That(counterMask.IsCounterMask(), Is.True);
            Assert.That(counterMask.CopyBytes(), Is.EqualTo(new byte[] { 0x80 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                outline.GetCffStemHint(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                outline.GetCffHintMask(2));
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(700));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(15));
            Assert.That(metrics.GetVerticalOriginY(), Is.EqualTo(1000));
            Assert.That(metrics.GetTopSideBearing(), Is.EqualTo(970));
            Assert.That(instance.GetGlyphOutline(1), Is.SameAs(outline));
            Assert.That(instance.GetGlyphMetrics(1), Is.SameAs(metrics));
        });
    }

    [Test]
    public void Cff2UsesUnifiedOutlineAndOpenTypeMetrics()
    {
        FontInstance instance = CreateFace(
            FontOutlineKind.Cff2,
            FontTags.Cff2,
            BuildCff2(),
            2).CreateDefaultInstance();
        GlyphOutline outline = instance.GetGlyphOutline(1);
        GlyphMetrics missingMetrics = instance.GetGlyphMetrics(0);
        GlyphMetrics metrics = instance.GetGlyphMetrics(1);

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetGlyphOutline(0).IsEmpty(), Is.True);
            Assert.That(missingMetrics.GetGlyphId(), Is.Zero);
            Assert.That(missingMetrics.GetAdvanceWidth(), Is.EqualTo(600));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(3));
            Assert.That(outline.GetCommand(0).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Move));
            Assert.That(outline.GetCommand(1).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Line));
            Assert.That(outline.GetCommand(2).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Close));
            Assert.That(outline.GetBounds()!.GetXMaximum(), Is.EqualTo(20));
            Assert.That(outline.GetBounds()!.GetYMaximum(), Is.EqualTo(30));
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(700));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(15));
            Assert.That(instance.GetGlyphName(1), Is.EqualTo("gid0001"));
            Assert.That(instance.GetTrueTypeHintingData(), Is.Null);
            Assert.That(outline.GetCffStemHintCount(), Is.EqualTo(2));
            Assert.That(outline.GetCffStemHint(0).IsVertical(), Is.False);
            Assert.That(outline.GetCffStemHint(1).IsVertical(), Is.True);
            Assert.That(outline.GetCffHintMaskCount(), Is.EqualTo(2));
            Assert.That(outline.GetCffHintMask(0).CopyBytes(),
                Is.EqualTo(new byte[] { 0xC0 }));
            Assert.That(outline.GetCffHintMask(1).IsCounterMask(), Is.True);
            Assert.That(outline.GetCffHintMask(1).CopyBytes(),
                Is.EqualTo(new byte[] { 0x80 }));
        });
    }

    [Test]
    public void Cff2AppliesVvarAdvanceBearingAndVerticalOriginMaps()
    {
        FontFace face = CreateVariableMetricFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        GlyphMetrics metrics = face
            .CreateInstance(selectedCoordinates)
            .GetGlyphMetrics(0);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(600));
            Assert.That(metrics.GetAdvanceHeight(), Is.EqualTo(950));
            Assert.That(metrics.GetTopSideBearing(), Is.EqualTo(105));
            Assert.That(metrics.GetVerticalOriginY(), Is.EqualTo(1000));
            Assert.That(metrics.UsesSynthesizedVerticalAdvance(), Is.False);
            Assert.That(metrics.UsesVorgOrigin(), Is.True);
        });
    }

    [Test]
    public void MaterializesCff2BlendAsReopenableStaticCff1()
    {
        FontFace face = CreateReopenableVariableOutlineFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        byte[] bytes = face
            .CreateInstance(selectedCoordinates)
            .CopyStaticSfntBytes();
        FontInstance varied = face.CreateInstance(selectedCoordinates);
        byte[] repeatedBytes = varied.CopyStaticSfntBytes();
        byte[] secondRepeatedBytes = varied.CopyStaticSfntBytes();
        FontFace staticFace = FontLoader.Load(
            bytes,
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance materialized = staticFace.CreateDefaultInstance();
        GlyphOutline outline = materialized.GetGlyphOutline(1);
        GlyphMetrics metrics = materialized.GetGlyphMetrics(1);
        int privateTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");

        Assert.Multiple(() =>
        {
            Assert.That(repeatedBytes, Is.EqualTo(bytes));
            Assert.That(secondRepeatedBytes, Is.EqualTo(bytes));
            Assert.That(secondRepeatedBytes, Is.Not.SameAs(repeatedBytes));
            Assert.That(FontChecksum.VerifyFont(bytes), Is.True);
            Assert.That(staticFace.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(staticFace.HasTable(FontTags.Cff), Is.True);
            Assert.That(staticFace.HasTable(FontTags.Cff2), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Fvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Hvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Dsig), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Stat), Is.False);
            Assert.That(staticFace.HasTable(privateTag), Is.True);
            Assert.That(staticFace.GetNames().GetSubfamilyName()!.GetValue(),
                Is.EqualTo("Medium"));
            Assert.That(staticFace.GetNames().GetTypographicSubfamilyName()!
                .GetValue(), Is.EqualTo("Medium"));
            Assert.That(staticFace.GetNames().GetFullName()!.GetValue(),
                Is.EqualTo("Demo Medium"));
            Assert.That(staticFace.GetNames().GetPostScriptName()!.GetValue(),
                Is.EqualTo("Demo-Medium"));
            Assert.That(staticFace.GetHeadTable()!.GetXMin(), Is.EqualTo(0));
            Assert.That(staticFace.GetHeadTable()!.GetYMin(), Is.EqualTo(0));
            Assert.That(staticFace.GetHeadTable()!.GetXMax(), Is.EqualTo(177));
            Assert.That(staticFace.GetHeadTable()!.GetYMax(), Is.EqualTo(10));
            Assert.That(staticFace.GetCffTable()!.GetTopDictionary()
                .CopyFontBoundingBox(), Is.EqualTo(
                    new double[] { 0, 0, 177, 10 }));
            Assert.That(staticFace.GetHheaTable()!.GetMinimumLeftSideBearing(),
                Is.EqualTo(20));
            Assert.That(staticFace.GetHheaTable()!.GetMinimumRightSideBearing(),
                Is.EqualTo(558));
            Assert.That(staticFace.GetHheaTable()!.GetMaximumXExtent(),
                Is.EqualTo(202));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(4));
            Assert.That(outline.GetCommand(1).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Line));
            Assert.That(outline.GetCommand(1).GetEndX(), Is.EqualTo(146.5));
            Assert.That(outline.GetCommand(1).GetEndY(), Is.Zero);
            Assert.That(outline.GetCommand(2).GetKind(),
                Is.EqualTo(GlyphPathCommandKind.Cubic));
            Assert.That(outline.GetCommand(2).GetEndX(), Is.EqualTo(176.5));
            Assert.That(outline.GetCommand(2).GetEndY(), Is.EqualTo(10));
            Assert.That(staticFace.GetCffTable()!.GetCharset().GetName(1),
                Is.EqualTo("gid0001"));
            Assert.That(staticFace.GetCffTable()!
                .GetGlobalSubroutineIndex().GetCount(), Is.Zero);
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(760));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(25));
            Assert.That(staticFace.HasTable(FontTags.Vhea), Is.True);
            Assert.That(staticFace.HasTable(FontTags.Vmtx), Is.True);
        });

        repeatedBytes[0] = 0;
        Assert.That(varied.CopyStaticSfntBytes(), Is.EqualTo(bytes));
    }

    [Test]
    public void ReopenedCffInstanceHasNoLiveVariationDependencies()
    {
        FontFace source = CreateReopenableVariableOutlineFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set(
            (int)TestSfntEncoding.EncodeTag("wght"),
            0.5);

        Assert.Multiple(() =>
        {
            Assert.That(source.HasTable(FontTags.Avar), Is.True);
            Assert.That(source.HasTable(FontTags.Cff2), Is.True);
            Assert.That(source.HasTable(FontTags.Fvar), Is.True);
            Assert.That(source.HasTable(FontTags.Hvar), Is.True);
            Assert.That(source.HasTable(FontTags.Mvar), Is.True);
            Assert.That(source.HasTable(FontTags.Stat), Is.True);
        });

        byte[] firstBytes = source
            .CreateInstance(selectedCoordinates)
            .CopyStaticSfntBytes();
        FontFace firstFace = FontLoader.Load(
            firstBytes,
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance firstInstance = firstFace.CreateDefaultInstance();
        byte[] secondBytes = firstInstance.CopyStaticSfntBytes();
        FontFace secondFace = FontLoader.Load(
            secondBytes,
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance secondInstance = secondFace.CreateDefaultInstance();

        AssertStaticCffVariationTablesAbsent(firstFace);
        AssertStaticCffVariationTablesAbsent(secondFace);
        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(firstBytes), Is.True);
            Assert.That(FontChecksum.VerifyFont(secondBytes), Is.True);
            Assert.That(firstFace.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(secondFace.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(firstFace.GetNamedInstanceCount(), Is.Zero);
            Assert.That(secondFace.GetNamedInstanceCount(), Is.Zero);
            Assert.That(firstInstance.GetCoordinates().GetAxisCount(), Is.Zero);
            Assert.That(secondInstance.GetCoordinates().GetAxisCount(), Is.Zero);
            Assert.That(secondInstance.GetGlyphOutline(1)
                .GetCommand(1).GetEndX(), Is.EqualTo(146.5));
            Assert.That(secondInstance.GetGlyphMetrics(1).GetAdvanceWidth(),
                Is.EqualTo(760));
        });
    }

    [Test]
    public void MaterializesCff2VvarAndVorgMetrics()
    {
        FontFace face = CreateReopenableVariableMetricFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontFace staticFace = FontLoader.Load(
            face.CreateInstance(selectedCoordinates).CopyStaticSfntBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        GlyphMetrics metrics = staticFace
            .CreateDefaultInstance()
            .GetGlyphMetrics(0);

        Assert.Multiple(() =>
        {
            Assert.That(staticFace.HasTable(FontTags.Vvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Vorg), Is.True);
            Assert.That(metrics.GetAdvanceHeight(), Is.EqualTo(950));
            Assert.That(metrics.GetVerticalOriginY(), Is.EqualTo(1000));
            Assert.That(metrics.UsesVorgOrigin(), Is.True);
        });
    }

    [Test]
    public void RejectsCffCharStringAndMaxpGlyphCountMismatch()
    {
        FontInstance cff1 = CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            BuildCff1(),
            3).CreateDefaultInstance();
        FontInstance cff2 = CreateFace(
            FontOutlineKind.Cff2,
            FontTags.Cff2,
            BuildCff2(),
            3).CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => cff1.GetGlyphOutline(0));
            Assert.Throws<FontFormatException>(() => cff2.GetGlyphOutline(0));
        });
    }

    internal static FontFace CreateFace(
        FontOutlineKind kind,
        int outlineTag,
        byte[] outlineData,
        int glyphCount)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp(glyphCount));
        AddTable(tables, FontTags.Hhea, BuildHhea(glyphCount));
        AddTable(tables, FontTags.Hmtx, BuildHmtx(glyphCount));
        AddTable(tables, outlineTag, outlineData);

        if (kind == FontOutlineKind.Cff1)
        {
            AddTable(tables, FontTags.Post, BuildPost(glyphCount));
        }
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(kind, Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateVariableMetricFace()
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp(1));
        AddTable(tables, FontTags.Hhea, BuildHhea(1));
        AddTable(tables, FontTags.Hmtx, BuildHmtx(1));
        AddTable(tables, FontTags.Vhea, BuildVhea());
        AddTable(tables, FontTags.Vmtx, BuildVmtx());
        AddTable(tables, FontTags.Vorg, BuildVorg());
        AddTable(tables, FontTags.Name, BuildVariationName());
        AddTable(tables, FontTags.Fvar, BuildVariationFvar());
        AddTable(tables, FontTags.Vvar, BuildVvar());
        AddTable(tables, FontTags.Cff2, BuildVariableCff2());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                FontOutlineKind.Cff2,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    internal static FontFace CreateReopenableVariableOutlineFace()
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp(2));
        AddTable(tables, FontTags.Hhea, BuildHhea(2));
        AddTable(tables, FontTags.Hmtx, BuildHmtx(2));
        AddTable(tables, FontTags.Cmap, new byte[4]);
        AddTable(tables, FontTags.Name, BuildVariationName());
        AddTable(tables, FontTags.Os2, new byte[4]);
        AddTable(tables, FontTags.Post, BuildPost(2));
        AddTable(tables, FontTags.Fvar, BuildVariationFvar());
        AddTable(tables, FontTags.Avar, BuildIdentityAvar());
        AddTable(tables, FontTags.Hvar, BuildHvar());
        AddTable(tables, FontTags.Mvar, BuildEmptyMvar());
        AddTable(tables, FontTags.Cff2, BuildVariableOutlineCff2());
        AddTable(tables, FontTags.Dsig, new byte[8]);
        AddTable(tables, FontTags.Stat, new byte[8]);
        AddTable(tables, (int)TestSfntEncoding.EncodeTag("ZZZZ"),
            new byte[] { 1, 2, 3 });
        return CreateFaceFromTables(tables);
    }

    static FontFace CreateReopenableVariableMetricFace()
    {
        FontFace source = CreateVariableMetricFace();
        SfntTableStore sourceTables = source.GetTableStore();
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;

        while (index < sourceTables.GetTableCount())
        {
            tables.Add(sourceTables.GetTable(index));
            index += 1;
        }

        AddTable(tables, FontTags.Cmap, new byte[4]);
        AddTable(tables, FontTags.Os2, new byte[4]);
        AddTable(tables, FontTags.Post, BuildPost(1));
        return CreateFaceFromTables(tables);
    }

    static FontFace CreateFaceFromTables(List<SfntTableData> tables)
    {
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                FontOutlineKind.Cff2,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildCff1()
    {
        byte[] charStrings = BuildCff1Index(
            new byte[] { 14 },
            new byte[]
            {
                149, 159, 1, 169, 179, 23,
                19, 0xC0, 20, 0x80,
                139, 139, 21, 159, 169, 5, 14
            });
        int prefixLength = BuildCff1Prefix(
            BuildCff1Top(0)).Length;
        int attempt = 0;

        while (attempt < 4)
        {
            byte[] prefix = BuildCff1Prefix(
                BuildCff1Top(prefixLength));

            if (prefix.Length == prefixLength)
            {
                return Join(prefix, charStrings);
            }

            prefixLength = prefix.Length;
            attempt += 1;
        }

        throw new InvalidOperationException("CFF1 test offsets did not stabilize.");
    }

    static byte[] BuildCff1Top(int charStringsOffset)
    {
        return Join(
            DictInteger(charStringsOffset),
            new byte[] { 17, 139, 139, 18 });
    }

    static byte[] BuildCff1Prefix(byte[] top)
    {
        return Join(
            new byte[] { 1, 0, 4, 1 },
            BuildCff1Index(new byte[] { (byte)'X' }),
            BuildCff1Index(top),
            BuildCff1Index(),
            BuildCff1Index());
    }

    static byte[] BuildCff2()
    {
        byte[] charStrings = BuildCff2Index(
            Array.Empty<byte>(),
            new byte[]
            {
                149, 159, 1, 169, 179, 23,
                19, 0xC0, 20, 0x80,
                139, 139, 21, 159, 169, 5
            });
        int topSize = 13;
        int fixedEnd = 5 + topSize + 4;
        int fontDictionaryArrayOffset = fixedEnd + charStrings.Length;
        byte[] top = Join(
            DictInteger(fixedEnd),
            new byte[] { 17 },
            DictInteger(fontDictionaryArrayOffset),
            new byte[] { 12, 36 });
        byte[] fontDictionaryArray = BuildCff2Index(
            new byte[] { 139, 139, 18 });
        return Join(
            new byte[] { 2, 0, 5, 0, (byte)top.Length },
            top,
            new byte[] { 0, 0, 0, 0 },
            charStrings,
            fontDictionaryArray);
    }

    static byte[] BuildVariableCff2()
    {
        byte[] store = BuildCff2VariationStore();
        byte[] charStrings = BuildCff2Index(Array.Empty<byte>());
        int topSize = 19;
        int charStringsOffset = 5 + topSize + 4;
        int fontDictionaryArrayOffset = charStringsOffset + charStrings.Length;
        int variationStoreOffset = fontDictionaryArrayOffset + 18;
        int privateOffset = variationStoreOffset + 2 + store.Length;
        byte[] fontDictionaryArray = BuildCff2Index(Join(
            DictInteger(2),
            DictInteger(privateOffset),
            new byte[] { 18 }));
        byte[] top = Join(
            DictInteger(charStringsOffset),
            new byte[] { 17 },
            DictInteger(variationStoreOffset),
            new byte[] { 24 },
            DictInteger(fontDictionaryArrayOffset),
            new byte[] { 12, 36 });
        return Join(
            new byte[] { 2, 0, 5, 0, (byte)top.Length },
            top,
            new byte[] { 0, 0, 0, 0 },
            charStrings,
            fontDictionaryArray,
            new byte[] { 0, (byte)store.Length },
            store,
            new byte[] { 139, 22 });
    }

    static byte[] BuildVariableOutlineCff2()
    {
        byte[] store = BuildCff2VariationStore();
        byte[] charStrings = BuildCff2Index(
            Array.Empty<byte>(),
            new byte[]
            {
                139, 139, 21,
                247, 12, 192, 140, 16, 6,
                149, 139, 149, 149, 149, 139, 8
            });
        int topSize = 19;
        int charStringsOffset = 5 + topSize + 4;
        int fontDictionaryArrayOffset = charStringsOffset + charStrings.Length;
        int variationStoreOffset = fontDictionaryArrayOffset + 18;
        int privateOffset = variationStoreOffset + 2 + store.Length;
        byte[] fontDictionaryArray = BuildCff2Index(Join(
            DictInteger(2),
            DictInteger(privateOffset),
            new byte[] { 18 }));
        byte[] top = Join(
            DictInteger(charStringsOffset),
            new byte[] { 17 },
            DictInteger(variationStoreOffset),
            new byte[] { 24 },
            DictInteger(fontDictionaryArrayOffset),
            new byte[] { 12, 36 });
        return Join(
            new byte[] { 2, 0, 5, 0, (byte)top.Length },
            top,
            new byte[] { 0, 0, 0, 0 },
            charStrings,
            fontDictionaryArray,
            new byte[] { 0, (byte)store.Length },
            store,
            new byte[] { 139, 22 });
    }

    static byte[] BuildCff2VariationStore()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildVariationName()
    {
        int[] nameIds = new int[] { 1, 2, 4, 6, 17, 256, 257, 258 };
        string[] values = new string[]
        {
            "Demo", "Regular", "Demo Regular", "Demo-Regular", "Regular",
            "Weight", "Medium", "Demo-Medium"
        };
        byte[][] encoded = values.Select(
            System.Text.Encoding.BigEndianUnicode.GetBytes).ToArray();
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)(6 + values.Length * 12));
        int offset = 0;

        for (int index = 0; index < values.Length; index += 1)
        {
            builder.WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)offset);
            offset += encoded[index].Length;
        }

        foreach (byte[] value in encoded) { builder.WriteBytes(value); }
        return builder.CopyBytes();
    }

    static byte[] BuildVariationFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(20)
            .WriteUInt16(1)
            .WriteUInt16(10)
            .WriteTag("wght")
            .WriteInt32(0)
            .WriteInt32(0)
            .WriteInt32(0x00010000)
            .WriteUInt16(0)
            .WriteUInt16(256)
            .WriteUInt16(257)
            .WriteUInt16(0)
            .WriteInt32(0x00008000)
            .WriteUInt16(258)
            .CopyBytes();
    }

    static byte[] BuildIdentityAvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(3)
            .WriteInt16(-0x4000).WriteInt16(-0x4000)
            .WriteInt16(0).WriteInt16(0)
            .WriteInt16(0x4000).WriteInt16(0x4000)
            .CopyBytes();
    }

    static byte[] BuildEmptyMvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(8)
            .WriteUInt16(0).WriteUInt16(0)
            .CopyBytes();
    }

    static void AssertStaticCffVariationTablesAbsent(FontFace face)
    {
        Assert.Multiple(() =>
        {
            Assert.That(face.HasTable(FontTags.Avar), Is.False);
            Assert.That(face.HasTable(FontTags.Cff2), Is.False);
            Assert.That(face.HasTable(FontTags.Cvar), Is.False);
            Assert.That(face.HasTable(FontTags.Fvar), Is.False);
            Assert.That(face.HasTable(FontTags.Gvar), Is.False);
            Assert.That(face.HasTable(FontTags.Hvar), Is.False);
            Assert.That(face.HasTable(FontTags.Mvar), Is.False);
            Assert.That(face.HasTable(FontTags.Stat), Is.False);
            Assert.That(face.HasTable(FontTags.Vvar), Is.False);
        });
    }

    static byte[] BuildVvar()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(44)
            .WriteUInt32(24)
            .WriteUInt32(29)
            .WriteUInt32(34)
            .WriteUInt32(39);
        WriteMetricMap(builder, 0);
        WriteMetricMap(builder, 1);
        WriteMetricMap(builder, 2);
        WriteMetricMap(builder, 3);
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16(100)
            .WriteInt16(10)
            .WriteInt16(0)
            .WriteInt16(200);
        return builder.CopyBytes();
    }

    static byte[] BuildHvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(32)
            .WriteUInt32(20)
            .WriteUInt32(26)
            .WriteUInt32(26)
            .WriteUInt8(0).WriteUInt8(3).WriteUInt16(2)
            .WriteUInt8(0).WriteUInt8(1)
            .WriteUInt8(0).WriteUInt8(3).WriteUInt16(2)
            .WriteUInt8(2).WriteUInt8(3)
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16(100)
            .WriteInt16(120)
            .WriteInt16(20)
            .WriteInt16(20)
            .CopyBytes();
    }

    static void WriteMetricMap(TestBinaryBuilder builder, int innerIndex)
    {
        builder
            .WriteUInt8(0)
            .WriteUInt8(3)
            .WriteUInt16(1)
            .WriteUInt8((byte)innerIndex);
    }

    static byte[] BuildCff1Index(params byte[][] objects)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)objects.Length);

        if (objects.Length == 0)
        {
            return builder.CopyBytes();
        }

        builder.WriteUInt8(1).WriteUInt8(1);
        int offset = 1;

        foreach (byte[] value in objects)
        {
            offset += value.Length;
            builder.WriteUInt8((byte)offset);
        }

        foreach (byte[] value in objects)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCff2Index(params byte[][] objects)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32((uint)objects.Length);

        if (objects.Length == 0)
        {
            return builder.CopyBytes();
        }

        builder.WriteUInt8(1).WriteUInt8(1);
        int offset = 1;

        foreach (byte[] value in objects)
        {
            offset += value.Length;
            builder.WriteUInt8((byte)offset);
        }

        foreach (byte[] value in objects)
        {
            builder.WriteBytes(value);
        }

        return builder.CopyBytes();
    }

    static byte[] DictInteger(int value)
    {
        return new TestBinaryBuilder()
            .WriteUInt8(29)
            .WriteInt32(value)
            .CopyBytes();
    }

    static byte[] BuildHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(1000).WriteInt16(1000)
            .WriteUInt16(0).WriteUInt16(8).WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildHhea(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(750).WriteInt16(-250).WriteInt16(50)
            .WriteUInt16((ushort)(600 + (glyphCount - 1) * 100))
            .WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildHmtx(int glyphCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            builder.WriteUInt16((ushort)(600 + glyphId * 100));
            builder.WriteInt16((short)(10 + glyphId * 5));
            glyphId += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildVhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(0)
            .WriteUInt16(900).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildVmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(900)
            .WriteInt16(100)
            .CopyBytes();
    }

    static byte[] BuildVorg()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16(900)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildPost(int glyphCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00020000U)
            .WriteInt32(0)
            .WriteInt16(-100)
            .WriteInt16(50)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt16((ushort)glyphCount);
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            builder.WriteUInt16((ushort)(glyphId == 0 ? 0 : 36));
            glyphId += 1;
        }

        return builder.CopyBytes();
    }

    static void AddTable(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] Join(params byte[][] values)
    {
        return values.SelectMany(value => value).ToArray();
    }
}

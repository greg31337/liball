namespace ttf.Tests;

internal class SfntMetadataInspectionTests
{
    [Test]
    public void ParsesMetaMapsAndCopiesTheirBoundedPayloads()
    {
        byte[] bytes = BuildMeta();
        SfntMetaTable? table = SfntMetaTableParser.FindTable(CreateCache(
            FontTags.Meta, bytes, false, FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(1U));
            Assert.That(table.GetFlags(), Is.Zero);
            Assert.That(table.GetDataMapCount(), Is.EqualTo(3));
            Assert.That(table.GetDataMap(0).GetTag(), Is.EqualTo((int)TestSfntEncoding.EncodeTag("dlng")));
            Assert.That(table.GetDataMap(0).GetDataOffset(), Is.EqualTo(52));
            Assert.That(table.GetDataMap(0).GetDataLength(), Is.EqualTo(9));
            Assert.That(table.GetDataMap(0).CopyData(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("Latn,Jpan")));
            Assert.That(table.FindFirstDataMap((int)TestSfntEncoding.EncodeTag("slng"))!.CopyData(),
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("Latn")));
            Assert.That(table.FindFirstDataMap((int)TestSfntEncoding.EncodeTag("zzzz")), Is.Null);
        });
    }

    [Test]
    public void ParsesEveryPcltFieldWithoutNormalizingLegacyBytes()
    {
        SfntPcltTable? table = SfntPcltTableParser.FindTable(CreateCache(
            FontTags.Pclt, BuildPclt(), false, FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetFontNumber(), Is.EqualTo(0x81020304U));
            Assert.That(table.GetPitch(), Is.EqualTo(600));
            Assert.That(table.GetXHeight(), Is.EqualTo(450));
            Assert.That(table.GetStyle(), Is.EqualTo(0x1234));
            Assert.That(table.GetTypeFamily(), Is.EqualTo(0x2345));
            Assert.That(table.GetCapHeight(), Is.EqualTo(700));
            Assert.That(table.GetSymbolSet(), Is.EqualTo(277));
            Assert.That(table.GetTypefaceByte(0), Is.EqualTo(-1));
            Assert.That(table.GetTypefaceByte(15), Is.EqualTo(15));
            Assert.That(table.CopyCharacterComplement(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(table.CopyFileName(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("ABC123")));
            Assert.That(table.GetStrokeWeight(), Is.EqualTo(-3));
            Assert.That(table.GetWidthType(), Is.EqualTo(5));
            Assert.That(table.GetSerifStyle(), Is.EqualTo(7));
        });
    }

    [Test]
    public void ParsesMergClassDefinitionsAndDirectionalMatrix()
    {
        SfntMergTable? table = SfntMergTableParser.FindTable(CreateCache(
            FontTags.Merg, BuildMerg(), true, FontLimits.CreateCompatible()));
        SfntMergClassDefinition first = table!.GetClassDefinition(0);
        SfntMergClassDefinition second = table.GetClassDefinition(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.Zero);
            Assert.That(table.GetMergeClassCount(), Is.EqualTo(3));
            Assert.That(table.GetClassDefinitionCount(), Is.EqualTo(2));
            Assert.That(first.GetFormat(), Is.EqualTo(1));
            Assert.That(first.GetClassForGlyph(1), Is.EqualTo(1));
            Assert.That(first.GetClassForGlyph(2), Is.EqualTo(2));
            Assert.That(second.GetFormat(), Is.EqualTo(2));
            Assert.That(second.GetRange(0).GetStartGlyphId(), Is.EqualTo(3));
            Assert.That(second.GetRange(0).GetEndGlyphId(), Is.EqualTo(4));
            Assert.That(table.GetClassForGlyph(4), Is.EqualTo(1));
            Assert.That(table.GetEntry(0, 1).MergesLeftToRight(), Is.True);
            Assert.That(table.GetEntry(1, 0).MergesRightToLeft(), Is.True);
            Assert.That(table.GetEntry(1, 2).IsSubordinateRightToLeft(), Is.True);
            Assert.That(table.GetEntry(2, 1).GroupsRightToLeft(), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetEntry(3, 0));
        });
    }

    [Test]
    public void PreservesDsigBlocksAndDiagnosesUnverifiedSignatures()
    {
        SfntDsigTable? table = SfntDsigTableParser.FindTable(CreateCache(
            FontTags.Dsig, BuildDsig(), false, FontLimits.CreateCompatible()));
        SfntDsigSignatureRecord pkcs7 = table!.GetSignature(0);
        SfntDsigSignatureRecord unknown = table.GetSignature(1);
        FontDiagnostic diagnostic = table.GetDiagnostic(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1U));
            Assert.That(table.PreventsResigning(), Is.True);
            Assert.That(table.GetSignatureCount(), Is.EqualTo(2));
            Assert.That(table.WereSignaturesVerified(), Is.False);
            Assert.That(pkcs7.IsPkcs7Format(), Is.True);
            Assert.That(pkcs7.GetBlockLength(), Is.EqualTo(12));
            Assert.That(pkcs7.GetBlockOffset(), Is.EqualTo(32));
            Assert.That(pkcs7.CopySignatureData(), Is.EqualTo(new byte[] { 0x30, 0x02, 0x01, 0x00 }));
            Assert.That(unknown.GetFormat(), Is.EqualTo(99U));
            Assert.That(unknown.CopyBlockData(), Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(unknown.CopySignatureData(), Is.Empty);
            Assert.That(table.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(diagnostic.GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetCode(), Is.EqualTo(FontDiagnosticCode.SignatureNotVerified));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Dsig));
            Assert.That(diagnostic.GetLogicalPath(), Is.EqualTo("DSIG.signatures"));
        });
    }

    [TestCaseSource(nameof(InvalidMetaTables))]
    public void RejectsMalformedMetaTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => SfntMetaTableParser.FindTable(CreateCache(
            FontTags.Meta, bytes, false, FontLimits.CreateCompatible())));
    }

    [TestCaseSource(nameof(InvalidPcltTables))]
    public void RejectsMalformedPcltTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => SfntPcltTableParser.FindTable(CreateCache(
            FontTags.Pclt, bytes, false, FontLimits.CreateCompatible())));
    }

    [TestCaseSource(nameof(InvalidMergTables))]
    public void RejectsMalformedMergTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => SfntMergTableParser.FindTable(CreateCache(
            FontTags.Merg, bytes, true, FontLimits.CreateCompatible())));
    }

    [TestCaseSource(nameof(InvalidDsigTables))]
    public void RejectsMalformedDsigTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => SfntDsigTableParser.FindTable(CreateCache(
            FontTags.Dsig, bytes, false, FontLimits.CreateCompatible())));
    }

    [Test]
    public void EnforcesCoverageLimitsForVariableInspectionRecords()
    {
        FontLimits limits = CreateLimits(1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => SfntMetaTableParser.FindTable(CreateCache(
                FontTags.Meta, BuildMeta(), false, limits)));
            Assert.Throws<FontLimitException>(() => SfntDsigTableParser.FindTable(CreateCache(
                FontTags.Dsig, BuildDsig(), false, limits)));
            Assert.Throws<FontLimitException>(() => SfntMergTableParser.FindTable(CreateCache(
                FontTags.Merg, BuildMerg(), true, limits)));
        });
    }

    [Test]
    public void KeepsUnknownVersionsAsRawOnlyViews()
    {
        SfntParsedTableView? meta = SfntMetaTableParser.FindView(CreateCache(
            FontTags.Meta, new TestBinaryBuilder().WriteUInt32(2).CopyBytes(), false, FontLimits.CreateCompatible()));
        SfntParsedTableView? pclt = SfntPcltTableParser.FindView(CreateCache(
            FontTags.Pclt, new TestBinaryBuilder().WriteUInt16(2).WriteUInt16(0).CopyBytes(), false, FontLimits.CreateCompatible()));
        SfntParsedTableView? merg = SfntMergTableParser.FindView(CreateCache(
            FontTags.Merg, new TestBinaryBuilder().WriteUInt16(1).CopyBytes(), true, FontLimits.CreateCompatible()));
        SfntParsedTableView? dsig = SfntDsigTableParser.FindView(CreateCache(
            FontTags.Dsig, new TestBinaryBuilder().WriteUInt32(2).CopyBytes(), false, FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(meta!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(pclt!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(merg!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(dsig!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(meta.HasParsedData(), Is.False);
            Assert.That(pclt.HasParsedData(), Is.False);
            Assert.That(merg.HasParsedData(), Is.False);
            Assert.That(dsig.HasParsedData(), Is.False);
        });
    }

    [Test]
    public void IntegratesAllTablesWithFontFaceAndPreservesRawBytes()
    {
        byte[] metaBytes = BuildMeta();
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Meta, metaBytes),
            CreateTable(FontTags.Pclt, BuildPclt()),
            CreateTable(FontTags.Merg, BuildMerg()),
            CreateTable(FontTags.Dsig, BuildDsig()),
            CreateTable(FontTags.Maxp, BuildMaxp())
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(FontOutlineKind.TrueType, Array.Empty<FontDiagnostic>()),
            cache);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetMetaTableView()!.GetParsedData(), Is.SameAs(face.GetMetaTable()));
            Assert.That(face.GetPcltTableView()!.GetParsedData(), Is.SameAs(face.GetPcltTable()));
            Assert.That(face.GetMergTableView()!.GetParsedData(), Is.SameAs(face.GetMergTable()));
            Assert.That(face.GetDsigTableView()!.GetParsedData(), Is.SameAs(face.GetDsigTable()));
            Assert.That(face.CopyTable(FontTags.Meta), Is.EqualTo(metaBytes));
        });
    }

    [Test]
    public void MissingTablesReturnNoViewsOrModels()
    {
        SfntTableCache cache = CreateCache(0, null, false, FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(SfntMetaTableParser.FindTable(cache), Is.Null);
            Assert.That(SfntPcltTableParser.FindTable(cache), Is.Null);
            Assert.That(SfntMergTableParser.FindTable(cache), Is.Null);
            Assert.That(SfntDsigTableParser.FindTable(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntMetaTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntPcltTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntMergTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntDsigTableParser.FindView(null!));
        });
    }

    static IEnumerable<TestCaseData> InvalidMetaTables()
    {
        byte[] flags = BuildMeta();
        flags[7] = 1;
        yield return InvalidCase(flags, "MetaReservedFlags");

        byte[] badTag = BuildMeta();
        badTag[16] = (byte)'1';
        yield return InvalidCase(badTag, "MetaInvalidTag");

        byte[] embeddedSpace = BuildMeta();
        embeddedSpace[17] = (byte)' ';
        yield return InvalidCase(embeddedSpace, "MetaEmbeddedTagSpace");

        byte[] badOffset = BuildMeta();
        badOffset[23] = 51;
        yield return InvalidCase(badOffset, "MetaPayloadBeforeMaps");

        yield return InvalidCase(BuildMeta().Take(30).ToArray(), "MetaTruncatedMaps");
    }

    static IEnumerable<TestCaseData> InvalidPcltTables()
    {
        byte[] reserved = BuildPclt();
        reserved[53] = 1;
        yield return InvalidCase(reserved, "PcltReservedByte");
        yield return InvalidCase(BuildPclt().SkipLast(1).ToArray(), "PcltWrongLength");
    }

    static IEnumerable<TestCaseData> InvalidMergTables()
    {
        byte[] reservedBehavior = BuildMerg();
        reservedBehavior[34] = 0x80;
        yield return InvalidCase(reservedBehavior, "MergReservedBehavior");

        byte[] duplicateGlyph = BuildMerg();
        duplicateGlyph[28] = 0;
        duplicateGlyph[29] = 2;
        yield return InvalidCase(duplicateGlyph, "MergDuplicateGlyph");

        byte[] invalidGlyph = BuildMerg();
        invalidGlyph[30] = 0;
        invalidGlyph[31] = 5;
        yield return InvalidCase(invalidGlyph, "MergInvalidGlyph");

        byte[] overlappingRegion = BuildMerg();
        overlappingRegion[5] = 16;
        yield return InvalidCase(overlappingRegion, "MergOverlappingMatrix");
    }

    static IEnumerable<TestCaseData> InvalidDsigTables()
    {
        byte[] flags = BuildDsig();
        flags[7] = 2;
        yield return InvalidCase(flags, "DsigReservedFlags");

        byte[] reserved = BuildDsig();
        reserved[33] = 1;
        yield return InvalidCase(reserved, "DsigReservedBlock");

        byte[] length = BuildDsig();
        length[39] = 3;
        yield return InvalidCase(length, "DsigInconsistentLength");

        byte[] overlap = BuildDsig();
        overlap[31] = 40;
        yield return InvalidCase(overlap, "DsigOverlappingBlocks");

        yield return InvalidCase(BuildDsig().Take(20).ToArray(), "DsigTruncatedRecords");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntTableCache CreateCache(
        int tag,
        byte[]? bytes,
        bool includeMaxp,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        if (bytes != null) { tables.Add(CreateTable(tag, bytes)); }
        if (includeMaxp) { tables.Add(CreateTable(FontTags.Maxp, BuildMaxp())); }
        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
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

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder().WriteUInt32(0x00005000U).WriteUInt16(5).CopyBytes();
    }

    static byte[] BuildMeta()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(1)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(3)
            .WriteTag("dlng").WriteUInt32(52).WriteUInt32(9)
            .WriteTag("slng").WriteUInt32(61).WriteUInt32(4)
            .WriteTag("AB12").WriteUInt32(65).WriteUInt32(3)
            .WriteBytes(System.Text.Encoding.ASCII.GetBytes("Latn,Jpan"))
            .WriteBytes(System.Text.Encoding.ASCII.GetBytes("Latn"))
            .WriteBytes(new byte[] { 0, 0xFF, 7 })
            .CopyBytes();
    }

    static byte[] BuildPclt()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x81020304U)
            .WriteUInt16(600).WriteUInt16(450)
            .WriteUInt16(0x1234).WriteUInt16(0x2345)
            .WriteUInt16(700).WriteUInt16(277)
            .WriteInt8(-1);
        int index = 1;
        while (index < 16)
        {
            builder.WriteInt8((sbyte)index);
            index += 1;
        }
        return builder
            .WriteBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            .WriteBytes(System.Text.Encoding.ASCII.GetBytes("ABC123"))
            .WriteInt8(-3).WriteInt8(5).WriteUInt8(7).WriteUInt8(0)
            .CopyBytes();
    }

    static byte[] BuildMerg()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(3).WriteUInt16(34)
            .WriteUInt16(2).WriteUInt16(10)
            .WriteUInt16(14).WriteUInt16(24)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(2).WriteUInt16(1)
            .WriteUInt16(3).WriteUInt16(4).WriteUInt16(1)
            .WriteBytes(new byte[] { 0, 1, 2, 0x10, 0x20, 0x40, 3, 0x30, 0 })
            .CopyBytes();
    }

    static byte[] BuildDsig()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(1).WriteUInt16(2).WriteUInt16(1)
            .WriteUInt32(1).WriteUInt32(12).WriteUInt32(32)
            .WriteUInt32(99).WriteUInt32(3).WriteUInt32(44)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt32(4)
            .WriteBytes(new byte[] { 0x30, 0x02, 0x01, 0x00 })
            .WriteBytes(new byte[] { 9, 8, 7 })
            .CopyBytes();
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        FontLimits defaults = FontLimits.CreateCompatible();
        return FontLimits.Create(
            defaults.GetMaxInputBytes(),
            defaults.GetMaxOutputBytes(),
            defaults.GetMaxFaces(),
            defaults.GetMaxTablesPerFace(),
            defaults.GetMaxGlyphsPerFace(),
            defaults.GetMaxPointsPerGlyph(),
            defaults.GetMaxContoursPerGlyph(),
            defaults.GetMaxComponentDepth(),
            defaults.GetMaxCffSubroutineDepth(),
            defaults.GetMaxLayoutNesting(),
            defaults.GetMaxLookupsPerTable(),
            defaults.GetMaxLayoutOperations(),
            maxCoverageEntries,
            defaults.GetMaxStringBytes(),
            defaults.GetMaxDecompressedBytes(),
            defaults.GetMaxDecompressionRatio(),
            defaults.GetMaxVariationRegions(),
            defaults.GetMaxPaintDepth(),
            defaults.GetMaxDiagnostics());
    }
}

using ttf;

namespace ttf.Tests;

internal class SfntMathTableTests
{
    const int Constants = 10;
    const int GlyphInfo = 232;
    const int Italics = 240;
    const int ItalicsCoverage = 252;
    const int KernInfo = 280;
    const int Variants = 318;
    const int VerticalCoverage = 332;
    const int VerticalConstruction = 344;
    const int Assembly = 356;

    [Test]
    public void ParsesConstantsAndPerGlyphInformation()
    {
        SfntMathTable? table = Parse(BuildMath(), true);
        SfntMathConstants constants = table!.GetConstants();
        SfntMathValue axis = constants.GetValue(
            SfntMathConstant.AxisHeight);
        SfntMathGlyphInfo glyphInfo = table.GetGlyphInfo();
        SfntMathKernRecord? kern = glyphInfo.GetKernInfo()!.FindRecord(4);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(constants.GetScriptPercentScaleDown(), Is.EqualTo(80));
            Assert.That(constants.GetScriptScriptPercentScaleDown(),
                Is.EqualTo(60));
            Assert.That(constants.GetDelimitedSubFormulaMinimumHeight(),
                Is.EqualTo(1500));
            Assert.That(constants.GetDisplayOperatorMinimumHeight(),
                Is.EqualTo(1300));
            Assert.That(constants.GetValueCount(), Is.EqualTo(51));
            Assert.That(constants.GetValue(SfntMathConstant.MathLeading)
                .GetValue(), Is.EqualTo(-25));
            Assert.That(axis.GetValue(), Is.EqualTo(-24));
            Assert.That(axis.HasAdjustment(), Is.True);
            Assert.That(axis.GetDeviceOffset(), Is.EqualTo(214));
            Assert.That(axis.GetDeviceTableOffset(), Is.EqualTo(224));
            Assert.That(axis.GetAdjustment()!.GetDevice()!.GetDelta(10),
                Is.EqualTo(1));
            Assert.That(constants.GetValue(
                SfntMathConstant.RadicalKernAfterDegree).GetValue(),
                Is.EqualTo(25));
            Assert.That(constants.GetRadicalDegreeBottomRaisePercent(),
                Is.EqualTo(60));
            Assert.That(glyphInfo.GetItalicsCorrections()!.FindValue(2)!
                .GetValue(), Is.EqualTo(30));
            Assert.That(glyphInfo.GetItalicsCorrections()!.FindValue(4)!
                .GetValue(), Is.EqualTo(40));
            Assert.That(glyphInfo.GetItalicsCorrections()!.FindValue(3),
                Is.Null);
            Assert.That(glyphInfo.GetTopAccentAttachments()!.FindValue(3)!
                .GetValue(), Is.EqualTo(200));
            Assert.That(glyphInfo.GetExtendedShapeCoverage()!
                .Covers(5), Is.True);
            Assert.That(glyphInfo.IsExtendedShape(5), Is.True);
            Assert.That(glyphInfo.IsExtendedShape(4), Is.False);
            Assert.That(kern!.GetTopRight()!.GetCorrectionHeightCount(),
                Is.EqualTo(1));
            Assert.That(kern.GetTopRight()!.GetCorrectionHeight(0)
                .GetValue(), Is.EqualTo(100));
            Assert.That(kern.GetTopRight()!.GetKernValueCount(),
                Is.EqualTo(2));
            Assert.That(kern.GetTopRight()!.GetKernValue(0).GetValue(),
                Is.EqualTo(-20));
            Assert.That(kern.GetTopRight()!.GetKernValue(1).GetValue(),
                Is.EqualTo(-10));
            Assert.That(kern.GetTopLeft(), Is.Null);
            Assert.That(kern.GetBottomRight(), Is.Null);
            Assert.That(kern.GetBottomLeft()!.GetCorrectionHeightCount(),
                Is.Zero);
            Assert.That(kern.GetBottomLeft()!.GetKernValue(0).GetValue(),
                Is.EqualTo(5));
            Assert.That(glyphInfo.GetKernInfo()!.FindRecord(3), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                constants.GetValue((SfntMathConstant)51));
        });
    }

    [Test]
    public void ParsesVerticalHorizontalVariantsAndAssemblyParts()
    {
        SfntMathVariants variants = Parse(BuildMath(), true)!.GetVariants();
        SfntMathGlyphConstruction? vertical =
            variants.FindVerticalConstruction(6);
        SfntMathGlyphConstruction? horizontal =
            variants.FindHorizontalConstruction(7);
        SfntMathGlyphAssembly? assembly = vertical!.GetAssembly();
        SfntMathGlyphPart first = assembly!.GetPart(0);
        SfntMathGlyphPart second = assembly.GetPart(1);

        Assert.Multiple(() =>
        {
            Assert.That(variants.GetMinimumConnectorOverlap(), Is.EqualTo(20));
            Assert.That(variants.GetVerticalCoverage().Covers(6), Is.True);
            Assert.That(variants.GetHorizontalCoverage().Covers(7), Is.True);
            Assert.That(variants.GetVerticalConstructionCount(), Is.EqualTo(1));
            Assert.That(variants.GetHorizontalConstructionCount(), Is.EqualTo(1));
            Assert.That(vertical.GetVariantCount(), Is.EqualTo(2));
            Assert.That(vertical.GetVariant(0).GetGlyphId(), Is.EqualTo(8));
            Assert.That(vertical.GetVariant(0).GetAdvanceMeasurement(),
                Is.EqualTo(500));
            Assert.That(vertical.GetVariant(1).GetGlyphId(), Is.EqualTo(9));
            Assert.That(vertical.GetVariant(1).GetAdvanceMeasurement(),
                Is.EqualTo(700));
            Assert.That(horizontal!.GetVariant(0).GetGlyphId(), Is.EqualTo(8));
            Assert.That(horizontal.GetVariant(0).GetAdvanceMeasurement(),
                Is.EqualTo(400));
            Assert.That(horizontal.GetAssembly(), Is.Null);
            Assert.That(assembly.GetItalicsCorrection().GetValue(),
                Is.EqualTo(15));
            Assert.That(assembly.GetPartCount(), Is.EqualTo(2));
            Assert.That(first.GetGlyphId(), Is.EqualTo(6));
            Assert.That(first.GetStartConnectorLength(), Is.Zero);
            Assert.That(first.GetEndConnectorLength(), Is.EqualTo(20));
            Assert.That(first.GetFullAdvance(), Is.EqualTo(200));
            Assert.That(first.IsExtender(), Is.False);
            Assert.That(second.GetGlyphId(), Is.EqualTo(7));
            Assert.That(second.GetStartConnectorLength(), Is.EqualTo(20));
            Assert.That(second.GetFullAdvance(), Is.EqualTo(150));
            Assert.That(second.GetFlags(), Is.EqualTo(1));
            Assert.That(second.IsExtender(), Is.True);
            Assert.That(variants.FindVerticalConstruction(7), Is.Null);
            Assert.That(variants.FindHorizontalConstruction(6), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                assembly.GetPart(2));
        });
    }

    [Test]
    public void AllowsOptionalGlyphInformationTablesToBeAbsent()
    {
        byte[] bytes = BuildMath();
        PatchUInt16(bytes, GlyphInfo, 0);
        PatchUInt16(bytes, GlyphInfo + 2, 0);
        PatchUInt16(bytes, GlyphInfo + 4, 0);
        PatchUInt16(bytes, GlyphInfo + 6, 0);
        SfntMathGlyphInfo glyphInfo = Parse(bytes, true)!.GetGlyphInfo();

        Assert.Multiple(() =>
        {
            Assert.That(glyphInfo.GetItalicsCorrections(), Is.Null);
            Assert.That(glyphInfo.GetTopAccentAttachments(), Is.Null);
            Assert.That(glyphInfo.GetExtendedShapeCoverage(), Is.Null);
            Assert.That(glyphInfo.GetKernInfo(), Is.Null);
            Assert.That(glyphInfo.IsExtendedShape(5), Is.False);
        });
    }

    [Test]
    public void AppliesVersionPolicyAndHandlesAbsentTable()
    {
        byte[] future = BuildMath();
        PatchUInt16(future, 2, 1);
        SfntParsedTableView? compatible = SfntMathTableParser.FindView(
            CreateCache(
                future,
                true,
                FontLoadMode.Compatible,
                FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntMathTableParser.FindView(
            CreateCache(
                future,
                true,
                FontLoadMode.Strict,
                FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(compatible!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(((SfntMathTable)compatible.GetParsedData())
                .GetMinorVersion(), Is.EqualTo(1));
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
            Assert.That(SfntMathTableParser.FindView(absent), Is.Null);
            Assert.That(SfntMathTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntMathTableParser.FindView(null!));
        });
    }

    [TestCaseSource(nameof(InvalidMathTables))]
    public void RejectsMalformedMathStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, true));
    }

    [Test]
    public void RequiresMaxpAndEnforcesEntryLimits()
    {
        SfntTableCache missingMaxp = CreateCache(
            BuildMath(),
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache limited = CreateCache(
            BuildMath(),
            true,
            FontLoadMode.Compatible,
            CreateLimits(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntMathTableParser.FindTable(missingMaxp));
            Assert.Throws<FontLimitException>(() =>
                SfntMathTableParser.FindTable(limited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceInspection()
    {
        SfntTableCache cache = CreateCache(
            BuildMath(),
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

        Assert.Multiple(() =>
        {
            Assert.That(face.GetMathTable(), Is.Not.Null);
            Assert.That(face.GetMathTableView()!.GetParsedData(),
                Is.SameAs(face.GetMathTable()));
        });
    }

    static IEnumerable<TestCaseData> InvalidMathTables()
    {
        byte[] nullConstants = BuildMath();
        PatchUInt16(nullConstants, 4, 0);
        yield return InvalidCase(nullConstants, "NullConstantsOffset");

        byte[] deviceInsideConstants = BuildMath();
        PatchUInt16(deviceInsideConstants, Constants + 14, 2);
        yield return InvalidCase(
            deviceInsideConstants,
            "DeviceInsideConstants");

        byte[] mismatchedItalics = BuildMath();
        PatchUInt16(mismatchedItalics, Italics + 2, 1);
        yield return InvalidCase(mismatchedItalics, "MismatchedItalicsCount");

        byte[] invalidCoverageGlyph = BuildMath();
        PatchUInt16(invalidCoverageGlyph, ItalicsCoverage + 4, 10);
        yield return InvalidCase(invalidCoverageGlyph, "InvalidCoverageGlyph");

        byte[] kernInsideHeader = BuildMath();
        PatchUInt16(kernInsideHeader, KernInfo + 4, 4);
        yield return InvalidCase(kernInsideHeader, "KernInsideHeader");

        byte[] mismatchedVerticalCoverage = BuildMath();
        PatchUInt16(mismatchedVerticalCoverage, VerticalCoverage + 2, 0);
        yield return InvalidCase(
            mismatchedVerticalCoverage,
            "MismatchedVerticalCoverage");

        byte[] nullConstruction = BuildMath();
        PatchUInt16(nullConstruction, Variants + 10, 0);
        yield return InvalidCase(nullConstruction, "NullConstructionOffset");

        byte[] invalidVariantGlyph = BuildMath();
        PatchUInt16(invalidVariantGlyph, VerticalConstruction + 4, 10);
        yield return InvalidCase(invalidVariantGlyph, "InvalidVariantGlyph");

        byte[] emptyAssembly = BuildMath();
        PatchUInt16(emptyAssembly, Assembly + 4, 0);
        yield return InvalidCase(emptyAssembly, "EmptyAssembly");

        byte[] reservedPartFlags = BuildMath();
        PatchUInt16(reservedPartFlags, Assembly + 14, 2);
        yield return InvalidCase(reservedPartFlags, "ReservedPartFlags");

        yield return InvalidCase(BuildMath()[..200], "TruncatedConstants");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntMathTable? Parse(byte[] bytes, bool includeMaxp)
    {
        return SfntMathTableParser.FindTable(CreateCache(
            bytes,
            includeMaxp,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? bytes,
        bool includeMaxp,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (bytes != null)
        {
            tables.Add(CreateTable(FontTags.Math, bytes));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(10)
                .CopyBytes()));
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

    internal static byte[] BuildMath()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0);

        int constants = builder.GetLength();
        builder.PatchUInt16(4, (ushort)constants);
        builder.WriteInt16(80)
            .WriteInt16(60)
            .WriteUInt16(1500)
            .WriteUInt16(1300);
        int constantIndex = 0;

        while (constantIndex < 51)
        {
            builder.WriteInt16((short)(constantIndex - 25))
                .WriteUInt16(0);
            constantIndex += 1;
        }

        builder.WriteInt16(60);
        builder.PatchUInt16(constants + 14, 214);
        builder.WriteUInt16(10)
            .WriteUInt16(10)
            .WriteUInt16(3)
            .WriteUInt16(0x0100);

        int glyphInfo = builder.GetLength();
        builder.PatchUInt16(6, (ushort)glyphInfo);
        builder.WriteZeros(8);

        int italics = builder.GetLength();
        builder.PatchUInt16(glyphInfo, (ushort)(italics - glyphInfo));
        builder.WriteUInt16(0)
            .WriteUInt16(2)
            .WriteInt16(30)
            .WriteUInt16(0)
            .WriteInt16(40)
            .WriteUInt16(0);
        int italicsCoverage = WriteCoverage(
            builder,
            new int[] { 2, 4 });
        builder.PatchUInt16(
            italics,
            (ushort)(italicsCoverage - italics));

        int accents = builder.GetLength();
        builder.PatchUInt16(
            glyphInfo + 2,
            (ushort)(accents - glyphInfo));
        builder.WriteUInt16(0)
            .WriteUInt16(1)
            .WriteInt16(200)
            .WriteUInt16(0);
        int accentCoverage = WriteCoverage(builder, new int[] { 3 });
        builder.PatchUInt16(
            accents,
            (ushort)(accentCoverage - accents));

        int extendedCoverage = WriteCoverage(builder, new int[] { 5 });
        builder.PatchUInt16(
            glyphInfo + 4,
            (ushort)(extendedCoverage - glyphInfo));

        int kernInfo = builder.GetLength();
        builder.PatchUInt16(
            glyphInfo + 6,
            (ushort)(kernInfo - glyphInfo));
        builder.WriteUInt16(0)
            .WriteUInt16(1)
            .WriteZeros(8);
        int kernCoverage = WriteCoverage(builder, new int[] { 4 });
        builder.PatchUInt16(
            kernInfo,
            (ushort)(kernCoverage - kernInfo));
        int topRightKern = builder.GetLength();
        builder.PatchUInt16(
            kernInfo + 4,
            (ushort)(topRightKern - kernInfo));
        builder.WriteUInt16(1)
            .WriteInt16(100)
            .WriteUInt16(0)
            .WriteInt16(-20)
            .WriteUInt16(0)
            .WriteInt16(-10)
            .WriteUInt16(0);
        int bottomLeftKern = builder.GetLength();
        builder.PatchUInt16(
            kernInfo + 10,
            (ushort)(bottomLeftKern - kernInfo));
        builder.WriteUInt16(0)
            .WriteInt16(5)
            .WriteUInt16(0);

        int variants = builder.GetLength();
        builder.PatchUInt16(8, (ushort)variants);
        builder.WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int verticalCoverage = WriteCoverage(builder, new int[] { 6 });
        builder.PatchUInt16(
            variants + 2,
            (ushort)(verticalCoverage - variants));
        int horizontalCoverage = WriteCoverage(builder, new int[] { 7 });
        builder.PatchUInt16(
            variants + 4,
            (ushort)(horizontalCoverage - variants));

        int verticalConstruction = builder.GetLength();
        builder.PatchUInt16(
            variants + 10,
            (ushort)(verticalConstruction - variants));
        builder.WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(8)
            .WriteUInt16(500)
            .WriteUInt16(9)
            .WriteUInt16(700);
        int assembly = builder.GetLength();
        builder.PatchUInt16(
            verticalConstruction,
            (ushort)(assembly - verticalConstruction));
        builder.WriteInt16(15)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(6)
            .WriteUInt16(0)
            .WriteUInt16(20)
            .WriteUInt16(200)
            .WriteUInt16(0)
            .WriteUInt16(7)
            .WriteUInt16(20)
            .WriteUInt16(0)
            .WriteUInt16(150)
            .WriteUInt16(1);

        int horizontalConstruction = builder.GetLength();
        builder.PatchUInt16(
            variants + 12,
            (ushort)(horizontalConstruction - variants));
        builder.WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(8)
            .WriteUInt16(400);
        return builder.CopyBytes();
    }

    static int WriteCoverage(TestBinaryBuilder builder, int[] glyphIds)
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

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static FontLimits CreateLimits(int maximumEntries)
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
            maximumEntries,
            defaults.GetMaxStringBytes(),
            defaults.GetMaxDecompressedBytes(),
            defaults.GetMaxDecompressionRatio(),
            defaults.GetMaxVariationRegions(),
            defaults.GetMaxPaintDepth(),
            defaults.GetMaxDiagnostics());
    }
}

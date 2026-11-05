using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class SfntColrV1TableTests
{
    [Test]
    public void ParsesBaseGlyphLayerAndClipLists()
    {
        SfntColrTable? table = Parse(BuildAllPaintsColr(), 40, BuildOneAxisFvar(), BuildName());
        SfntColrClipList? clips = table!.GetClipList();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetBaseGlyphRecordCount(), Is.Zero);
            Assert.That(table.GetLayerRecordCount(), Is.Zero);
            Assert.That(table.GetBaseGlyphPaintRecordCount(), Is.EqualTo(32));
            Assert.That(table.GetBaseGlyphPaintRecord(0).GetGlyphId(), Is.Zero);
            Assert.That(table.FindBaseGlyphPaintRecord(31),
                Is.SameAs(table.GetBaseGlyphPaintRecord(31)));
            Assert.That(table.FindBaseGlyphPaintRecord(39), Is.Null);
            Assert.That(table.GetLayerList()!.GetPaintCount(), Is.EqualTo(1));
            Assert.That(table.GetLayerList()!.GetPaint(0).IsSupported(), Is.False);
            Assert.That(clips!.GetFormat(), Is.EqualTo(1));
            Assert.That(clips.GetRecordCount(), Is.EqualTo(2));
            Assert.That(clips.FindClipBox(0), Is.SameAs(clips.GetRecord(0).GetClipBox()));
            Assert.That(clips.FindClipBox(2), Is.Null);
            Assert.That(clips.FindClipBox(4), Is.SameAs(clips.GetRecord(1).GetClipBox()));
            Assert.That(clips.GetRecord(0).GetClipBox().GetCoordinate(0), Is.EqualTo(-10));
            Assert.That(clips.GetRecord(0).GetClipBox().GetCoordinate(3), Is.EqualTo(200));
            Assert.That(clips.GetRecord(1).GetClipBox().IsVariable(), Is.True);
        });
    }

    [Test]
    public void ParsesVersionZeroCompatibilityRecordsInVersionOneTable()
    {
        SfntColrTable? table = Parse(
            BuildCompatibilityColr(),
            10,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetBaseGlyphRecordCount(), Is.EqualTo(1));
            Assert.That(table.GetBaseGlyphRecord(0).GetGlyphId(), Is.EqualTo(2));
            Assert.That(table.GetBaseGlyphRecord(0).GetLayer(0).GetGlyphId(), Is.EqualTo(3));
            Assert.That(table.GetBaseGlyphPaintRecordCount(), Is.EqualTo(1));
            Assert.That(table.GetBaseGlyphPaintRecord(0).GetGlyphId(), Is.EqualTo(4));
            Assert.That(table.GetBaseGlyphPaintRecord(0).GetPaint().GetFormat(), Is.EqualTo(2));
            Assert.That(table.HasVariationData(), Is.False);
        });
    }

    [Test]
    public void ParsesEveryDefinedPaintFormatAndUnknownPaints()
    {
        SfntColrTable? table = Parse(BuildAllPaintsColr(), 40, BuildOneAxisFvar(), BuildName());
        int index = 0;

        while (index < 32)
        {
            SfntColrPaint paint = table!.GetBaseGlyphPaintRecord(index).GetPaint();
            Assert.That(paint.GetFormat(), Is.EqualTo(index + 1));
            Assert.That(paint.IsSupported(), Is.True);
            Assert.That(table.FindPaintByOffset(paint.GetOffset()), Is.SameAs(paint));
            index += 1;
        }

        SfntColrPaint layers = table!.GetBaseGlyphPaintRecord(0).GetPaint();
        SfntColrPaint solid = table.GetBaseGlyphPaintRecord(1).GetPaint();
        SfntColrPaint linear = table.GetBaseGlyphPaintRecord(3).GetPaint();
        SfntColrPaint glyph = table.GetBaseGlyphPaintRecord(9).GetPaint();
        SfntColrPaint colrGlyph = table.GetBaseGlyphPaintRecord(10).GetPaint();
        SfntColrPaint transform = table.GetBaseGlyphPaintRecord(11).GetPaint();
        SfntColrPaint composite = table.GetBaseGlyphPaintRecord(31).GetPaint();

        Assert.Multiple(() =>
        {
            Assert.That(layers.GetFirstLayerIndex(), Is.Zero);
            Assert.That(layers.GetLayerCount(), Is.EqualTo(1));
            Assert.That(solid.GetPaletteEntryIndex(), Is.EqualTo(1));
            Assert.That(solid.GetValue(0), Is.EqualTo(1));
            Assert.That(linear.GetColorLine()!.GetStopCount(), Is.EqualTo(2));
            Assert.That(linear.GetColorLine()!.GetExtendRaw(), Is.EqualTo(99));
            Assert.That(linear.GetColorLine()!.GetExtendMode(), Is.EqualTo(SfntColrExtendMode.Pad));
            Assert.That(linear.GetColorLine()!.GetStop(0).GetPaletteEntryIndex(), Is.Zero);
            Assert.That(linear.GetColorLine()!.GetStop(1).UsesForegroundColor(), Is.True);
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(35));
            Assert.That(glyph.GetChildPaintOffsetCount(), Is.EqualTo(1));
            Assert.That(colrGlyph.GetGlyphId(), Is.Zero);
            Assert.That(transform.GetAffineTransform()!.GetValue(0), Is.EqualTo(1));
            Assert.That(transform.GetAffineTransform()!.GetValue(4), Is.EqualTo(10));
            Assert.That(composite.GetCompositeModeRaw(), Is.EqualTo(99));
            Assert.That(composite.GetCompositeMode(), Is.EqualTo(SfntColrCompositeMode.Clear));
            Assert.That(composite.GetChildPaintOffsetCount(), Is.EqualTo(2));
            Assert.That(table.FindPaintByOffset(glyph.GetChildPaintOffset(0))!.IsSupported(), Is.False);
        });
    }

    [Test]
    public void ParsesAllReducedTransformFamiliesAndVariableMetadata()
    {
        SfntColrTable? table = Parse(BuildAllPaintsColr(), 40, BuildOneAxisFvar(), BuildName());
        int[] expectedValueCounts = new int[]
        {
            2, 2, 2, 2, 4, 4, 1, 1, 3, 3, 1, 1, 3, 3, 2, 2
        };
        int format = 14;

        while (format <= 29)
        {
            SfntColrPaint paint = table!.GetBaseGlyphPaintRecord(format - 1).GetPaint();
            Assert.That(paint.GetValueCount(), Is.EqualTo(expectedValueCounts[format - 14]));
            Assert.That(paint.IsVariable(), Is.EqualTo((format & 1) != 0));
            Assert.That(paint.GetChildPaintOffsetCount(), Is.EqualTo(1));
            format += 1;
        }

        SfntColrPaint centeredSkew = table!.GetBaseGlyphPaintRecord(29).GetPaint();
        SfntColrPaint variableCenteredSkew = table.GetBaseGlyphPaintRecord(30).GetPaint();
        Assert.Multiple(() =>
        {
            Assert.That(centeredSkew.GetValueCount(), Is.EqualTo(4));
            Assert.That(variableCenteredSkew.GetValueCount(), Is.EqualTo(4));
            Assert.That(variableCenteredSkew.GetVariationIndexBase(), Is.EqualTo(uint.MaxValue));
            Assert.That(table.HasVariationData(), Is.True);
        });
    }

    [Test]
    public void EvaluatesMappedVariationValuesUsingRawFixedPointUnits()
    {
        SfntColrTable? table = Parse(
            BuildVariableSolidColr(),
            5,
            BuildOneAxisFvar(),
            BuildName());
        SfntColrPaint paint = table!.GetBaseGlyphPaintRecord(0).GetPaint();
        SfntVariationLocation defaultLocation = table.CreateVariationLocation(
            new double[] { 0 });
        SfntVariationLocation peakLocation = table.CreateVariationLocation(
            new double[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(paint.GetFormat(), Is.EqualTo(3));
            Assert.That(paint.GetVariationIndexBase(), Is.Zero);
            Assert.That(table.GetVariationIndexMap(), Is.Not.Null);
            Assert.That(table.GetItemVariationStore(), Is.Not.Null);
            Assert.That(paint.GetValue(0), Is.EqualTo(0.5));
            Assert.That(paint.GetValueAtLocation(0, defaultLocation),
                Is.EqualTo(0.5));
            Assert.That(paint.GetValueAtLocation(0, peakLocation),
                Is.EqualTo(0.75));
        });
    }

    [Test]
    public void AppliesFwordAndFixedVariationDeltasInTheirEncodedUnits()
    {
        SfntColrTable? table = Parse(
            BuildVariableTransformsColr(),
            5,
            BuildOneAxisFvar(),
            BuildName());
        SfntVariationLocation location = table!.CreateVariationLocation(
            new double[] { 1 });
        SfntColrPaint translate = table.GetBaseGlyphPaintRecord(0).GetPaint();
        SfntColrPaint transform = table.GetBaseGlyphPaintRecord(1).GetPaint();

        Assert.Multiple(() =>
        {
            Assert.That(translate.GetFormat(), Is.EqualTo(15));
            Assert.That(translate.GetValueAtLocation(0, location),
                Is.EqualTo(15));
            Assert.That(translate.GetValueAtLocation(1, location),
                Is.EqualTo(17));
            Assert.That(transform.GetFormat(), Is.EqualTo(13));
            Assert.That(transform.GetAffineTransform()!
                .GetValueAtLocation(0, location), Is.EqualTo(2));
            Assert.That(transform.GetAffineTransform()!
                .GetValueAtLocation(3, location), Is.EqualTo(1));
        });
    }

    [Test]
    public void UsesBaseValuesForNoVariationSentinel()
    {
        SfntColrTable? table = Parse(BuildAllPaintsColr(), 40, BuildOneAxisFvar(), BuildName());
        SfntVariationLocation location = table!.CreateVariationLocation(new double[] { 0 });
        SfntColrPaint variableSolid = table.GetBaseGlyphPaintRecord(2).GetPaint();
        SfntColrColorStop stop = table.GetBaseGlyphPaintRecord(4)
            .GetPaint().GetColorLine()!.GetStop(0);
        SfntColrClipBox clip = table.GetClipList()!.GetRecord(1).GetClipBox();

        Assert.Multiple(() =>
        {
            Assert.That(variableSolid.GetVariationIndexBase(), Is.EqualTo(uint.MaxValue));
            Assert.That(variableSolid.GetValueAtLocation(0, location),
                Is.EqualTo(0.5));
            Assert.That(stop.GetStopOffsetAtLocation(location), Is.Zero);
            Assert.That(stop.GetAlphaAtLocation(location), Is.EqualTo(1));
            Assert.That(clip.GetCoordinateAtLocation(2, location),
                Is.EqualTo(50));
        });
    }

    [Test]
    public void ExposesStronglyTypedPublicPaintsClipsAndGlyphClosure()
    {
        FontFace face = CreateFace(
            BuildAllPaintsColr(),
            40,
            BuildOneAxisFvar(),
            BuildName());
        FontInstance instance = face.CreateDefaultInstance();
        ColorGlyphResolver resolver = new ColorGlyphResolver();
        ColorPaintGraph layers = resolver.ResolveGlyph(instance, 0).GetPaintGraph()!;
        ColorPaintGraph linearGraph = resolver.ResolveGlyph(instance, 3).GetPaintGraph()!;
        ColorPaint linear = linearGraph.GetRoot();
        ColorPaint radial = resolver.ResolveGlyph(instance, 5).GetPaintGraph()!.GetRoot();
        ColorPaint sweep = resolver.ResolveGlyph(instance, 7).GetPaintGraph()!.GetRoot();
        ColorPaint glyph = resolver.ResolveGlyph(instance, 9).GetPaintGraph()!.GetRoot();
        ColorPaint colrGlyph = resolver.ResolveGlyph(instance, 10).GetPaintGraph()!.GetRoot();
        ColorPaint transform = resolver.ResolveGlyph(instance, 13).GetPaintGraph()!.GetRoot();
        ColorPaint composite = resolver.ResolveGlyph(instance, 31).GetPaintGraph()!.GetRoot();
        ColorGlyphClipBox clip = layers.GetClipBox()!;
        ColorGlyphClipBox variableClip = linearGraph.GetClipBox()!;

        Assert.Multiple(() =>
        {
            Assert.That(layers.GetRoot().GetFormat(),
                Is.EqualTo(ColorPaintFormat.LayerList));
            Assert.That(layers.GetRoot().GetLayerCount(), Is.EqualTo(1));
            Assert.That(layers.GetRoot().GetLayer(0).GetKind(),
                Is.EqualTo(ColorPaintKind.Unsupported));
            Assert.That(clip.GetSourceFormat(), Is.EqualTo(1));
            Assert.That(clip.IsSupported(), Is.True);
            Assert.That(clip.IsVariable(), Is.False);
            Assert.That(clip.GetXMinimum(), Is.EqualTo(-10));
            Assert.That(clip.GetYMinimum(), Is.EqualTo(-20));
            Assert.That(clip.GetXMaximum(), Is.EqualTo(100));
            Assert.That(clip.GetYMaximum(), Is.EqualTo(200));
            Assert.That(variableClip.IsVariable(), Is.True);
            Assert.That(variableClip.GetXMaximum(), Is.EqualTo(50));
            Assert.That(layers.GetVariationCoordinates(),
                Is.SameAs(instance.GetCoordinates()));
            Assert.That(linear.GetFormat(),
                Is.EqualTo(ColorPaintFormat.LinearGradient));
            Assert.That(linear.GetLinearGradient().GetX0(), Is.EqualTo(1));
            Assert.That(linear.GetLinearGradient().GetY2(), Is.EqualTo(6));
            Assert.That(linear.GetGradientExtendMode(),
                Is.EqualTo(ColorGradientExtendMode.Pad));
            Assert.That(linear.GetStopCount(), Is.EqualTo(2));
            Assert.That(radial.GetRadialGradient().GetStartRadius(), Is.EqualTo(22));
            Assert.That(radial.GetRadialGradient().GetEndRadius(), Is.EqualTo(25));
            Assert.That(sweep.GetSweepGradient().GetCenterX(), Is.EqualTo(1));
            Assert.That(sweep.GetSweepGradient().GetEndAngle(),
                Is.EqualTo(4.0 / 16384));
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(35));
            Assert.That(glyph.GetGlyphPaint().GetKind(),
                Is.EqualTo(ColorPaintKind.Unsupported));
            Assert.That(resolver.ResolveGlyph(instance, 9).GetPaintGraph()!
                .GetReferencedGlyphId(0), Is.EqualTo(35));
            Assert.That(colrGlyph.GetGlyphId(), Is.Zero);
            Assert.That(colrGlyph.GetReferencedPaint().GetKind(),
                Is.EqualTo(ColorPaintKind.LayerList));
            Assert.That(transform.GetFormat(),
                Is.EqualTo(ColorPaintFormat.Translate));
            Assert.That(transform.GetTransform().GetXX(), Is.EqualTo(1));
            Assert.That(transform.GetTransform().GetYY(), Is.EqualTo(1));
            Assert.That(transform.GetTransform().GetDX(), Is.EqualTo(1));
            Assert.That(transform.GetTransform().GetDY(), Is.EqualTo(2));
            Assert.That(transform.GetTransformedPaint().GetKind(),
                Is.EqualTo(ColorPaintKind.Unsupported));
            Assert.That(composite.GetCompositeOperation(),
                Is.EqualTo(ColorCompositeMode.Clear));
            Assert.That(composite.GetCompositeSource().GetKind(),
                Is.EqualTo(ColorPaintKind.Unsupported));
            Assert.That(composite.GetCompositeBackdrop().GetKind(),
                Is.EqualTo(ColorPaintKind.Unsupported));
        });
    }

    [Test]
    public void ExposesValuesResolvedAtTheFontInstanceLocation()
    {
        FontFace face = CreateFace(
            BuildVariableSolidColr(),
            5,
            BuildOneAxisFvar(),
            BuildName());
        FontInstance defaultInstance = face.CreateDefaultInstance();
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 1);
        FontInstance peakInstance = face.CreateInstance(coordinates);
        ColorGlyphResolver resolver = new ColorGlyphResolver();
        ColorPaintGraph defaultGraph = resolver.ResolveGlyph(
            defaultInstance,
            1).GetPaintGraph()!;
        ColorPaintGraph peakGraph = resolver.ResolveGlyph(
            peakInstance,
            1).GetPaintGraph()!;

        Assert.Multiple(() =>
        {
            Assert.That(defaultGraph.GetRoot().GetFormat(),
                Is.EqualTo(ColorPaintFormat.VariableSolid));
            Assert.That(defaultGraph.GetRoot().GetAlpha(), Is.EqualTo(0.5));
            Assert.That(peakGraph.GetRoot().GetAlpha(), Is.EqualTo(0.75));
            Assert.That(defaultGraph.GetVariationCoordinates(),
                Is.SameAs(defaultInstance.GetCoordinates()));
            Assert.That(peakGraph.GetVariationCoordinates(),
                Is.SameAs(peakInstance.GetCoordinates()));
            Assert.That(peakGraph.GetVariationCoordinates().GetNormalizedValue(0),
                Is.EqualTo(1));
        });
    }

    [TestCaseSource(nameof(InvalidVersionOneTables))]
    public void RejectsMalformedVersionOneStructures(byte[] colr)
    {
        Assert.Throws<FontFormatException>(() =>
            Parse(colr, 40, BuildOneAxisFvar(), BuildName()));
    }

    [Test]
    public void RequiresVariationAndColorDependenciesAndEnforcesLimits()
    {
        byte[] bytes = BuildAllPaintsColr();
        SfntTableCache missingFvar = CreateCache(
            bytes, BuildCpal(), BuildMaxp(40), null, BuildName(),
            FontLimits.CreateCompatible());
        SfntTableCache missingCpal = CreateCache(
            bytes, null, BuildMaxp(40), BuildOneAxisFvar(), BuildName(),
            FontLimits.CreateCompatible());
        SfntTableCache limited = CreateCache(
            bytes, BuildCpal(), BuildMaxp(40), BuildOneAxisFvar(), BuildName(),
            CreateLimits(20));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntColrTableParser.FindTable(missingFvar));
            Assert.Throws<FontFormatException>(() => SfntColrTableParser.FindTable(missingCpal));
            Assert.Throws<FontLimitException>(() => SfntColrTableParser.FindTable(limited));
        });
    }

    [Test]
    public void ValidatesVersionOneAccessorIndexes()
    {
        SfntColrTable? table = Parse(BuildAllPaintsColr(), 40, BuildOneAxisFvar(), BuildName());
        SfntColrPaint paint = table!.GetBaseGlyphPaintRecord(3).GetPaint();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetBaseGlyphPaintRecord(32));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetPaint(table.GetPaintCount()));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetLayerList()!.GetPaint(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetClipList()!.GetRecord(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => paint.GetValue(6));
            Assert.Throws<ArgumentOutOfRangeException>(() => paint.GetColorLine()!.GetStop(2));
        });
    }

    static IEnumerable<TestCaseData> InvalidVersionOneTables()
    {
        byte[] valid = BuildAllPaintsColr();
        yield return InvalidCase(valid[..33], "TruncatedHeader");

        byte[] missingBaseList = BuildAllPaintsColr();
        PatchUInt32(missingBaseList, 14, 0);
        yield return InvalidCase(missingBaseList, "MissingBaseGlyphList");

        byte[] unsortedBaseList = BuildAllPaintsColr();
        PatchUInt16(unsortedBaseList, 44, 0);
        yield return InvalidCase(unsortedBaseList, "UnsortedBaseGlyphList");

        byte[] missingPaletteEntry = BuildAllPaintsColr();
        PatchUInt16(missingPaletteEntry, GetRootOffset(missingPaletteEntry, 1) + 1, 3);
        yield return InvalidCase(missingPaletteEntry, "MissingPaletteEntry");

        byte[] missingPaintGlyph = BuildAllPaintsColr();
        PatchUInt16(missingPaintGlyph, GetRootOffset(missingPaintGlyph, 9) + 4, 40);
        yield return InvalidCase(missingPaintGlyph, "MissingPaintGlyph");

        byte[] missingColrGlyph = BuildAllPaintsColr();
        PatchUInt16(missingColrGlyph, GetRootOffset(missingColrGlyph, 10) + 1, 39);
        yield return InvalidCase(missingColrGlyph, "MissingColrGlyph");

        byte[] missingLayerSlice = BuildAllPaintsColr();
        PatchUInt32(missingLayerSlice, GetRootOffset(missingLayerSlice, 0) + 2, 1);
        yield return InvalidCase(missingLayerSlice, "MissingLayerSlice");

        byte[] zeroChildOffset = BuildAllPaintsColr();
        PatchUInt24(zeroChildOffset, GetRootOffset(zeroChildOffset, 9) + 1, 0);
        yield return InvalidCase(zeroChildOffset, "ZeroChildOffset");

        byte[] overlappingClips = BuildAllPaintsColr();
        int clipOffset = ReadUInt32(overlappingClips, 22);
        PatchUInt16(overlappingClips, clipOffset + 12, 1);
        yield return InvalidCase(overlappingClips, "OverlappingClips");

        byte[] invertedClip = BuildAllPaintsColr();
        clipOffset = ReadUInt32(invertedClip, 22);
        PatchUInt16(invertedClip, clipOffset + 20, 300);
        yield return InvalidCase(invertedClip, "InvertedClipBox");

        byte[] missingStore = BuildAllPaintsColr();
        PatchUInt32(missingStore, 30, 0);
        yield return InvalidCase(missingStore, "VariablePaintWithoutStore");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntColrTable? Parse(
        byte[] colr,
        int glyphCount,
        byte[]? fvar,
        byte[]? name)
    {
        return SfntColrTableParser.FindTable(CreateCache(
            colr,
            BuildCpal(),
            BuildMaxp(glyphCount),
            fvar,
            name,
            FontLimits.CreateCompatible()));
    }

    static FontFace CreateFace(
        byte[] colr,
        int glyphCount,
        byte[]? fvar,
        byte[]? name)
    {
        SfntTableCache cache = CreateCache(
            colr,
            BuildCpal(),
            BuildMaxp(glyphCount),
            fvar,
            name,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableCache CreateCache(
        byte[] colr,
        byte[]? cpal,
        byte[] maxp,
        byte[]? fvar,
        byte[]? name,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Colr, colr);
        AddTable(tables, FontTags.Cpal, cpal);
        AddTable(tables, FontTags.Maxp, maxp);
        AddTable(tables, FontTags.Fvar, fvar);
        AddTable(tables, FontTags.Name, name);
        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
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

    static byte[] BuildAllPaintsColr()
    {
        TestBinaryBuilder builder = WriteVersionOneHeader();
        int baseListOffset = builder.GetLength();
        builder.WriteUInt32(32);
        int[] recordOffsetPositions = new int[32];
        int index = 0;

        while (index < 32)
        {
            builder.WriteUInt16((ushort)index);
            recordOffsetPositions[index] = builder.GetLength();
            builder.WriteUInt32(0);
            index += 1;
        }

        index = 0;

        while (index < 32)
        {
            int paintOffset = builder.GetLength();
            builder.PatchUInt32(
                recordOffsetPositions[index],
                (uint)(paintOffset - baseListOffset));
            WritePaint(builder, index + 1);
            index += 1;
        }

        int layerListOffset = builder.GetLength();
        builder.PatchUInt32(18, (uint)layerListOffset);
        builder.WriteUInt32(1).WriteUInt32(8).WriteUInt8(250);

        int clipListOffset = builder.GetLength();
        builder.PatchUInt32(22, (uint)clipListOffset);
        builder
            .WriteUInt8(1)
            .WriteUInt32(2)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt24(19)
            .WriteUInt16(3).WriteUInt16(4).WriteUInt24(28)
            .WriteUInt8(1)
            .WriteInt16(-10).WriteInt16(-20).WriteInt16(100).WriteInt16(200)
            .WriteUInt8(2)
            .WriteInt16(0).WriteInt16(0).WriteInt16(50).WriteInt16(60)
            .WriteUInt32(uint.MaxValue);

        int storeOffset = builder.GetLength();
        builder.PatchUInt32(30, (uint)storeOffset);
        WriteEmptyVariationStore(builder);
        return builder.CopyBytes();
    }

    static byte[] BuildVariableSolidColr()
    {
        TestBinaryBuilder builder = WriteVersionOneHeader();
        builder
            .WriteUInt32(1)
            .WriteUInt16(1)
            .WriteUInt32(10)
            .WriteUInt8(3)
            .WriteUInt16(0)
            .WriteInt16(8192)
            .WriteUInt32(0);
        int mapOffset = builder.GetLength();
        builder.PatchUInt32(26, (uint)mapOffset);
        builder
            .WriteUInt8(0)
            .WriteUInt8(0)
            .WriteUInt16(1)
            .WriteUInt8(0);
        int storeOffset = builder.GetLength();
        builder.PatchUInt32(30, (uint)storeOffset);
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(16384)
            .WriteInt16(16384)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16(4096);
        return builder.CopyBytes();
    }

    static byte[] BuildCompatibilityColr()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt32(34)
            .WriteUInt32(40)
            .WriteUInt16(1)
            .WriteUInt32(44)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt16(2).WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(3).WriteUInt16(1)
            .WriteUInt32(1)
            .WriteUInt16(4).WriteUInt32(10)
            .WriteUInt8(2).WriteUInt16(0).WriteInt16(16384)
            .CopyBytes();
    }

    static byte[] BuildVariableTransformsColr()
    {
        TestBinaryBuilder builder = WriteVersionOneHeader();
        int baseListOffset = builder.GetLength();
        builder
            .WriteUInt32(2)
            .WriteUInt16(1).WriteUInt32(16)
            .WriteUInt16(2).WriteUInt32(29)
            .WriteUInt8(15).WriteUInt24(12)
            .WriteInt16(10).WriteInt16(20).WriteUInt32(0)
            .WriteUInt8(245)
            .WriteUInt8(13).WriteUInt24(7).WriteUInt24(8)
            .WriteUInt8(246)
            .WriteInt32(65536).WriteInt32(0).WriteInt32(0)
            .WriteInt32(65536).WriteInt32(0).WriteInt32(0)
            .WriteUInt32(2);
        Assert.That(baseListOffset, Is.EqualTo(34));
        int mapOffset = builder.GetLength();
        builder.PatchUInt32(26, (uint)mapOffset);
        builder.WriteUInt8(0).WriteUInt8(3).WriteUInt16(8);
        int index = 0;

        while (index < 8)
        {
            builder.WriteUInt8((byte)index);
            index += 1;
        }

        int storeOffset = builder.GetLength();
        builder.PatchUInt32(30, (uint)storeOffset);
        builder
            .WriteUInt16(1).WriteUInt32(12).WriteUInt16(1).WriteUInt32(22)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteInt16(0).WriteInt16(16384).WriteInt16(16384)
            .WriteUInt16(8).WriteUInt16(0x8001).WriteUInt16(1).WriteUInt16(0)
            .WriteInt32(5).WriteInt32(-3).WriteInt32(65536)
            .WriteInt32(0).WriteInt32(0).WriteInt32(0).WriteInt32(0).WriteInt32(0);
        return builder.CopyBytes();
    }

    static TestBinaryBuilder WriteVersionOneHeader()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt16(0)
            .WriteUInt32(34)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0);
    }

    static void WritePaint(TestBinaryBuilder builder, int format)
    {
        if (format == 1)
        {
            builder.WriteUInt8(1).WriteUInt8(1).WriteUInt32(0);
            return;
        }

        if (format == 2 || format == 3)
        {
            builder.WriteUInt8((byte)format).WriteUInt16(1).WriteInt16(
                format == 2 ? (short)20000 : (short)8192);

            if (format == 3)
            {
                builder.WriteUInt32(uint.MaxValue);
            }

            return;
        }

        if (format >= 4 && format <= 9)
        {
            WriteGradientPaint(builder, format);
            return;
        }

        if (format == 10)
        {
            builder.WriteUInt8(10).WriteUInt24(6).WriteUInt16(35).WriteUInt8(240);
            return;
        }

        if (format == 11)
        {
            builder.WriteUInt8(11).WriteUInt16(0);
            return;
        }

        if (format == 12 || format == 13)
        {
            bool variable = format == 13;
            builder
                .WriteUInt8((byte)format)
                .WriteUInt24(7)
                .WriteUInt24(8)
                .WriteUInt8(241)
                .WriteInt32(65536)
                .WriteInt32(0)
                .WriteInt32(0)
                .WriteInt32(65536)
                .WriteInt32(10 * 65536)
                .WriteInt32(20 * 65536);

            if (variable)
            {
                builder.WriteUInt32(uint.MaxValue);
            }

            return;
        }

        if (format >= 14 && format <= 31)
        {
            WriteReducedTransform(builder, format);
            return;
        }

        builder
            .WriteUInt8(32)
            .WriteUInt24(8)
            .WriteUInt8(99)
            .WriteUInt24(9)
            .WriteUInt8(242)
            .WriteUInt8(243);
    }

    static void WriteGradientPaint(TestBinaryBuilder builder, int format)
    {
        bool variable = (format & 1) != 0;
        int valueCount = format <= 7 ? 6 : 4;
        int fixedLength = 4 + valueCount * 2 + (variable ? 4 : 0);
        builder.WriteUInt8((byte)format).WriteUInt24((uint)fixedLength);
        int index = 0;

        while (index < valueCount)
        {
            if ((format == 6 || format == 7) && (index == 2 || index == 5))
            {
                builder.WriteUInt16((ushort)(20 + index));
            }
            else
            {
                builder.WriteInt16((short)(index + 1));
            }

            index += 1;
        }

        if (variable)
        {
            builder.WriteUInt32(uint.MaxValue);
        }

        builder.WriteUInt8(99).WriteUInt16(2);
        WriteColorStop(builder, variable, 0, 0, 16384);
        WriteColorStop(builder, variable, 16384, 0xFFFF, -100);
    }

    static void WriteColorStop(
        TestBinaryBuilder builder,
        bool variable,
        int stopOffset,
        int paletteEntry,
        int alpha)
    {
        builder
            .WriteInt16((short)stopOffset)
            .WriteUInt16((ushort)paletteEntry)
            .WriteInt16((short)alpha);

        if (variable)
        {
            builder.WriteUInt32(uint.MaxValue);
        }
    }

    static void WriteReducedTransform(TestBinaryBuilder builder, int format)
    {
        bool variable = (format & 1) != 0;
        int valueCount;

        if (format == 14 || format == 15 || format == 16 || format == 17 ||
            format == 28 || format == 29)
        {
            valueCount = 2;
        }
        else if (format == 18 || format == 19 || format == 30 || format == 31)
        {
            valueCount = 4;
        }
        else if (format == 20 || format == 21 || format == 24 || format == 25)
        {
            valueCount = 1;
        }
        else
        {
            valueCount = 3;
        }

        int fixedLength = 4 + valueCount * 2 + (variable ? 4 : 0);
        builder.WriteUInt8((byte)format).WriteUInt24((uint)fixedLength);
        int index = 0;

        while (index < valueCount)
        {
            builder.WriteInt16((short)(index + 1));
            index += 1;
        }

        if (variable)
        {
            builder.WriteUInt32(uint.MaxValue);
        }

        builder.WriteUInt8(244);
    }

    static void WriteEmptyVariationStore(TestBinaryBuilder builder)
    {
        builder
            .WriteUInt16(1)
            .WriteUInt32(8)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0);
    }

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt32(14)
            .WriteUInt16(0)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(4).WriteUInt8(5).WriteUInt8(6).WriteUInt8(255)
            .WriteUInt8(7).WriteUInt8(8).WriteUInt8(9).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildOneAxisFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(16).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(20)
            .WriteUInt16(0).WriteUInt16(8)
            .WriteTag("wght")
            .WriteInt32(0).WriteInt32(0).WriteInt32(65536)
            .WriteUInt16(0).WriteUInt16(256)
            .CopyBytes();
    }

    static byte[] BuildName()
    {
        byte[] value = Encoding.BigEndianUnicode.GetBytes("Weight");
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(18)
            .WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
            .WriteUInt16(256).WriteUInt16((ushort)value.Length).WriteUInt16(0)
            .WriteBytes(value)
            .CopyBytes();
    }

    static int GetRootOffset(byte[] bytes, int recordIndex)
    {
        return 34 + ReadUInt32(bytes, 34 + 4 + recordIndex * 6 + 2);
    }

    static int ReadUInt32(byte[] bytes, int offset)
    {
        return System.Convert.ToInt32(BinaryPrimitives.ReadUInt32BigEndian(
            bytes.AsSpan(offset, 4)));
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(offset, 2),
            (ushort)value);
    }

    static void PatchUInt24(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 16);
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        return FontLimits.Create(
            1000000, 1000000, 10, 100, 100, 100, 100, 10, 10, 10,
            100, 1000, maxCoverageEntries, 1000, 1000000, 100, 100, 10, 100);
    }
}

using ttf;

namespace ttf.Tests;

internal class FontMetricInspectionPublicTests
{
    [Test]
    public void ExposesHorizontalAndVerticalBaseBaselineCoordinates()
    {
        FontMetricInspection inspection = CreateCompleteFace()
            .GetMetricInspection();
        FontBaselines baselines = inspection.GetBaselines()!;
        FontBaselineAxis horizontal = baselines.GetHorizontalAxis()!;
        FontBaselineAxis vertical = baselines.GetVerticalAxis()!;
        FontBaselineScript latin = horizontal.FindScript(Tag("latn"))!;
        FontBaselineCoordinate ideographic = latin.GetCoordinate(0);
        FontBaselineCoordinate roman = latin.GetCoordinate(1);
        FontBaselineScript han = vertical.FindScript(Tag("hani"))!;
        FontBaselineCoordinate verticalIdeographic = han.GetCoordinate(0);

        Assert.Multiple(() =>
        {
            Assert.That(baselines.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(baselines.GetMinorVersion(), Is.Zero);
            Assert.That(horizontal.IsHorizontal(), Is.True);
            Assert.That(horizontal.IsVertical(), Is.False);
            Assert.That(horizontal.GetBaselineTagCount(), Is.EqualTo(2));
            Assert.That(horizontal.GetBaselineTag(0), Is.EqualTo(Tag("ideo")));
            Assert.That(horizontal.GetBaselineTag(1), Is.EqualTo(Tag("romn")));
            Assert.That(horizontal.GetScriptCount(), Is.EqualTo(1));
            Assert.That(horizontal.GetScript(0), Is.SameAs(latin));
            Assert.That(latin.GetScriptTag(), Is.EqualTo(Tag("latn")));
            Assert.That(latin.HasBaselineValues(), Is.True);
            Assert.That(latin.GetDefaultBaselineTag(), Is.EqualTo(Tag("romn")));
            Assert.That(latin.GetCoordinateCount(), Is.EqualTo(2));
            Assert.That(ideographic.GetBaselineTag(), Is.EqualTo(Tag("ideo")));
            Assert.That(ideographic.GetFormat(), Is.EqualTo(1));
            Assert.That(ideographic.GetCoordinate(), Is.EqualTo(-120));
            Assert.That(ideographic.HasContourPointReference(), Is.False);
            Assert.That(roman.GetBaselineTag(), Is.EqualTo(Tag("romn")));
            Assert.That(roman.GetFormat(), Is.EqualTo(2));
            Assert.That(roman.GetCoordinate(), Is.Zero);
            Assert.That(roman.HasContourPointReference(), Is.True);
            Assert.That(roman.GetReferenceGlyphId(), Is.EqualTo(3));
            Assert.That(roman.GetContourPointIndex(), Is.EqualTo(7));
            Assert.That(vertical.IsVertical(), Is.True);
            Assert.That(han.GetDefaultBaselineTag(), Is.EqualTo(Tag("ideo")));
            Assert.That(verticalIdeographic.GetFormat(), Is.EqualTo(3));
            Assert.That(verticalIdeographic.GetCoordinate(), Is.EqualTo(500));
            Assert.That(verticalIdeographic.HasAdjustment(), Is.False);
            Assert.That(horizontal.FindScript(Tag("hani")), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                horizontal.GetBaselineTag(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                horizontal.GetScript(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                latin.GetCoordinate(2));
            Assert.Throws<InvalidOperationException>(() =>
                ideographic.GetReferenceGlyphId());
        });
    }

    [Test]
    public void ExposesHdmxVdmxLtshGaspAndPairKerningData()
    {
        FontFace face = CreateCompleteFace();
        FontMetricInspection inspection = face.GetMetricInspection();
        FontHorizontalDeviceMetrics hdmx =
            inspection.GetHorizontalDeviceMetrics()!;
        FontVerticalDeviceMetrics vdmx = inspection.GetVerticalDeviceMetrics()!;
        FontLinearThresholds ltsh = inspection.GetLinearThresholds()!;
        FontGridFittingBehavior gasp = inspection.GetGridFittingBehavior()!;
        FontLegacyKerning kern = inspection.GetLegacyKerning()!;
        FontLegacyKerningSubtable pairs = kern.GetSubtable(0);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetMetricInspection(), Is.SameAs(inspection));
            Assert.That(hdmx.GetVersion(), Is.Zero);
            Assert.That(hdmx.GetDeviceRecordSize(), Is.EqualTo(8));
            Assert.That(hdmx.GetRecordCount(), Is.EqualTo(2));
            Assert.That(hdmx.GetPixelSize(0), Is.EqualTo(12));
            Assert.That(hdmx.GetMaximumWidth(0), Is.EqualTo(7));
            Assert.That(hdmx.GetWidth(0, 2), Is.EqualTo(7));
            Assert.That(hdmx.FindRecordIndex(16), Is.EqualTo(1));
            Assert.That(hdmx.FindRecordIndex(14), Is.EqualTo(-1));
            Assert.That(vdmx.GetVersion(), Is.EqualTo(1));
            Assert.That(vdmx.GetGroupCount(), Is.EqualTo(2));
            Assert.That(vdmx.GetGroupStartingPixelHeight(0), Is.EqualTo(8));
            Assert.That(vdmx.GetGroupEndingPixelHeight(0), Is.EqualTo(12));
            Assert.That(vdmx.GetGroupRecordCount(0), Is.EqualTo(2));
            Assert.That(vdmx.GetRecordPixelHeight(0, 1), Is.EqualTo(12));
            Assert.That(vdmx.GetRecordMaximumY(0, 1), Is.EqualTo(10));
            Assert.That(vdmx.GetRecordMinimumY(0, 1), Is.EqualTo(-3));
            Assert.That(vdmx.GetRatioCount(), Is.EqualTo(3));
            Assert.That(vdmx.GetRatioCharacterSet(0), Is.EqualTo(1));
            Assert.That(vdmx.GetRatioX(0), Is.EqualTo(1));
            Assert.That(vdmx.GetRatioStartingY(1), Is.EqualTo(1));
            Assert.That(vdmx.GetRatioEndingY(1), Is.EqualTo(2));
            Assert.That(vdmx.IsDefaultRatio(2), Is.True);
            Assert.That(vdmx.GetRatioGroupIndex(0), Is.Zero);
            Assert.That(vdmx.GetRatioGroupIndex(1), Is.EqualTo(1));
            Assert.That(vdmx.FindRatioIndex(1, 1), Is.Zero);
            Assert.That(vdmx.FindRatioIndex(2, 1), Is.EqualTo(1));
            Assert.That(ltsh.GetVersion(), Is.Zero);
            Assert.That(ltsh.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(ltsh.GetLinearThreshold(2), Is.EqualTo(24));
            Assert.That(ltsh.IsLinearAtPixelsPerEm(2, 23), Is.False);
            Assert.That(ltsh.IsLinearAtPixelsPerEm(2, 24), Is.True);
            Assert.That(gasp.GetVersion(), Is.EqualTo(1));
            Assert.That(gasp.GetRangeCount(), Is.EqualTo(3));
            Assert.That(gasp.GetMaximumPixelsPerEm(0), Is.EqualTo(8));
            Assert.That(gasp.GetBehavior(0), Is.EqualTo(2));
            Assert.That(gasp.UsesGrayscaleRendering(0), Is.True);
            Assert.That(gasp.UsesGridFitting(1), Is.True);
            Assert.That(gasp.UsesSymmetricGridFitting(2), Is.True);
            Assert.That(gasp.UsesSymmetricSmoothing(2), Is.True);
            Assert.That(gasp.FindRangeIndex(9), Is.EqualTo(1));
            Assert.That(kern.GetVersion(), Is.Zero);
            Assert.That(kern.IsAppleFormat(), Is.False);
            Assert.That(kern.GetSubtableCount(), Is.EqualTo(1));
            Assert.That(kern.GetSubtable(0), Is.SameAs(pairs));
            Assert.That(pairs.GetFormat(), Is.Zero);
            Assert.That(pairs.GetLength(), Is.EqualTo(26));
            Assert.That(pairs.GetCoverage(), Is.EqualTo(0x000D));
            Assert.That(pairs.GetTupleIndex(), Is.Zero);
            Assert.That(pairs.IsAppleFormat(), Is.False);
            Assert.That(pairs.IsHorizontal(), Is.True);
            Assert.That(pairs.IsVertical(), Is.False);
            Assert.That(pairs.ContainsMinimumValues(), Is.False);
            Assert.That(pairs.IsCrossStream(), Is.True);
            Assert.That(pairs.OverridesPreviousValues(), Is.True);
            Assert.That(pairs.IsVariationData(), Is.False);
            Assert.That(pairs.SupportsPairLookup(), Is.True);
            Assert.That(pairs.GetPairCount(), Is.EqualTo(2));
            Assert.That(pairs.GetPairLeftGlyphId(0), Is.EqualTo(1));
            Assert.That(pairs.GetPairRightGlyphId(0), Is.EqualTo(2));
            Assert.That(pairs.GetPairValue(0), Is.EqualTo(-80));
            Assert.That(pairs.GetKerningValue(1, 2), Is.EqualTo(-80));
            Assert.That(pairs.GetKerningValue(2, 1), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => hdmx.GetWidth(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                hdmx.FindRecordIndex(256));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                vdmx.GetRecordPixelHeight(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                vdmx.FindRatioIndex(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ltsh.GetLinearThreshold(5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ltsh.IsLinearAtPixelsPerEm(2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => gasp.GetBehavior(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                gasp.FindRangeIndex(0x10000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pairs.GetKerningValue(5, 1));
            Assert.Throws<InvalidOperationException>(() => pairs.GetStateCount());
        });
    }

    [Test]
    public void ExposesAppleStateMachineKerningWithoutPretendingItIsPairData()
    {
        FontLegacyKerning kern = CreateAppleKernFace()
            .GetMetricInspection()
            .GetLegacyKerning()!;
        FontLegacyKerningSubtable state = kern.GetSubtable(0);

        Assert.Multiple(() =>
        {
            Assert.That(kern.IsAppleFormat(), Is.True);
            Assert.That(kern.GetVersion(), Is.EqualTo(0x00010000U));
            Assert.That(state.GetFormat(), Is.EqualTo(1));
            Assert.That(state.IsHorizontal(), Is.True);
            Assert.That(state.IsCrossStream(), Is.True);
            Assert.That(state.SupportsPairLookup(), Is.False);
            Assert.That(state.GetStateClassCount(), Is.EqualTo(5));
            Assert.That(state.GetStateCount(), Is.EqualTo(2));
            Assert.That(state.GetInitialStateIndex(), Is.Zero);
            Assert.That(state.GetStateClassForGlyph(2), Is.EqualTo(4));
            Assert.That(state.GetStateActionIndex(0, 4), Is.EqualTo(1));
            Assert.That(state.GetStateActionCount(), Is.EqualTo(2));
            Assert.That(state.GetStateActionFlags(1), Is.EqualTo(0x8022));
            Assert.That(state.GetStateActionNewStateIndex(1), Is.EqualTo(1));
            Assert.That(state.StateActionPushesGlyph(1), Is.True);
            Assert.That(state.StateActionDoesNotAdvance(1), Is.False);
            Assert.That(state.GetStateValueCount(), Is.EqualTo(2));
            Assert.That(state.GetStateValue(0), Is.EqualTo(683));
            Assert.That(state.GetStateValue(1), Is.EqualTo(-1));
            Assert.Throws<InvalidOperationException>(() =>
                state.GetKerningValue(1, 2));
            Assert.Throws<InvalidOperationException>(() => state.GetPairCount());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.GetStateActionIndex(2, 0));
        });
    }

    [Test]
    public void KeepsResolvedMetricsAndExactRunInkPubliclyReachable()
    {
        FontFace face = FontInstanceTests.CreateFace();
        FontInstance instance = face.CreateDefaultInstance();
        FontDesignMetrics design = face.GetDesignMetrics(
            FontMetricSelectionPolicy.HorizontalHeader);
        FontScaledMetrics scaled = face.GetScaledMetrics(
            FontMetricSelectionPolicy.HorizontalHeader,
            100);
        ShapedRun run = CreateRun();
        TextMeasurement measurement = instance.MeasureRun(run, 100);
        TextStyle style = new TextStyle(100);
        style.SetHorizontalScale(2);
        TextMeasurement styled = instance.MeasureStyledRun(run, style);

        Assert.Multiple(() =>
        {
            Assert.That(design.GetUnitsPerEm(), Is.EqualTo(1000));
            Assert.That(scaled.GetDesignMetrics().GetAscender(),
                Is.EqualTo(design.GetAscender()));
            Assert.That(scaled.GetPointSize(), Is.EqualTo(100));
            Assert.That(measurement.GetXAdvance(), Is.EqualTo(120));
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(measurement.GetInkBounds().GetXMinimum(), Is.EqualTo(-1));
            Assert.That(measurement.GetInkBounds().GetXMaximum(), Is.EqualTo(52.5));
            Assert.That(styled.GetXAdvance(), Is.EqualTo(240));
            Assert.That(styled.GetInkBounds().GetXMaximum(), Is.EqualTo(105));
            Assert.Throws<ArgumentNullException>(() =>
                instance.MeasureRun(null!, 12));
            Assert.Throws<ArgumentNullException>(() =>
                instance.MeasureStyledRun(run, null!));
        });
    }

    static FontFace CreateCompleteFace()
    {
        return CreateFace(new int[]
        {
            FontTags.Base,
            FontTags.Gasp,
            FontTags.Hdmx,
            FontTags.Kern,
            FontTags.Ltsh,
            FontTags.Maxp,
            FontTags.Vdmx
        }, new byte[][]
        {
            BuildBase(),
            BuildGasp(),
            BuildHdmx(),
            BuildWindowsKern(),
            BuildLtsh(),
            BuildMaxp(),
            BuildVdmx()
        });
    }

    static FontFace CreateAppleKernFace()
    {
        return CreateFace(
            new int[] { FontTags.Kern, FontTags.Maxp },
            new byte[][] { BuildAppleKern(), BuildMaxp() });
    }

    static FontFace CreateFace(int[] tags, byte[][] values)
    {
        SfntTableData[] tables = new SfntTableData[tags.Length];
        int index = 0;

        while (index < tags.Length)
        {
            FontData data = FontData.Create(values[index]);
            tables[index] = new SfntTableData(
                tags[index],
                FontChecksum.CalculateTable(values[index]),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true);
            index += 1;
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static ShapedRun CreateRun()
    {
        RuneBuffer source = Utf16Decoder.Decode(
            "aa",
            Utf16DecodingMode.Strict,
            2);
        ShapingBuffer buffer = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        buffer.SetGlyphId(0, 0);
        buffer.SetGlyphId(1, 2);
        SfntGposPositionBuffer positions = SfntGposPositionBuffer.Create(
            new int[] { 0, 2 });
        positions.ApplyAdjustment(
            0,
            new SfntGposAdjustment(-10, 20, 500, 0));
        positions.ApplyAdjustment(
            1,
            new SfntGposAdjustment(5, -4, 700, 0));
        return ShapedRunBuilder.Build(buffer, source, positions);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(5)
            .CopyBytes();
    }

    static byte[] BuildGasp()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(8)
            .WriteUInt16(2)
            .WriteUInt16(16)
            .WriteUInt16(1)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(12)
            .CopyBytes();
    }

    static byte[] BuildHdmx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt32(8)
            .WriteUInt8(12)
            .WriteUInt8(7)
            .WriteBytes(new byte[] { 5, 6, 7, 4, 3 })
            .WriteUInt8(0)
            .WriteUInt8(16)
            .WriteUInt8(10)
            .WriteBytes(new byte[] { 8, 9, 10, 9, 8 })
            .WriteUInt8(0)
            .CopyBytes();
    }

    static byte[] BuildVdmx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(3)
            .WriteBytes(new byte[] { 1, 1, 1, 1 })
            .WriteBytes(new byte[] { 0, 2, 1, 2 })
            .WriteBytes(new byte[] { 1, 0, 0, 0 })
            .WriteUInt16(24)
            .WriteUInt16(40)
            .WriteUInt16(24)
            .WriteUInt16(2)
            .WriteUInt8(8)
            .WriteUInt8(12)
            .WriteUInt16(8)
            .WriteInt16(7)
            .WriteInt16(-2)
            .WriteUInt16(12)
            .WriteInt16(10)
            .WriteInt16(-3)
            .WriteUInt16(2)
            .WriteUInt8(10)
            .WriteUInt8(20)
            .WriteUInt16(10)
            .WriteInt16(9)
            .WriteInt16(-2)
            .WriteUInt16(20)
            .WriteInt16(17)
            .WriteInt16(-4)
            .CopyBytes();
    }

    static byte[] BuildLtsh()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(5)
            .WriteBytes(new byte[] { 1, 12, 24, 1, 50 })
            .CopyBytes();
    }

    static byte[] BuildWindowsKern()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
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

    static byte[] BuildAppleKern()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt32(1)
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

    static byte[] BuildBase()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int horizontalOffset = WriteHorizontalAxis(builder);
        int verticalOffset = WriteVerticalAxis(builder);
        builder.PatchUInt16(4, (ushort)horizontalOffset);
        builder.PatchUInt16(6, (ushort)verticalOffset);
        return builder.CopyBytes();
    }

    static int WriteHorizontalAxis(TestBinaryBuilder builder)
    {
        int axis = builder.GetLength();
        builder.WriteUInt16(0).WriteUInt16(0);
        int tagList = builder.GetLength();
        builder.PatchUInt16(axis, (ushort)(tagList - axis));
        builder.WriteUInt16(2).WriteTag("ideo").WriteTag("romn");
        int scriptList = builder.GetLength();
        builder.PatchUInt16(axis + 2, (ushort)(scriptList - axis));
        builder.WriteUInt16(1).WriteTag("latn").WriteUInt16(0);
        int script = builder.GetLength();
        builder.PatchUInt16(scriptList + 6, (ushort)(script - scriptList));
        builder.WriteUInt16(0).WriteUInt16(0).WriteUInt16(0);
        int values = builder.GetLength();
        builder.PatchUInt16(script, (ushort)(values - script));
        builder.WriteUInt16(1).WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);
        int ideographic = builder.GetLength();
        builder.PatchUInt16(values + 4, (ushort)(ideographic - values));
        builder.WriteUInt16(1).WriteInt16(-120);
        int roman = builder.GetLength();
        builder.PatchUInt16(values + 6, (ushort)(roman - values));
        builder.WriteUInt16(2).WriteInt16(0).WriteUInt16(3).WriteUInt16(7);
        return axis;
    }

    static int WriteVerticalAxis(TestBinaryBuilder builder)
    {
        int axis = builder.GetLength();
        builder.WriteUInt16(4).WriteUInt16(10);
        builder.WriteUInt16(1).WriteTag("ideo");
        builder.WriteUInt16(1).WriteTag("hani").WriteUInt16(8);
        builder.WriteUInt16(6).WriteUInt16(0).WriteUInt16(0);
        builder.WriteUInt16(0).WriteUInt16(1).WriteUInt16(6);
        builder.WriteUInt16(3).WriteInt16(500).WriteUInt16(0);
        return axis;
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}

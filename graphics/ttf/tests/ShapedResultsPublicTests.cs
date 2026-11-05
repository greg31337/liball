using ttf;

namespace ttf.Tests;

internal class ShapedResultsPublicTests
{
    [Test]
    public void ExposesLigatureGlyphClusterMappingsAndTotals()
    {
        FontInstance instance = LoadInstance();
        TextShapingResult result = instance.ShapeText("fi");
        TextShapingRun shapingRun = result.GetRun(0);
        ShapedRun run = shapingRun.GetShapedRun();
        ShapedGlyph glyph = run.GetGlyph(0);
        ShapedCluster cluster = run.GetCluster(0);
        int[] glyphScalars = glyph.CopyUnicodeScalars();
        int[] clusterScalars = cluster.CopyUnicodeScalars();
        glyphScalars[0] = 0;
        clusterScalars[0] = 0;

        TextMeasurement direct = shapingRun.Measure(100);
        TextMeasurement aggregate = result.Measure(100);
        TextMeasurement existing = instance.MeasureRun(run, 100);

        Assert.Multiple(() =>
        {
            Assert.That(shapingRun.GetFontInstance(), Is.SameAs(instance));
            Assert.That(run.GetDirection(),
                Is.EqualTo(ShapingDirection.LeftToRight));
            Assert.That(run.GetSourceLength(), Is.EqualTo(2));
            Assert.That(run.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(run.GetClusterCount(), Is.EqualTo(1));
            Assert.That(glyph.GetGlyphId(), Is.EqualTo(6));
            Assert.That(glyph.GetClusterIndex(), Is.Zero);
            Assert.That(glyph.GetUtf16Start(), Is.Zero);
            Assert.That(glyph.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(glyph.GetUtf16End(), Is.EqualTo(2));
            Assert.That(glyph.GetUnicodeScalarCount(), Is.EqualTo(2));
            Assert.That(glyph.GetUnicodeScalar(0), Is.EqualTo('f'));
            Assert.That(glyph.GetUnicodeScalar(1), Is.EqualTo('i'));
            Assert.That(glyph.CopyUnicodeScalars(),
                Is.EqualTo(new int[] { 'f', 'i' }));
            Assert.That(glyph.GetLigatureComponentCount(), Is.EqualTo(2));
            Assert.That(glyph.GetLigatureComponentIndex(), Is.Zero);
            Assert.That(glyph.GetLogicalIndex(), Is.Zero);
            Assert.That(glyph.GetVisualIndex(), Is.Zero);
            Assert.That(glyph.IsLigature(), Is.True);
            Assert.That(glyph.GetFlags() & ShapedGlyphFlags.Ligature,
                Is.EqualTo(ShapedGlyphFlags.Ligature));
            Assert.That(cluster.GetUtf16Start(), Is.Zero);
            Assert.That(cluster.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(cluster.GetUtf16End(), Is.EqualTo(2));
            Assert.That(cluster.CopyUnicodeScalars(),
                Is.EqualTo(new int[] { 'f', 'i' }));
            Assert.That(cluster.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(cluster.GetGlyphVisualIndex(0), Is.Zero);
            Assert.That(cluster.GetLogicalIndex(), Is.Zero);
            Assert.That(cluster.GetVisualIndex(), Is.Zero);
            Assert.That(run.GetLogicalGlyph(0), Is.SameAs(glyph));
            Assert.That(run.GetVisualGlyphIndex(0), Is.Zero);
            Assert.That(run.GetLogicalGlyphIndex(0), Is.Zero);
            Assert.That(run.GetVisualCluster(0), Is.SameAs(cluster));
            Assert.That(run.GetVisualClusterIndex(0), Is.Zero);
            Assert.That(run.GetLogicalClusterIndex(0), Is.Zero);
            Assert.That(run.GetTotalXAdvance(), Is.EqualTo(glyph.GetXAdvance()));
            Assert.That(run.GetTotalYAdvance(), Is.EqualTo(glyph.GetYAdvance()));
            Assert.That(shapingRun.GetTotalXAdvance(),
                Is.EqualTo(run.GetTotalXAdvance()));
            Assert.That(direct.GetXAdvance(), Is.EqualTo(existing.GetXAdvance()));
            Assert.That(aggregate.GetXAdvance(), Is.EqualTo(direct.GetXAdvance()));
            Assert.That(direct.GetInkBounds().IsEmpty(), Is.False);
            Assert.That(aggregate.GetInkBounds().GetXMinimum(),
                Is.EqualTo(direct.GetInkBounds().GetXMinimum()));
            Assert.That(run.IsUnsafeToBreakAtUtf16Index(0), Is.False);
            Assert.That(run.IsUnsafeToBreakAtUtf16Index(2), Is.False);
        });
    }

    [Test]
    public void ExposesRightToLeftLogicalAndVisualMaps()
    {
        FontInstance instance = LoadInstance();
        TextShapingResult result = instance.ShapeText("\u05D0\u05D1");
        ShapedRun run = result.GetRun(0).GetShapedRun();
        ShapedGlyph firstVisual = run.GetGlyph(0);
        ShapedCluster firstLogical = run.GetCluster(0);

        Assert.Multiple(() =>
        {
            Assert.That(run.GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(run.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(run.GetClusterCount(), Is.EqualTo(2));
            Assert.That(firstVisual.GetUtf16Start(), Is.EqualTo(1));
            Assert.That(firstVisual.GetLogicalIndex(), Is.EqualTo(1));
            Assert.That(firstVisual.GetVisualIndex(), Is.Zero);
            Assert.That(run.GetVisualGlyphIndex(0), Is.EqualTo(1));
            Assert.That(run.GetLogicalGlyphIndex(0), Is.EqualTo(1));
            Assert.That(run.GetLogicalGlyph(0).GetUtf16Start(), Is.Zero);
            Assert.That(firstLogical.GetUtf16Start(), Is.Zero);
            Assert.That(firstLogical.GetVisualIndex(), Is.EqualTo(1));
            Assert.That(run.GetVisualClusterIndex(0), Is.EqualTo(1));
            Assert.That(run.GetLogicalClusterIndex(0), Is.EqualTo(1));
            Assert.That(run.GetVisualCluster(0).GetUtf16Start(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExposesOriginalUnicodeRangesAndDiagnosticsAfterNormalization()
    {
        FontInstance instance = LoadInstance();
        TextShapingResult result = instance.ShapeText("A\u030A");
        ShapedGlyph glyph = result.GetRun(0).GetShapedRun().GetGlyph(0);
        FontDiagnostic diagnostic = result.GetDiagnostic(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSourceRuneCount(), Is.EqualTo(2));
            Assert.That(result.GetNormalizedRuneCount(), Is.EqualTo(1));
            Assert.That(result.GetGraphemeClusterCount(), Is.EqualTo(1));
            Assert.That(glyph.GetUtf16Start(), Is.Zero);
            Assert.That(glyph.GetUtf16End(), Is.EqualTo(2));
            Assert.That(glyph.CopyUnicodeScalars(),
                Is.EqualTo(new int[] { 'A', 0x030A }));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(diagnostic.GetMessage(), Does.Contain("U+00C5"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetDiagnostic(1));
        });
    }

    [Test]
    public void PropagatesGposAttachmentFlagsAndUnsafeBoundaries()
    {
        FontInstance instance = LoadInstance();
        TextShapingOptions options = new TextShapingOptions();
        options.SetNormalizationMode(UnicodeNormalizationMode.None);
        TextShapingResult positioned = instance.ShapeTextWithOptions(
            "A\u0301",
            options);
        ShapedRun positionedRun = positioned.GetRun(0).GetShapedRun();
        ShapedGlyph mark = positionedRun.GetGlyph(1);

        RuneBuffer source = Utf16Decoder.Decode(
            "abc",
            Utf16DecodingMode.Strict,
            3);
        ShapingBuffer buffer = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        int index = 0;
        while (index < buffer.GetCount())
        {
            buffer.SetGlyphId(index, index + 1);
            index += 1;
        }
        buffer.MergeClusters(0, 2);
        buffer.SetUnsafeToBreak(2, true);
        ShapedRun unsafeRun = ShapedRunBuilder.Build(buffer, source);

        Assert.Multiple(() =>
        {
            Assert.That(mark.IsMarkAttachment(), Is.True);
            Assert.That(mark.IsCursiveAttachment(), Is.False);
            Assert.That(mark.GetFlags() & ShapedGlyphFlags.MarkAttachment,
                Is.EqualTo(ShapedGlyphFlags.MarkAttachment));
            Assert.That(mark.GetXOffset(), Is.EqualTo(-300));
            Assert.That(mark.GetYOffset(), Is.EqualTo(200));
            Assert.That(unsafeRun.IsUnsafeToBreakAtUtf16Index(1), Is.True);
            Assert.That(unsafeRun.IsUnsafeToBreakAtUtf16Index(2), Is.True);
            Assert.That(unsafeRun.GetCluster(1).IsUnsafeToBreak(), Is.True);
            Assert.That(unsafeRun.GetGlyph(2).IsUnsafeToBreak(), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                unsafeRun.IsUnsafeToBreakAtUtf16Index(4));
        });
    }

    [Test]
    public void EmptyResultsHaveZeroAdvanceAndEmptyInk()
    {
        TextShapingResult result = LoadInstance().ShapeText("");
        TextMeasurement measurement = result.Measure(12);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.Zero);
            Assert.That(measurement.GetXAdvance(), Is.Zero);
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(measurement.GetInkBounds().IsEmpty(), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRun(0));
        });
    }

    static FontInstance LoadInstance()
    {
        return HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
    }
}

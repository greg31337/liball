using ttf;

namespace ttf.Tests;

internal class SfntShapedRunMetricsTests
{
    static int Dflt = (int)TestSfntEncoding.EncodeTag("DFLT");
    static int Latn = (int)TestSfntEncoding.EncodeTag("latn");
    static int Ideo = (int)TestSfntEncoding.EncodeTag("ideo");
    static int Romn = (int)TestSfntEncoding.EncodeTag("romn");

    [Test]
    public void ResolvesHorizontalBaselineAndPositionsEveryGdefCaret()
    {
        SfntGposPositionBuffer buffer = CreateHorizontalBuffer();
        SfntGposAdjustmentContext baselineContext =
            new SfntGposAdjustmentContext(1000, 100, 50, null);
        SfntGposAdjustmentContext caretContext =
            new SfntGposAdjustmentContext(1000, 100, 50, null);
        SfntShapedRunMetricContext context =
            new SfntShapedRunMetricContext(
                (glyphId, pointIndex, horizontalCoordinate) =>
                    ResolvePoint(
                        glyphId,
                        pointIndex,
                        horizontalCoordinate),
                baselineContext,
                caretContext);
        SfntShapedRunMetrics metrics = SfntShapedRunMetricResolver.Resolve(
            CreateBaseTable(),
            CreateGdefTable(),
            buffer,
            Latn,
            0,
            true,
            context);
        SfntShapedBaselineResult? baseline = metrics.GetBaseline();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.IsHorizontal(), Is.True);
            Assert.That(metrics.IsVertical(), Is.False);
            Assert.That(metrics.GetAdvance(), Is.EqualTo(1480));
            Assert.That(baseline!.GetScriptTag(), Is.EqualTo(Dflt));
            Assert.That(baseline.GetBaselineTag(), Is.EqualTo(Romn));
            Assert.That(baseline.GetCoordinateFormat(), Is.EqualTo(2));
            Assert.That(baseline.GetPosition(), Is.EqualTo(-40));
            Assert.That(baseline.UsedDefaultScript(), Is.True);
            Assert.That(baseline.UsedDefaultBaseline(), Is.True);
            Assert.That(baseline.UsedCoordinateFallback(), Is.False);
            Assert.That(metrics.GetCaretCount(), Is.EqualTo(3));
            Assert.That(metrics.GetCaret(0).GetGlyphIndex(), Is.EqualTo(1));
            Assert.That(metrics.GetCaret(0).GetGlyphId(), Is.EqualTo(4));
            Assert.That(metrics.GetCaret(0).GetPosition(), Is.EqualTo(610));
            Assert.That(metrics.GetCaret(1).HasContourPointIndex(), Is.True);
            Assert.That(metrics.GetCaret(1).GetContourPointIndex(), Is.EqualTo(9));
            Assert.That(metrics.GetCaret(1).GetPosition(), Is.EqualTo(735));
            Assert.That(metrics.GetCaret(2).GetCaretFormat(), Is.EqualTo(3));
            Assert.That(metrics.GetCaret(2).GetPosition(), Is.EqualTo(860));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                metrics.GetCaret(3));
        });
    }

    [Test]
    public void ResolvesVerticalMetricsAndReportsUnavailablePointCaret()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 4 });
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(0, 20, 0, 600));
        SfntShapedRunMetricContext context =
            new SfntShapedRunMetricContext(
                null,
                new SfntGposAdjustmentContext(1000, 100, 50, null),
                new SfntGposAdjustmentContext(1000, 100, 50, null));
        SfntShapedRunMetrics metrics = SfntShapedRunMetricResolver.Resolve(
            CreateBaseTable(),
            CreateGdefTable(),
            buffer,
            Dflt,
            Ideo,
            false,
            context);
        SfntShapedBaselineResult? baseline = metrics.GetBaseline();
        SfntShapedCaretResult pointCaret = metrics.GetCaret(1);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.IsVertical(), Is.True);
            Assert.That(metrics.GetAdvance(), Is.EqualTo(600));
            Assert.That(baseline!.GetBaselineTag(), Is.EqualTo(Ideo));
            Assert.That(baseline.GetPosition(), Is.EqualTo(320));
            Assert.That(baseline.UsedDefaultScript(), Is.False);
            Assert.That(baseline.UsedDefaultBaseline(), Is.False);
            Assert.That(metrics.GetCaret(0).GetPosition(), Is.EqualTo(120));
            Assert.That(pointCaret.HasPosition(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                pointCaret.GetPosition());
            Assert.That(metrics.GetCaret(2).GetPosition(), Is.EqualTo(320));
        });
    }

    [Test]
    public void UsesBaseCoordinateFallbackWhenContourPointsAreUnavailable()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            Array.Empty<int>());
        SfntShapedRunMetrics metrics = SfntShapedRunMetricResolver.Resolve(
            CreateBaseTable(),
            null!,
            buffer,
            Dflt,
            0,
            true,
            null!);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetAdvance(), Is.Zero);
            Assert.That(metrics.GetCaretCount(), Is.Zero);
            Assert.That(metrics.GetBaseline()!.GetPosition(), Is.EqualTo(-20));
            Assert.That(metrics.GetBaseline()!.UsedCoordinateFallback(), Is.True);
        });
    }

    [Test]
    public void RequiresBuffersAndContextsNeededByAdjustments()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 4 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntShapedRunMetricResolver.Resolve(
                    null!, null!, null!, Dflt, Ideo, true, null!));
            Assert.Throws<InvalidOperationException>(() =>
                SfntShapedRunMetricResolver.Resolve(
                    CreateBaseTable(),
                    null!,
                    buffer,
                    Dflt,
                    Ideo,
                    false,
                    null!));
            Assert.Throws<InvalidOperationException>(() =>
                SfntShapedRunMetricResolver.Resolve(
                    null!,
                    CreateGdefTable(),
                    buffer,
                    Dflt,
                    Ideo,
                    true,
                    null!));
        });
    }

    static SfntGposPositionBuffer CreateHorizontalBuffer()
    {
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 4, 2 });
        buffer.ApplyAdjustment(
            0,
            new SfntGposAdjustment(0, 0, 500, 0));
        buffer.ApplyAdjustment(
            1,
            new SfntGposAdjustment(10, 0, 580, 0));
        buffer.ApplyAdjustment(
            2,
            new SfntGposAdjustment(0, 0, 400, 0));
        return buffer;
    }

    static SfntBaseTable CreateBaseTable()
    {
        SfntDeviceOrVariationIndex baselineAdjustment =
            CreateDeviceAdjustment(100, 2);
        SfntBaseCoordinate horizontalIdeographic = new SfntBaseCoordinate(
            3,
            100,
            0,
            0,
            1,
            1,
            baselineAdjustment);
        SfntBaseCoordinate horizontalRoman = new SfntBaseCoordinate(
            2,
            -20,
            3,
            7,
            0,
            0,
            null);
        SfntBaseAxis horizontal = new SfntBaseAxis(
            true,
            new int[] { Ideo, Romn },
            new SfntBaseScript[]
            {
                new SfntBaseScript(
                    Dflt,
                    new SfntBaseValues(
                        1,
                        new SfntBaseCoordinate[]
                        {
                            horizontalIdeographic,
                            horizontalRoman
                        }),
                    null,
                    Array.Empty<SfntBaseLanguageSystem>())
            });
        SfntBaseCoordinate verticalIdeographic = new SfntBaseCoordinate(
            3,
            300,
            0,
            0,
            1,
            1,
            baselineAdjustment);
        SfntBaseAxis vertical = new SfntBaseAxis(
            false,
            new int[] { Ideo },
            new SfntBaseScript[]
            {
                new SfntBaseScript(
                    Dflt,
                    new SfntBaseValues(
                        0,
                        new SfntBaseCoordinate[] { verticalIdeographic }),
                    null,
                    Array.Empty<SfntBaseLanguageSystem>())
            });
        return new SfntBaseTable(1, 1, 0, horizontal, vertical);
    }

    static SfntGdefTable CreateGdefTable()
    {
        SfntGdefCaretValue[] carets = new SfntGdefCaretValue[]
        {
            new SfntGdefCaretValue(1, 100, 0, 0, 0, null),
            new SfntGdefCaretValue(2, 0, 9, 0, 0, null),
            new SfntGdefCaretValue(
                3,
                300,
                0,
                1,
                1,
                CreateDeviceAdjustment(100, 5))
        };
        SfntCoverageTable coverage = new SfntCoverageTable(
            1,
            new int[] { 4 },
            Array.Empty<SfntCoverageRange>(),
            1);
        SfntGdefLigatureCaretList list = new SfntGdefLigatureCaretList(
            coverage,
            new SfntGdefLigatureGlyph[]
            {
                new SfntGdefLigatureGlyph(carets)
            });
        return new SfntGdefTable(
            1,
            3,
            null,
            null,
            list,
            null,
            null,
            null);
    }

    static SfntDeviceOrVariationIndex CreateDeviceAdjustment(
        int pixelsPerEm,
        int delta)
    {
        return SfntDeviceOrVariationIndex.Create(new SfntDeviceTable(
            pixelsPerEm,
            pixelsPerEm,
            3,
            new int[] { delta << 8 }));
    }

    static double ResolvePoint(
        int glyphId,
        int pointIndex,
        bool horizontalCoordinate)
    {
        if (glyphId == 3 && pointIndex == 7 && !horizontalCoordinate)
        {
            return -40;
        }

        if (glyphId == 4 && pointIndex == 9 && horizontalCoordinate)
        {
            return 225;
        }

        throw new InvalidOperationException("Unexpected contour point.");
    }
}

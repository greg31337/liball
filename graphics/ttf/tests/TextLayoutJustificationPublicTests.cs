using ttf;

namespace ttf.Tests;

internal class TextLayoutJustificationPublicTests
{
    [Test]
    public void AppliesStartCenterAndEndAlignmentOnHorizontalAndVerticalAxes()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions startOptions = new TextLayoutOptions();
        double natural = Prepare(face, "AB", startOptions)
            .Reflow(1000).GetLine(0).GetAdvance();
        double width = natural + 40;
        TextLayoutOptions centerOptions = new TextLayoutOptions();
        centerOptions.SetAlignment(TextAlignment.Center);
        TextLayoutOptions endOptions = new TextLayoutOptions();
        endOptions.SetAlignment(TextAlignment.End);
        TextLayoutOptions verticalOptions = new TextLayoutOptions();
        verticalOptions.SetAlignment(TextAlignment.End);
        verticalOptions.SetWritingMode(TextWritingMode.VerticalTopToBottom);

        TextLineResult start = Prepare(face, "AB", startOptions)
            .Reflow(width).GetLine(0);
        TextLineResult center = Prepare(face, "AB", centerOptions)
            .Reflow(width).GetLine(0);
        TextLineResult end = Prepare(face, "AB", endOptions)
            .Reflow(width).GetLine(0);
        TextLineResult vertical = Prepare(face, "AB", verticalOptions)
            .Reflow(width).GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(start.GetInlineOffset(), Is.Zero);
            Assert.That(center.GetInlineOffset(), Is.EqualTo(20).Within(0.000001));
            Assert.That(end.GetInlineOffset(), Is.EqualTo(40).Within(0.000001));
            Assert.That(start.GetVisualRun(0).GetX(), Is.Zero);
            Assert.That(center.GetVisualRun(0).GetX(),
                Is.EqualTo(20).Within(0.000001));
            Assert.That(end.GetVisualRun(0).GetX(),
                Is.EqualTo(40).Within(0.000001));
            Assert.That(vertical.GetWritingMode(),
                Is.EqualTo(TextWritingMode.VerticalTopToBottom));
            Assert.That(vertical.GetVisualRun(0).GetY(),
                Is.EqualTo(-vertical.GetInlineOffset()).Within(0.000001));
        });
    }

    [Test]
    public void ConstructsHorizontalAndVerticalBidiRunsAndGlyphPositions()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace face = FontClusterFallbackTests.Clone(
            source,
            "Mixed",
            new int[] { 0x0041, 0x05D0, 0x05D1 });
        TextLayoutOptions horizontalOptions = new TextLayoutOptions();
        TextLayoutOptions topOptions = new TextLayoutOptions();
        topOptions.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        TextLayoutOptions bottomOptions = new TextLayoutOptions();
        bottomOptions.SetWritingMode(TextWritingMode.VerticalBottomToTop);

        TextLineResult horizontal = Prepare(
            face,
            "A\u05D0\u05D1",
            horizontalOptions).Reflow(1000).GetLine(0);
        TextLineResult top = Prepare(
            face,
            "A\u05D0\u05D1",
            topOptions).Reflow(1000).GetLine(0);
        TextLineResult bottom = Prepare(
            face,
            "A\u05D0\u05D1",
            bottomOptions).Reflow(1000).GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.GetRunCount(), Is.EqualTo(2));
            Assert.That(HasOddBidiRun(horizontal), Is.True);
            AssertInverseRunMappings(horizontal);
            Assert.That(top.GetRunCount(), Is.GreaterThanOrEqualTo(1));
            Assert.That(top.GetVisualRun(0).GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
            Assert.That(bottom.GetVisualRun(0).GetDirection(),
                Is.EqualTo(ShapingDirection.BottomToTop));
            Assert.That(HasVerticalGlyphProgression(top, true), Is.True);
            Assert.That(HasVerticalGlyphProgression(bottom, false), Is.True);
        });
    }

    [Test]
    public void ReportsAppliedSpaceAndTrackingAdjustments()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions naturalOptions = new TextLayoutOptions();
        double spaceNatural = Prepare(face, "A A", naturalOptions)
            .Reflow(1000).GetLine(0).GetAdvance();
        TextLayoutOptions spaceOptions = JustifiedOptions();
        TextLineResult space = Prepare(face, "A A", spaceOptions)
            .Reflow(spaceNatural + 12).GetLine(0);

        double trackingNatural = Prepare(face, "ABC", naturalOptions)
            .Reflow(1000).GetLine(0).GetAdvance();
        TextLayoutOptions trackingOptions = JustifiedOptions();
        trackingOptions.SetMaximumSpaceExpansion(0);
        trackingOptions.SetMaximumTrackingExpansion(10);
        TextLineResult tracking = Prepare(face, "ABC", trackingOptions)
            .Reflow(trackingNatural + 12).GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(space.GetNaturalAdvance(),
                Is.EqualTo(spaceNatural).Within(0.000001));
            Assert.That(space.GetJustificationTargetAdvance(),
                Is.EqualTo(spaceNatural + 12).Within(0.000001));
            Assert.That(space.GetJustificationExpansion(),
                Is.EqualTo(12).Within(0.000001));
            Assert.That(space.IsFullyJustified(), Is.True);
            Assert.That(space.GetJustificationApplicationCount(), Is.EqualTo(1));
            Assert.That(space.GetJustificationApplication(0).GetKind(),
                Is.EqualTo(TextJustificationApplicationKind.Space));
            Assert.That(space.GetJustificationApplication(0).GetClusterBoundary(),
                Is.EqualTo(2));
            Assert.That(space.GetJustificationBoundaryAdjustment(2),
                Is.EqualTo(12).Within(0.000001));
            Assert.That(tracking.GetJustificationApplicationCount(), Is.EqualTo(2));
            Assert.That(tracking.GetJustificationApplication(0).GetKind(),
                Is.EqualTo(TextJustificationApplicationKind.Tracking));
            Assert.That(tracking.GetJustificationBoundaryAdjustment(1),
                Is.EqualTo(6).Within(0.000001));
            Assert.That(tracking.GetJustificationBoundaryAdjustment(2),
                Is.EqualTo(6).Within(0.000001));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                tracking.GetJustificationApplication(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                tracking.GetJustificationBoundaryAdjustment(4));
        });
    }

    [Test]
    public void AppliesSnapshottedJstfAndKashidaOpportunitiesInOrder()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutOptions naturalOptions = new TextLayoutOptions();
        double natural = Prepare(face, "ABCD", naturalOptions)
            .Reflow(1000).GetLine(0).GetAdvance();
        TextJustificationOpportunity[] supplied =
            new TextJustificationOpportunity[]
            {
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf,
                    1,
                    2,
                    0),
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Kashida,
                    3,
                    10,
                    0)
            };
        TextLayoutOptions options = JustifiedOptions();
        options.SetMaximumSpaceExpansion(0);
        options.SetMaximumTrackingExpansion(0);
        options.SetUseJstfJustification(true);
        options.SetUseKashidaJustification(true);
        options.SetJustificationOpportunities(supplied);
        PreparedTextResult prepared = Prepare(face, "ABCD", options);

        supplied[0] = new TextJustificationOpportunity(
            TextJustificationOpportunityKind.Kashida,
            2,
            100,
            0);
        options.ClearJustificationOpportunities();
        TextLineResult line = prepared.Reflow(natural + 6).GetLine(0);
        TextJustificationApplication first =
            line.GetJustificationApplication(0);
        TextJustificationApplication second =
            line.GetJustificationApplication(1);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetJustificationExpansion(),
                Is.EqualTo(6).Within(0.000001));
            Assert.That(line.GetJustificationApplicationCount(), Is.EqualTo(2));
            Assert.That(first.GetKind(),
                Is.EqualTo(TextJustificationApplicationKind.Jstf));
            Assert.That(first.GetClusterBoundary(), Is.EqualTo(1));
            Assert.That(first.GetExpansion(), Is.EqualTo(2));
            Assert.That(second.GetKind(),
                Is.EqualTo(TextJustificationApplicationKind.Kashida));
            Assert.That(second.GetClusterBoundary(), Is.EqualTo(3));
            Assert.That(second.GetExpansion(), Is.EqualTo(4).Within(0.000001));
            Assert.That(options.GetJustificationOpportunityCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                options.SetJustificationOpportunities(null!));
            Assert.Throws<ArgumentException>(() =>
                options.SetJustificationOpportunities(
                    new TextJustificationOpportunity[] { null! }));
        });
    }

    static TextLayoutOptions JustifiedOptions()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        options.SetJustifyLastLine(true);
        return options;
    }

    static PreparedTextResult Prepare(
        FontFace face,
        string text,
        TextLayoutOptions options)
    {
        FontSet set = new FontSet();
        set.Add(face);
        FontNameValue? familyName = face.GetNames().GetFamilyName();
        if (familyName == null)
        {
            familyName = face.GetNames().GetTypographicFamilyName();
        }
        string family = familyName!.GetValue();
        return new TextLayoutEngine().Prepare(
            set,
            text,
            new TextLayoutStyle[]
            {
                new TextLayoutStyle(
                    0,
                    text.Length,
                    new FontRequest(family),
                    new TextStyle(12))
            },
            options);
    }

    static bool HasOddBidiRun(TextLineResult line)
    {
        int index = 0;
        while (index < line.GetRunCount())
        {
            if ((line.GetVisualRun(index).GetBidiLevel() & 1) != 0)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static void AssertInverseRunMappings(TextLineResult line)
    {
        int logicalIndex = 0;
        while (logicalIndex < line.GetRunCount())
        {
            int visualIndex = line.GetVisualRunIndex(logicalIndex);
            Assert.That(line.GetLogicalRunIndex(visualIndex),
                Is.EqualTo(logicalIndex));
            logicalIndex += 1;
        }
    }

    static bool HasVerticalGlyphProgression(
        TextLineResult line,
        bool topToBottom)
    {
        int runIndex = 0;
        while (runIndex < line.GetRunCount())
        {
            PositionedRunResult run = line.GetVisualRun(runIndex);
            if (run.GetGlyphCount() > 1)
            {
                double first = run.GetVisualGlyph(0).GetY();
                double second = run.GetVisualGlyph(1).GetY();
                return topToBottom ? second < first : second > first;
            }
            runIndex += 1;
        }
        return false;
    }
}

namespace ttf.Tests;

public class TextJustificationTests
{
    [Test]
    public void DistributesExpansionAcrossVisibleSpacesFirst()
    {
        string text = "aa bb cc";
        TextBreakMap breaks = PlanBreaks(text);
        TextLayoutOptions options = JustifiedOptions();
        GreedyLineFit line = FitLine(text, breaks, options, 20);

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            false,
            breaks,
            12,
            options,
            Array.Empty<TextJustificationOpportunity>());

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetNaturalAdvance(), Is.EqualTo(8));
            Assert.That(plan.GetTargetAdvance(), Is.EqualTo(12));
            Assert.That(plan.GetJustifiedAdvance(), Is.EqualTo(12));
            Assert.That(plan.IsFullyJustified(), Is.True);
            Assert.That(plan.GetBoundaryAdjustment(3), Is.EqualTo(2));
            Assert.That(plan.GetBoundaryAdjustment(6), Is.EqualTo(2));
            Assert.That(plan.GetBoundaryAdjustment(1), Is.Zero);
            Assert.That(plan.GetApplicationCount(), Is.EqualTo(2));
            Assert.That(plan.GetApplication(0).GetKind(),
                Is.EqualTo(AppliedJustificationKind.Space));
            Assert.That(plan.GetApplication(0).GetClusterBoundary(), Is.EqualTo(3));
        });
    }

    [Test]
    public void UsesCappedSpaceExpansionBeforeTracking()
    {
        string text = "ab cd";
        TextBreakMap breaks = PlanBreaks(text);
        TextLayoutOptions options = JustifiedOptions();
        options.SetMaximumSpaceExpansion(1);
        options.SetMaximumTrackingExpansion(1);
        GreedyLineFit line = FitLine(text, breaks, options, 20);

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            false,
            breaks,
            8,
            options,
            Array.Empty<TextJustificationOpportunity>());

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsFullyJustified(), Is.True);
            Assert.That(plan.GetBoundaryAdjustment(3), Is.EqualTo(1));
            Assert.That(plan.GetBoundaryAdjustment(1), Is.EqualTo(1));
            Assert.That(plan.GetBoundaryAdjustment(4), Is.EqualTo(1));
            Assert.That(plan.GetBoundaryAdjustment(2), Is.Zero);
            Assert.That(plan.GetApplicationCount(), Is.EqualTo(3));
            Assert.That(plan.GetApplication(0).GetKind(),
                Is.EqualTo(AppliedJustificationKind.Space));
            Assert.That(plan.GetApplication(1).GetKind(),
                Is.EqualTo(AppliedJustificationKind.Tracking));
        });
    }

    [Test]
    public void AppliesJstfPrioritiesBeforeKashidaOpportunities()
    {
        string text = "abcd";
        TextBreakMap breaks = PlanBreaks(text);
        TextLayoutOptions options = JustifiedOptions();
        options.SetMaximumSpaceExpansion(0);
        options.SetUseJstfJustification(true);
        options.SetUseKashidaJustification(true);
        GreedyLineFit line = FitLine(text, breaks, options, 20);
        TextJustificationOpportunity[] opportunities =
            new TextJustificationOpportunity[]
            {
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 1, 2, 1),
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 2, 1, 0),
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Kashida, 3, 5, 0)
            };

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            false,
            breaks,
            10,
            options,
            opportunities);

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsFullyJustified(), Is.True);
            Assert.That(plan.GetBoundaryAdjustment(1), Is.EqualTo(2));
            Assert.That(plan.GetBoundaryAdjustment(2), Is.EqualTo(1));
            Assert.That(plan.GetBoundaryAdjustment(3), Is.EqualTo(3));
            Assert.That(plan.GetApplicationCount(), Is.EqualTo(3));
            Assert.That(plan.GetApplication(0).GetKind(),
                Is.EqualTo(AppliedJustificationKind.Jstf));
            Assert.That(plan.GetApplication(0).GetClusterBoundary(), Is.EqualTo(2));
            Assert.That(plan.GetApplication(1).GetClusterBoundary(), Is.EqualTo(1));
            Assert.That(plan.GetApplication(2).GetKind(),
                Is.EqualTo(AppliedJustificationKind.Kashida));
            Assert.That(plan.GetApplication(2).GetExpansion(), Is.EqualTo(3));
        });
    }

    [Test]
    public void SharesEqualPriorityAdvancedExpansionAndHonorsCapacity()
    {
        string text = "abcd";
        TextBreakMap breaks = PlanBreaks(text);
        TextLayoutOptions options = JustifiedOptions();
        options.SetMaximumSpaceExpansion(0);
        options.SetUseJstfJustification(true);
        GreedyLineFit line = FitLine(text, breaks, options, 20);
        TextJustificationOpportunity[] opportunities =
            new TextJustificationOpportunity[]
            {
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 1, 1, 0),
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 2, 10, 0)
            };

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            false,
            breaks,
            8,
            options,
            opportunities);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetBoundaryAdjustment(1), Is.EqualTo(1));
            Assert.That(plan.GetBoundaryAdjustment(2), Is.EqualTo(3));
            Assert.That(plan.IsFullyJustified(), Is.True);
        });
    }

    [Test]
    public void AdvancedOpportunitiesAreExplicitlyOptIn()
    {
        string text = "abcd";
        TextBreakMap breaks = PlanBreaks(text);
        TextLayoutOptions options = JustifiedOptions();
        options.SetMaximumSpaceExpansion(0);
        GreedyLineFit line = FitLine(text, breaks, options, 20);
        TextJustificationOpportunity opportunity =
            new TextJustificationOpportunity(
                TextJustificationOpportunityKind.Kashida,
                2,
                10,
                0);

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            false,
            breaks,
            8,
            options,
            new TextJustificationOpportunity[] { opportunity });

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetJustifiedAdvance(), Is.EqualTo(4));
            Assert.That(plan.IsFullyJustified(), Is.False);
            Assert.That(plan.GetApplicationCount(), Is.Zero);
        });
    }

    [Test]
    public void SkipsFinalHardBreakAndEllipsizedLinesByDefault()
    {
        TextLayoutOptions options = JustifiedOptions();
        TextBreakMap finalBreaks = PlanBreaks("a a");
        GreedyLineFit finalLine = FitLine("a a", finalBreaks, options, 20);
        LineJustificationPlan skippedFinal = TextJustificationPlanner.Plan(
            finalLine,
            true,
            finalBreaks,
            8,
            options,
            Array.Empty<TextJustificationOpportunity>());

        TextBreakMap hardBreaks = PlanBreaks("a a\nnext");
        GreedyLineFit hardLine = FitFirstLine("a a\nnext", hardBreaks, options, 20);
        LineJustificationPlan skippedHard = TextJustificationPlanner.Plan(
            hardLine,
            false,
            hardBreaks,
            8,
            options,
            Array.Empty<TextJustificationOpportunity>());

        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.CharacterEllipsis);
        TextBreakMap ellipsisBreaks = PlanBreaks("abcdef");
        GreedyLineFit ellipsisLine = GreedyLineFitter.Fit(
            ellipsisBreaks,
            UniformAdvances(6),
            4,
            options,
            0,
            1).GetLine(0);
        LineJustificationPlan skippedEllipsis = TextJustificationPlanner.Plan(
            ellipsisLine,
            false,
            ellipsisBreaks,
            8,
            options,
            Array.Empty<TextJustificationOpportunity>());

        Assert.Multiple(() =>
        {
            Assert.That(skippedFinal.GetApplicationCount(), Is.Zero);
            Assert.That(skippedHard.GetApplicationCount(), Is.Zero);
            Assert.That(skippedEllipsis.GetApplicationCount(), Is.Zero);
        });
    }

    [Test]
    public void CanExplicitlyJustifyTheLastLineWithoutChangingSourceRanges()
    {
        string text = "a a";
        TextBreakMap breaks = PlanBreaks(text);
        int[] starts = CopyUtf16Starts(breaks);
        TextLayoutOptions options = JustifiedOptions();
        options.SetJustifyLastLine(true);
        GreedyLineFit line = FitLine(text, breaks, options, 20);

        LineJustificationPlan plan = TextJustificationPlanner.Plan(
            line,
            true,
            breaks,
            6,
            options,
            Array.Empty<TextJustificationOpportunity>());

        Assert.Multiple(() =>
        {
            Assert.That(plan.IsFullyJustified(), Is.True);
            Assert.That(plan.GetClusterStart(), Is.EqualTo(line.GetClusterStart()));
            Assert.That(plan.GetClusterEnd(), Is.EqualTo(line.GetClusterEnd()));
            Assert.That(CopyUtf16Starts(breaks), Is.EqualTo(starts));
            Assert.That(text, Is.EqualTo("a a"));
        });
    }

    [Test]
    public void ValidatesOptionsOpportunitiesAndPlannerInputs()
    {
        TextLayoutOptions options = JustifiedOptions();
        TextBreakMap breaks = PlanBreaks("ab");
        GreedyLineFit line = FitLine("ab", breaks, options, 10);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumSpaceExpansion(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumTrackingExpansion(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextJustificationOpportunity(
                    (TextJustificationOpportunityKind)9, 1, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, -1, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextJustificationOpportunity(
                    TextJustificationOpportunityKind.Jstf, 1, 1, -1));
            Assert.Throws<ArgumentNullException>(() =>
                TextJustificationPlanner.Plan(
                    null!, false, breaks, 4, options,
                    Array.Empty<TextJustificationOpportunity>()));
            Assert.Throws<ArgumentNullException>(() =>
                TextJustificationPlanner.Plan(
                    line, false, null, 4, options,
                    Array.Empty<TextJustificationOpportunity>()));
            Assert.Throws<ArgumentNullException>(() =>
                TextJustificationPlanner.Plan(
                    line, false, breaks, 4, null,
                    Array.Empty<TextJustificationOpportunity>()));
            Assert.Throws<ArgumentNullException>(() =>
                TextJustificationPlanner.Plan(
                    line, false, breaks, 4, options, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TextJustificationPlanner.Plan(
                    line, false, breaks, double.PositiveInfinity, options,
                    Array.Empty<TextJustificationOpportunity>()));
            Assert.Throws<ArgumentException>(() =>
                TextJustificationPlanner.Plan(
                    line,
                    false,
                    breaks,
                    4,
                    options,
                    new TextJustificationOpportunity[] { null! }));
            Assert.Throws<ArgumentException>(() =>
                TextJustificationPlanner.Plan(
                    line,
                    false,
                    breaks,
                    4,
                    options,
                    new TextJustificationOpportunity[]
                    {
                        new TextJustificationOpportunity(
                            TextJustificationOpportunityKind.Jstf, 3, 1, 0)
                    }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TextJustificationPlanner.Plan(
                    line, false, breaks, 4, options,
                    Array.Empty<TextJustificationOpportunity>())
                    .GetBoundaryAdjustment(3));
        });
    }

    static TextLayoutOptions JustifiedOptions()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        return options;
    }

    static GreedyLineFit FitLine(
        string text,
        TextBreakMap breaks,
        TextLayoutOptions options,
        double maximumAdvance)
    {
        GreedyLineFitResult result = GreedyLineFitter.Fit(
            breaks,
            UniformAdvances(breaks.GetClusterCount()),
            maximumAdvance,
            options,
            0,
            1);
        Assert.That(result.GetLineCount(), Is.EqualTo(1));
        return result.GetLine(0);
    }

    static GreedyLineFit FitFirstLine(
        string text,
        TextBreakMap breaks,
        TextLayoutOptions options,
        double maximumAdvance)
    {
        return GreedyLineFitter.Fit(
            breaks,
            UniformAdvances(breaks.GetClusterCount()),
            maximumAdvance,
            options,
            0,
            1).GetLine(0);
    }

    static TextBreakMap PlanBreaks(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        UnicodeData data = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = GraphemeSegmenter.Segment(
            input,
            data,
            input.GetCount(),
            input.GetCount());
        LineBreakMap lineBreaks = LineBreakAlgorithm.Analyze(
            input,
            data,
            input.GetCount());
        return TextBreakPlanner.Plan(
            input,
            clusters,
            lineBreaks,
            false,
            clusters.GetClusterCount());
    }

    static double[] UniformAdvances(int count)
    {
        double[] advances = new double[count];
        int index = 0;
        while (index < count)
        {
            advances[index] = 1;
            index++;
        }

        return advances;
    }

    static int[] CopyUtf16Starts(TextBreakMap breaks)
    {
        int[] starts = new int[breaks.GetClusterCount()];
        int index = 0;
        while (index < starts.Length)
        {
            starts[index] = breaks.GetUtf16Start(index);
            index++;
        }

        return starts;
    }
}

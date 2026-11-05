namespace ttf.Tests;

public class GreedyLineFitterTests
{
    [Test]
    public void GreedilyChoosesTheLastLegalFittingBoundary()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFitResult result = Fit(
            "aa bb cc",
            UniformAdvances(8, 1),
            5,
            options,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(2));
            Assert.That(result.IsTruncated(), Is.False);
            Assert.That(result.GetLine(0).GetClusterStart(), Is.Zero);
            Assert.That(result.GetLine(0).GetClusterEnd(), Is.EqualTo(6));
            Assert.That(result.GetLine(0).GetVisibleClusterEnd(), Is.EqualTo(5));
            Assert.That(result.GetLine(0).GetAdvance(), Is.EqualTo(5));
            Assert.That(result.GetLine(1).GetClusterStart(), Is.EqualTo(6));
            Assert.That(result.GetLine(1).GetAdvance(), Is.EqualTo(2));
        });
    }

    [Test]
    public void HardBreaksEndLinesWithoutContributingVisibleWidth()
    {
        GreedyLineFitResult result = Fit(
            "ab\r\ncd",
            new double[] { 1, 1, 8, 1, 1 },
            20,
            new TextLayoutOptions(),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetClusterEnd(), Is.EqualTo(3));
            Assert.That(result.GetLine(0).GetVisibleClusterEnd(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetAdvance(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).EndedByHardBreak(), Is.True);
            Assert.That(result.GetLine(1).GetClusterStart(), Is.EqualTo(3));
            Assert.That(result.GetLine(1).EndedByHardBreak(), Is.False);
        });
    }

    [Test]
    public void ATerminalHardBreakCreatesAnEmptyFinalLine()
    {
        GreedyLineFitResult result = Fit(
            "ab\n",
            new double[] { 1, 1, 7 },
            20,
            new TextLayoutOptions(),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).EndedByHardBreak(), Is.True);
            Assert.That(result.GetLine(1).GetClusterStart(), Is.EqualTo(3));
            Assert.That(result.GetLine(1).GetClusterEnd(), Is.EqualTo(3));
            Assert.That(result.GetLine(1).GetAdvance(), Is.Zero);
        });
    }

    [Test]
    public void AppliesStartEndAndCenterAlignment()
    {
        TextLayoutOptions start = new TextLayoutOptions();
        TextLayoutOptions end = new TextLayoutOptions();
        TextLayoutOptions center = new TextLayoutOptions();
        end.SetAlignment(TextAlignment.End);
        center.SetAlignment(TextAlignment.Center);

        GreedyLineFit startLine = Fit("ab", UniformAdvances(2, 1), 10, start, 1, 1)
            .GetLine(0);
        GreedyLineFit endLine = Fit("ab", UniformAdvances(2, 1), 10, end, 1, 1)
            .GetLine(0);
        GreedyLineFit centerLine = Fit("ab", UniformAdvances(2, 1), 10, center, 1, 1)
            .GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(startLine.GetInlineOffset(), Is.Zero);
            Assert.That(endLine.GetInlineOffset(), Is.EqualTo(8));
            Assert.That(centerLine.GetInlineOffset(), Is.EqualTo(4));
        });
    }

    [Test]
    public void TrimsLineEdgeWhitespaceWhenConfigured()
    {
        TextLayoutOptions trimmed = new TextLayoutOptions();
        TextLayoutOptions retained = new TextLayoutOptions();
        retained.SetTrimLineWhitespace(false);

        GreedyLineFit trimmedLine = Fit(
            "  ab  ",
            UniformAdvances(6, 1),
            10,
            trimmed,
            1,
            1).GetLine(0);
        GreedyLineFit retainedLine = Fit(
            "  ab  ",
            UniformAdvances(6, 1),
            10,
            retained,
            1,
            1).GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(trimmedLine.GetVisibleClusterStart(), Is.EqualTo(2));
            Assert.That(trimmedLine.GetVisibleClusterEnd(), Is.EqualTo(4));
            Assert.That(trimmedLine.GetAdvance(), Is.EqualTo(2));
            Assert.That(retainedLine.GetVisibleClusterStart(), Is.Zero);
            Assert.That(retainedLine.GetVisibleClusterEnd(), Is.EqualTo(6));
            Assert.That(retainedLine.GetAdvance(), Is.EqualTo(6));
        });
    }

    [Test]
    public void AccountsForAVisibleHyphenWhenBreakingAtSoftHyphen()
    {
        GreedyLineFitResult result = Fit(
            "ab\u00ADcd",
            new double[] { 1, 1, 0, 1, 1 },
            3,
            new TextLayoutOptions(),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetClusterEnd(), Is.EqualTo(3));
            Assert.That(result.GetLine(0).GetContentAdvance(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetAdvance(), Is.EqualTo(3));
            Assert.That(result.GetLine(0).RequiresHyphen(), Is.True);
            Assert.That(result.GetLine(1).RequiresHyphen(), Is.False);
        });
    }

    [Test]
    public void CharacterEllipsisUsesShapedWidthAndConfiguredText()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.CharacterEllipsis);
        options.SetEllipsisText("...");
        options.SetAlignment(TextAlignment.End);

        GreedyLineFitResult result = Fit(
            "abcdef",
            UniformAdvances(6, 1),
            5,
            options,
            1,
            2);
        GreedyLineFit line = result.GetLine(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(1));
            Assert.That(result.IsTruncated(), Is.True);
            Assert.That(result.GetTruncationClusterIndex(), Is.EqualTo(3));
            Assert.That(line.GetVisibleClusterEnd(), Is.EqualTo(3));
            Assert.That(line.GetContentAdvance(), Is.EqualTo(3));
            Assert.That(line.GetAdvance(), Is.EqualTo(5));
            Assert.That(line.GetInlineOffset(), Is.Zero);
            Assert.That(line.HasEllipsis(), Is.True);
            Assert.That(line.GetEllipsisText(), Is.EqualTo("..."));
        });
    }

    [Test]
    public void WordEllipsisDoesNotSplitAnUnbreakableWord()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.WordEllipsis);

        GreedyLineFitResult result = Fit(
            "abcdef",
            UniformAdvances(6, 1),
            4,
            options,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTruncated(), Is.True);
            Assert.That(result.GetTruncationClusterIndex(), Is.Zero);
            Assert.That(result.GetLine(0).GetContentAdvance(), Is.Zero);
            Assert.That(result.GetLine(0).GetAdvance(), Is.EqualTo(1));
            Assert.That(result.GetLine(0).HasEllipsis(), Is.True);
        });
    }

    [Test]
    public void WordEllipsisRetainsTheLastFittingWholeWord()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.WordEllipsis);

        GreedyLineFitResult result = Fit(
            "aa bbcc",
            UniformAdvances(7, 1),
            4,
            options,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTruncated(), Is.True);
            Assert.That(result.GetTruncationClusterIndex(), Is.EqualTo(3));
            Assert.That(result.GetLine(0).GetVisibleClusterEnd(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetContentAdvance(), Is.EqualTo(2));
            Assert.That(result.GetLine(0).GetAdvance(), Is.EqualTo(3));
        });
    }

    [Test]
    public void MaximumLinesClipsRemainingContentWithoutEllipsisByDefault()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetMaximumLines(2);

        GreedyLineFitResult result = Fit(
            "aa bb cc",
            UniformAdvances(8, 1),
            2,
            options,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetLineCount(), Is.EqualTo(2));
            Assert.That(result.IsTruncated(), Is.True);
            Assert.That(result.GetTruncationClusterIndex(),
                Is.EqualTo(result.GetLine(1).GetClusterEnd()));
            Assert.That(result.GetLine(1).HasEllipsis(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                result.GetLine(1).GetEllipsisText());
        });
    }

    [Test]
    public void HandlesEmptyTextAndValidatesInputsAndOptions()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFitResult empty = Fit(
            string.Empty,
            Array.Empty<double>(),
            10,
            options,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetLineCount(), Is.EqualTo(1));
            Assert.That(empty.GetLine(0).GetAdvance(), Is.Zero);
            Assert.That(empty.IsTruncated(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                empty.GetTruncationClusterIndex());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetLine(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumLines(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetAlignment((TextAlignment)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetTrimmingMode((TextTrimmingMode)99));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetEllipsisText(null!));
            Assert.Throws<ArgumentException>(() =>
                options.SetEllipsisText(string.Empty));
            Assert.Throws<ArgumentNullException>(() =>
                GreedyLineFitter.Fit(null, Array.Empty<double>(), 1, options, 0, 0));
            Assert.Throws<ArgumentNullException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    null,
                    1,
                    options,
                    0,
                    0));
            Assert.Throws<ArgumentNullException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    new double[] { 1 },
                    1,
                    null,
                    0,
                    0));
            Assert.Throws<ArgumentException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    Array.Empty<double>(),
                    1,
                    options,
                    0,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    new double[] { double.NaN },
                    1,
                    options,
                    0,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    new double[] { 1 },
                    -1,
                    options,
                    0,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    new double[] { 1 },
                    1,
                    options,
                    -1,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GreedyLineFitter.Fit(
                    Plan("a", false),
                    new double[] { 1 },
                    1,
                    options,
                    0,
                    double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GreedyLineFitter.Fit(
                    Plan("ab", false),
                    new double[] { double.MaxValue, double.MaxValue },
                    double.MaxValue,
                    options,
                    0,
                    0));
        });
    }

    [Test]
    public void StoresOptionalHyphenationAndEmergencyBreakConfiguration()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        HyphenationDictionary dictionary = HyphenationDictionary.Load(
            System.Text.Encoding.UTF8.GetBytes("a1b\n"));

        options.SetAllowEmergencyBreaks(true);
        options.SetHyphenation(dictionary);

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowsEmergencyBreaks(), Is.True);
            Assert.That(options.HasHyphenation(), Is.True);
            Assert.That(options.GetHyphenation(), Is.SameAs(dictionary));
        });

        options.ClearHyphenation();
        Assert.That(options.HasHyphenation(), Is.False);
        Assert.Throws<InvalidOperationException>(() => options.GetHyphenation());
        Assert.Throws<ArgumentNullException>(() => options.SetHyphenation(null!));
    }

    static GreedyLineFitResult Fit(
        string text,
        double[] advances,
        double maximumAdvance,
        TextLayoutOptions options,
        double hyphenAdvance,
        double ellipsisAdvance)
    {
        return GreedyLineFitter.Fit(
            Plan(text, options.AllowsEmergencyBreaks()),
            advances,
            maximumAdvance,
            options,
            hyphenAdvance,
            ellipsisAdvance);
    }

    static TextBreakMap Plan(string text, bool emergency)
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
            emergency,
            clusters.GetClusterCount());
    }

    static double[] UniformAdvances(int count, double value)
    {
        double[] advances = new double[count];
        int index = 0;
        while (index < count)
        {
            advances[index] = value;
            index++;
        }

        return advances;
    }
}

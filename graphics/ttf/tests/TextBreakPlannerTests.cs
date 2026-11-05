namespace ttf.Tests;

public class TextBreakPlannerTests
{
    [Test]
    public void IdentifiesControlsAndKeepsBreakKindsDistinct()
    {
        TextBreakMap result = Plan(
            "ab\u00ADcd\txy\u00A0z\r\nq",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetClusterCount(), Is.EqualTo(12));
            Assert.That(result.GetSourceLength(), Is.EqualTo(13));
            Assert.That(result.IsSoftHyphen(2), Is.True);
            Assert.That(result.IsTab(5), Is.True);
            Assert.That(result.IsNonbreaking(8), Is.True);
            Assert.That(result.IsHardBreak(10), Is.True);
            Assert.That(result.GetUtf16Length(10), Is.EqualTo(2));
            Assert.That(result.GetBreak(0),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(1),
                Is.EqualTo(TextBreakKind.Emergency));
            Assert.That(result.RequiresHyphen(3), Is.True);
            Assert.That(result.GetBreak(6),
                Is.EqualTo(TextBreakKind.Ordinary));
            Assert.That(result.GetBreak(8),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(9),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(10),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(11),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(result.GetBreak(12),
                Is.EqualTo(TextBreakKind.Mandatory));
        });
    }

    [Test]
    public void MakesEmergencyBreakingExplicitlyOptional()
    {
        TextBreakMap ordinary = Plan("abcd", false);
        TextBreakMap emergency = Plan("abcd", true);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.CanBreak(1), Is.False);
            Assert.That(ordinary.CanBreak(2), Is.False);
            Assert.That(ordinary.CanBreak(3), Is.False);
            Assert.That(emergency.IsEmergency(1), Is.True);
            Assert.That(emergency.IsEmergency(2), Is.True);
            Assert.That(emergency.IsEmergency(3), Is.True);
        });
    }

    [Test]
    public void PreservesEveryHardAndNonbreakingControl()
    {
        TextBreakMap hard = Plan(
            "\u000B" + "A" + "\u000C" + "B" + "\u0085" + "C" +
            "\u2028" + "D" + "\u2029" + "E",
            true);
        TextBreakMap nonbreaking = Plan(
            "a\u2060b\uFEFFc\u2011d",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(hard.IsHardBreak(0), Is.True);
            Assert.That(hard.IsHardBreak(2), Is.True);
            Assert.That(hard.IsHardBreak(4), Is.True);
            Assert.That(hard.IsHardBreak(6), Is.True);
            Assert.That(hard.IsHardBreak(8), Is.True);
            Assert.That(hard.GetBreak(1),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(hard.GetBreak(3),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(hard.GetBreak(5),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(hard.GetBreak(7),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(hard.GetBreak(9),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.That(nonbreaking.IsNonbreaking(1), Is.True);
            Assert.That(nonbreaking.IsNonbreaking(3), Is.True);
            Assert.That(nonbreaking.IsNonbreaking(5), Is.True);
            Assert.That(nonbreaking.GetBreak(1),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(nonbreaking.GetBreak(2),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(nonbreaking.GetBreak(3),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(nonbreaking.GetBreak(4),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(nonbreaking.GetBreak(5),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(nonbreaking.GetBreak(6),
                Is.EqualTo(TextBreakKind.Prohibited));
        });
    }

    [Test]
    public void SuppressesOrdinaryAndEmergencyBreaksAtUnsafeShapingRanges()
    {
        RuneBuffer input = Decode("a bc");
        UnicodeData data = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = Segment(input, data);
        LineBreakMap unicodeBreaks = LineBreakAlgorithm.Analyze(
            input,
            data,
            input.GetCount());
        ShapingBuffer buffer = new ShapingBuffer(
            input,
            ShapingDirection.LeftToRight);
        int index = 0;
        while (index < buffer.GetCount())
        {
            buffer.SetGlyphId(index, index + 1);
            index++;
        }

        buffer.SetUnsafeToBreak(2, true);
        buffer.MergeClusters(2, 4);
        ShapedRun run = ShapedRunBuilder.Build(buffer, input);
        TextBreakMap result = TextBreakPlanner.Plan(
            input,
            clusters,
            unicodeBreaks,
            new ShapedRun[] { run },
            true,
            clusters.GetClusterCount());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetBreak(2),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(3),
                Is.EqualTo(TextBreakKind.Prohibited));
            Assert.That(result.GetBreak(4),
                Is.EqualTo(TextBreakKind.Mandatory));
        });
    }

    [Test]
    public void HandlesEmptyTextAndValidatesInputsAndLimits()
    {
        RuneBuffer input = Decode("ab");
        UnicodeData data = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = Segment(input, data);
        LineBreakMap breaks = LineBreakAlgorithm.Analyze(input, data, 2);
        TextBreakMap result = TextBreakPlanner.Plan(
            input,
            clusters,
            breaks,
            false,
            2);
        TextBreakMap empty = Plan(string.Empty, false);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetClusterCount(), Is.Zero);
            Assert.That(empty.GetBreak(0),
                Is.EqualTo(TextBreakKind.Mandatory));
            Assert.Throws<ArgumentNullException>(() =>
                TextBreakPlanner.Plan(null!, clusters, breaks, false, 2));
            Assert.Throws<ArgumentNullException>(() =>
                TextBreakPlanner.Plan(input, null!, breaks, false, 2));
            Assert.Throws<ArgumentNullException>(() =>
                TextBreakPlanner.Plan(input, clusters, null!, false, 2));
            Assert.Throws<ArgumentNullException>(() =>
                TextBreakPlanner.Plan(
                    input,
                    clusters,
                    breaks,
                    null!,
                    false,
                    2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TextBreakPlanner.Plan(input, clusters, breaks, false, -1));
            Assert.Throws<FontLimitException>(() =>
                TextBreakPlanner.Plan(input, clusters, breaks, false, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetRuneStart(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetBreak(3));
        });
    }

    static TextBreakMap Plan(string text, bool allowEmergencyBreaks)
    {
        RuneBuffer input = Decode(text);
        UnicodeData data = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = Segment(input, data);
        LineBreakMap lineBreaks = LineBreakAlgorithm.Analyze(
            input,
            data,
            input.GetCount());
        return TextBreakPlanner.Plan(
            input,
            clusters,
            lineBreaks,
            allowEmergencyBreaks,
            clusters.GetClusterCount());
    }

    static GraphemeClusterMap Segment(
        RuneBuffer input,
        UnicodeData data)
    {
        return GraphemeSegmenter.Segment(
            input,
            data,
            input.GetCount(),
            input.GetCount());
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }
}

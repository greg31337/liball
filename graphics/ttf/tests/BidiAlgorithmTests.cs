namespace ttf.Tests;

public class BidiAlgorithmTests
{
    [Test]
    public void ResolvesMixedTextAndBuildsVisualRuns()
    {
        BidiResult result = Resolve(
            "abc \u05D0\u05D1\u05D2",
            BidiParagraphDirection.Auto,
            7,
            2);

        Assert.That(result.GetParagraphLevel(), Is.Zero);
        Assert.That(Levels(result), Is.EqualTo(new int[]
        {
            0, 0, 0, 0, 1, 1, 1
        }));
        Assert.That(VisualText(result), Is.EqualTo("abc \u05D2\u05D1\u05D0"));
        Assert.That(LogicalOrder(result), Is.EqualTo(new int[]
        {
            0, 1, 2, 3, 6, 5, 4
        }));
        Assert.That(result.GetRunCount(), Is.EqualTo(2));
        AssertRun(result, 0, 0, 4, 0, false);
        AssertRun(result, 1, 4, 3, 1, true);
    }

    [Test]
    public void HonorsParagraphDirectionOverridesAndResetsTrailingWhitespace()
    {
        BidiResult forced = Resolve(
            "abc \u05D0\u05D1\u05D2",
            BidiParagraphDirection.RightToLeft,
            7,
            3);
        BidiResult trailing = Resolve(
            "\u05D0\u05D1 ",
            BidiParagraphDirection.Auto,
            3,
            1);

        Assert.That(forced.GetParagraphLevel(), Is.EqualTo(1));
        Assert.That(Levels(forced), Is.EqualTo(new int[]
        {
            2, 2, 2, 1, 1, 1, 1
        }));
        Assert.That(VisualText(forced), Is.EqualTo("\u05D2\u05D1\u05D0 abc"));
        Assert.That(Levels(trailing), Is.EqualTo(new int[] { 1, 1, 1 }));
        Assert.That(VisualText(trailing), Is.EqualTo(" \u05D1\u05D0"));
    }

    [Test]
    public void AppliesExplicitEmbeddingsOverridesAndX9Removal()
    {
        BidiResult result = Resolve(
            "a\u202Ebc\u202Cd",
            BidiParagraphDirection.LeftToRight,
            6,
            3);
        BidiResult separatedRuns = Resolve(
            "\u05D0\u202A\u05D1\u202C 1 2",
            BidiParagraphDirection.LeftToRight,
            8,
            4);

        Assert.That(VisualText(result), Is.EqualTo("acbd"));
        Assert.That(result.GetVisualRuneCount(), Is.EqualTo(4));
        Assert.That(result.IsRemovedFromVisualOrder(1), Is.True);
        Assert.That(result.IsRemovedFromVisualOrder(4), Is.True);
        Assert.That(result.GetVisualIndex(1), Is.EqualTo(-1));
        Assert.That(result.GetLevel(2), Is.EqualTo(1));
        Assert.That(result.GetLevel(3), Is.EqualTo(1));
        Assert.That(result.GetResolvedClass(2), Is.EqualTo(BidiClass.R));
        Assert.That(result.GetResolvedClass(3), Is.EqualTo(BidiClass.R));
        Assert.That(Levels(separatedRuns), Is.EqualTo(new int[]
        {
            1, 0, 3, 2, 0, 0, 0, 0
        }));
        Assert.That(LogicalOrder(separatedRuns), Is.EqualTo(new int[]
        {
            2, 0, 4, 5, 6, 7
        }));
    }

    [Test]
    public void KeepsIsolatedTextOutOfItsSurroundingRun()
    {
        BidiResult rightIsolate = Resolve(
            "A\u2067\u05D0\u05D1\u2069B",
            BidiParagraphDirection.Auto,
            6,
            5);
        BidiResult firstStrongIsolate = Resolve(
            "A\u2068\u05D0\u05D1\u2069B",
            BidiParagraphDirection.Auto,
            6,
            5);
        BidiResult segmentInIsolate = Resolve(
            "\u2066\t\u2069\u0660",
            BidiParagraphDirection.Auto,
            4,
            4);

        Assert.That(LogicalOrder(rightIsolate), Is.EqualTo(new int[]
        {
            0, 1, 3, 2, 4, 5
        }));
        Assert.That(LogicalOrder(firstStrongIsolate), Is.EqualTo(
            LogicalOrder(rightIsolate)));
        Assert.That(Levels(rightIsolate), Is.EqualTo(new int[]
        {
            0, 0, 1, 1, 0, 0
        }));
        Assert.That(rightIsolate.GetResolvedClass(1), Is.EqualTo(BidiClass.L));
        Assert.That(rightIsolate.GetResolvedClass(4), Is.EqualTo(BidiClass.L));
        Assert.That(Levels(segmentInIsolate), Is.EqualTo(new int[]
        {
            0, 0, 0, 2
        }));
    }

    [Test]
    public void ResolvesWeakNumbersAndNeutralSeparators()
    {
        BidiResult arabic = Resolve(
            "A \u0627 12",
            BidiParagraphDirection.Auto,
            6,
            4);
        BidiResult european = Resolve(
            "A 1,2 $3",
            BidiParagraphDirection.LeftToRight,
            8,
            1);

        Assert.That(Levels(arabic), Is.EqualTo(new int[]
        {
            0, 0, 1, 1, 2, 2
        }));
        Assert.That(arabic.GetResolvedClass(4), Is.EqualTo(BidiClass.AN));
        Assert.That(arabic.GetResolvedClass(5), Is.EqualTo(BidiClass.AN));
        Assert.That(VisualText(arabic), Is.EqualTo("A 12 \u0627"));
        Assert.That(VisualText(european), Is.EqualTo("A 1,2 $3"));
        Assert.That(european.GetResolvedClass(3), Is.EqualTo(BidiClass.L));
        Assert.That(european.GetResolvedClass(6), Is.EqualTo(BidiClass.L));
    }

    [Test]
    public void ResolvesPairedBracketsAndAppliesMirroringAtOddLevels()
    {
        BidiResult result = Resolve(
            "\u05D0\u05D1(abc)\u05D2",
            BidiParagraphDirection.Auto,
            8,
            3);

        Assert.That(Levels(result), Is.EqualTo(new int[]
        {
            1, 1, 1, 2, 2, 2, 1, 1
        }));
        Assert.That(result.GetResolvedClass(2), Is.EqualTo(BidiClass.R));
        Assert.That(result.GetResolvedClass(6), Is.EqualTo(BidiClass.R));
        Assert.That(VisualText(result), Is.EqualTo("\u05D2(abc)\u05D1\u05D0"));
        Assert.That(result.GetVisualCodePoint(1), Is.EqualTo('('));
        Assert.That(result.GetVisualCodePoint(5), Is.EqualTo(')'));
    }

    [Test]
    public void PreservesUtf16RangesAndSeparatesParagraphs()
    {
        BidiResult ranges = Resolve(
            "\u05D0\U0001F600\u05D1",
            BidiParagraphDirection.RightToLeft,
            3,
            3);
        BidiResult paragraphs = Resolve(
            "A\n\u05D0",
            BidiParagraphDirection.Auto,
            3,
            2);

        Assert.That(ranges.GetSourceLength(), Is.EqualTo(4));
        Assert.That(ranges.GetVisualUtf16Start(0), Is.EqualTo(3));
        Assert.That(ranges.GetVisualUtf16Length(1), Is.EqualTo(2));
        Assert.That(ranges.GetVisualUtf16Start(2), Is.Zero);
        Assert.That(paragraphs.GetParagraphLevelAtIndex(0), Is.Zero);
        Assert.That(paragraphs.GetParagraphLevelAtIndex(1), Is.Zero);
        Assert.That(paragraphs.GetParagraphLevelAtIndex(2), Is.EqualTo(1));
        Assert.That(LogicalOrder(paragraphs), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(paragraphs.GetRunCount(), Is.EqualTo(2));
    }

    [Test]
    public void HandlesDepthOverflowAndEnforcesResourceLimits()
    {
        string controls = new string('\u202B', 130) +
            "A" + new string('\u202C', 130);
        BidiResult overflow = Resolve(
            controls,
            BidiParagraphDirection.LeftToRight,
            controls.Length,
            1);

        Assert.That(VisualText(overflow), Is.EqualTo("A"));
        Assert.That(overflow.GetLevel(130), Is.LessThanOrEqualTo(126));
        Assert.That(Assert.Throws<FontLimitException>(() => Resolve(
            "AB",
            BidiParagraphDirection.Auto,
            1,
            1))!.Message, Is.EqualTo(
            "Bidirectional resolution exceeds the configured rune limit."));
        Assert.That(Assert.Throws<FontLimitException>(() => Resolve(
            "A\u05D0",
            BidiParagraphDirection.Auto,
            2,
            1))!.Message, Is.EqualTo(
            "Bidirectional resolution exceeds the configured visual run limit."));
    }

    [Test]
    public void ValidatesArgumentsAndResultIndexes()
    {
        RuneBuffer input = Decode("A");
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        BidiResult result = BidiAlgorithm.Resolve(
            input,
            unicodeData,
            BidiParagraphDirection.Auto,
            1,
            1);

        Assert.Throws<ArgumentNullException>(() => BidiAlgorithm.Resolve(
            null!,
            unicodeData,
            BidiParagraphDirection.Auto,
            1,
            1));
        Assert.Throws<ArgumentNullException>(() => BidiAlgorithm.Resolve(
            input,
            null!,
            BidiParagraphDirection.Auto,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BidiAlgorithm.Resolve(
            input,
            unicodeData,
            (BidiParagraphDirection)4,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BidiAlgorithm.Resolve(
            input,
            unicodeData,
            BidiParagraphDirection.Auto,
            -1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BidiAlgorithm.Resolve(
            input,
            unicodeData,
            BidiParagraphDirection.Auto,
            1,
            -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetLevel(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetLogicalIndex(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRunLevel(1));
    }

    static BidiResult Resolve(
        string text,
        BidiParagraphDirection direction,
        int maximumRuneCount,
        int maximumRunCount)
    {
        return BidiAlgorithm.Resolve(
            Decode(text),
            UnicodeData.LoadDefault(),
            direction,
            maximumRuneCount,
            maximumRunCount);
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static int[] Levels(BidiResult result)
    {
        int[] levels = new int[result.GetRuneCount()];
        for (int index = 0; index < levels.Length; index++)
        {
            levels[index] = result.GetLevel(index);
        }

        return levels;
    }

    static int[] LogicalOrder(BidiResult result)
    {
        int[] order = new int[result.GetVisualRuneCount()];
        for (int index = 0; index < order.Length; index++)
        {
            order[index] = result.GetLogicalIndex(index);
        }

        return order;
    }

    static string VisualText(BidiResult result)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < result.GetVisualRuneCount(); index++)
        {
            builder.Append(new System.Text.Rune(
                result.GetVisualCodePoint(index)).ToString());
        }

        return builder.ToString();
    }

    static void AssertRun(
        BidiResult result,
        int runIndex,
        int visualStart,
        int visualLength,
        int level,
        bool rightToLeft)
    {
        Assert.That(result.GetRunVisualStart(runIndex), Is.EqualTo(visualStart));
        Assert.That(result.GetRunVisualLength(runIndex), Is.EqualTo(visualLength));
        Assert.That(result.GetRunVisualEnd(runIndex),
            Is.EqualTo(visualStart + visualLength));
        Assert.That(result.GetRunLevel(runIndex), Is.EqualTo(level));
        Assert.That(result.IsRunRightToLeft(runIndex), Is.EqualTo(rightToLeft));
    }
}

namespace ttf.Tests;

public class ArabicMarkTransientReordererTests
{
    [Test]
    public void MovesBelowAboveAndShaddaMarksInTransientOrder()
    {
        RuneBuffer input = Decode(
            "\u0628\u064E\u0651\u0655\u0654");

        RuneBuffer result = Reorder(input, 5);

        Assert.That(ToText(result),
            Is.EqualTo("\u0628\u0655\u0654\u0651\u064E"));
        AssertRange(result, 0, 0, 1);
        AssertRange(result, 1, 3, 1);
        AssertRange(result, 2, 4, 1);
        AssertRange(result, 3, 2, 1);
        AssertRange(result, 4, 1, 1);
        Assert.That(result.GetSourceLength(), Is.EqualTo(5));

        Assert.That(ToText(input),
            Is.EqualTo("\u0628\u064E\u0651\u0655\u0654"));
        AssertRange(input, 1, 1, 1);
        AssertRange(input, 4, 4, 1);
    }

    [Test]
    public void MovesOnlyLeadingModifierMarksOfEachCombiningClass()
    {
        RuneBuffer leadingModifiers = Decode(
            "\u0628\u064E\u0655\u06E3\u0654\u0658");
        RuneBuffer blockedModifiers = Decode(
            "\u0628\u064E\u0656\u0655\u0653\u0654");

        RuneBuffer moved = Reorder(leadingModifiers, 6);
        RuneBuffer blocked = Reorder(blockedModifiers, 6);

        Assert.That(ToText(moved),
            Is.EqualTo("\u0628\u0655\u06E3\u0654\u0658\u064E"));
        Assert.That(ToText(blocked),
            Is.EqualTo("\u0628\u064E\u0656\u0655\u0653\u0654"));
    }

    [Test]
    public void TreatsCombiningClassZeroAsAReorderingBoundary()
    {
        RuneBuffer input = Decode(
            "\u0628\u064E\u034F\u0651\u0654");

        RuneBuffer result = Reorder(input, 5);

        Assert.That(ToText(result),
            Is.EqualTo("\u0628\u064E\u034F\u0654\u0651"));
        AssertRange(result, 1, 1, 1);
        AssertRange(result, 2, 2, 1);
        AssertRange(result, 3, 4, 1);
        AssertRange(result, 4, 3, 1);
    }

    [Test]
    public void DecomposesToNfdAndKeepsOriginalSourceRanges()
    {
        RuneBuffer input = Decode("\u0622\u064E");

        RuneBuffer result = Reorder(input, 3);

        Assert.That(ToText(result), Is.EqualTo("\u0627\u064E\u0653"));
        AssertRange(result, 0, 0, 1);
        AssertRange(result, 1, 1, 1);
        AssertRange(result, 2, 0, 1);
        Assert.That(result.GetSourceLength(), Is.EqualTo(2));
    }

    [Test]
    public void PreservesReplacementMetadata()
    {
        string malformed = new string(new char[]
        {
            '\uD800', '\u0628', '\u064E', '\u0651'
        });
        RuneBuffer input = Utf16Decoder.Decode(
            malformed,
            Utf16DecodingMode.Replacement,
            4);

        RuneBuffer result = Reorder(input, 4);

        Assert.That(ToText(result),
            Is.EqualTo("\uFFFD\u0628\u0651\u064E"));
        Assert.That(result.GetReplacementCount(), Is.EqualTo(1));
        Assert.That(result.HadReplacements(), Is.True);
        AssertRange(result, 0, 0, 1);
    }

    [Test]
    public void EnforcesNormalizationLimitsAndValidatesArguments()
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        RuneBuffer input = Decode("\u0622");

        Assert.Throws<FontLimitException>(() =>
            ArabicMarkTransientReorderer.Reorder(input, unicodeData, 1));
        Assert.Throws<ArgumentNullException>(() =>
            ArabicMarkTransientReorderer.Reorder(null!, unicodeData, 1));
        Assert.Throws<ArgumentNullException>(() =>
            ArabicMarkTransientReorderer.Reorder(input, null!, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArabicMarkTransientReorderer.Reorder(input, unicodeData, -1));

        RuneBuffer empty = Reorder(Decode(""), 0);
        Assert.That(empty.GetCount(), Is.Zero);
        Assert.That(empty.GetSourceLength(), Is.Zero);
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static RuneBuffer Reorder(RuneBuffer input, int maximumRuneCount)
    {
        return ArabicMarkTransientReorderer.Reorder(
            input,
            UnicodeData.LoadDefault(),
            maximumRuneCount);
    }

    static string ToText(RuneBuffer buffer)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < buffer.GetCount())
        {
            builder.Append(buffer.GetRune(index).ToString());
            index++;
        }

        return builder.ToString();
    }

    static void AssertRange(
        RuneBuffer buffer,
        int index,
        int start,
        int length)
    {
        Assert.That(buffer.GetUtf16Start(index), Is.EqualTo(start));
        Assert.That(buffer.GetUtf16Length(index), Is.EqualTo(length));
    }
}

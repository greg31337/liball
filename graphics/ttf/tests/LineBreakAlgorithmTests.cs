namespace ttf.Tests;

public class LineBreakAlgorithmTests
{
    [Test]
    public void ClassifiesTextAndDistinguishesAllowedAndMandatoryBreaks()
    {
        LineBreakMap result = Analyze("a b\r\nc", 6);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRuneCount(), Is.EqualTo(6));
            Assert.That(result.GetSourceLength(), Is.EqualTo(6));
            Assert.That(result.GetClass(0),
                Is.EqualTo(LineBreakClass.Alphabetic));
            Assert.That(result.GetClass(1), Is.EqualTo(LineBreakClass.Space));
            Assert.That(result.CanBreak(0), Is.False);
            Assert.That(result.CanBreak(1), Is.False);
            Assert.That(result.GetOpportunity(2),
                Is.EqualTo(LineBreakOpportunity.Allowed));
            Assert.That(result.CanBreak(3), Is.False);
            Assert.That(result.CanBreak(4), Is.False);
            Assert.That(result.IsMandatory(5), Is.True);
            Assert.That(result.IsMandatory(6), Is.True);
        });
    }

    [Test]
    public void HandlesCombiningZeroWidthRegionalAndEmojiSequences()
    {
        LineBreakMap combining = Analyze("A\u0301B", 3);
        LineBreakMap zeroWidth = Analyze("A\u200B B", 4);
        LineBreakMap regional = Analyze(
            "\U0001F1E6\U0001F1E7\U0001F1E8",
            3);
        LineBreakMap emoji = Analyze("\U0001F02C\U0001F3FF", 2);

        Assert.Multiple(() =>
        {
            Assert.That(combining.GetClass(1),
                Is.EqualTo(LineBreakClass.Alphabetic));
            Assert.That(combining.CanBreak(1), Is.False);
            Assert.That(combining.CanBreak(2), Is.False);
            Assert.That(zeroWidth.CanBreak(2), Is.False);
            Assert.That(zeroWidth.CanBreak(3), Is.True);
            Assert.That(regional.CanBreak(1), Is.False);
            Assert.That(regional.CanBreak(2), Is.True);
            Assert.That(emoji.CanBreak(1), Is.False);
        });
    }

    [Test]
    public void ResolvesComplexContextClassesAndValidatesInputs()
    {
        RuneBuffer input = Decode("\u0E01\u0E31");
        UnicodeData data = UnicodeData.LoadDefault();
        LineBreakMap result = LineBreakAlgorithm.Analyze(input, data, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetClass(0),
                Is.EqualTo(LineBreakClass.Alphabetic));
            Assert.That(result.GetClass(1),
                Is.EqualTo(LineBreakClass.Alphabetic));
            Assert.That(result.CanBreak(1), Is.False);
            Assert.Throws<ArgumentNullException>(() =>
                LineBreakAlgorithm.Analyze(null!, data, 1));
            Assert.Throws<ArgumentNullException>(() =>
                LineBreakAlgorithm.Analyze(input, null!, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LineBreakAlgorithm.Analyze(input, data, -1));
            Assert.Throws<FontLimitException>(() =>
                LineBreakAlgorithm.Analyze(input, data, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetClass(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetOpportunity(3));
        });
    }

    static LineBreakMap Analyze(string text, int maximumRuneCount)
    {
        return LineBreakAlgorithm.Analyze(
            Decode(text),
            UnicodeData.LoadDefault(),
            maximumRuneCount);
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }
}

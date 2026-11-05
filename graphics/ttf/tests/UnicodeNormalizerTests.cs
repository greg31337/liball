namespace ttf.Tests;

public class UnicodeNormalizerTests
{
    [Test]
    public void CanonicalDecompositionCopiesTheOriginalRangeToEveryRune()
    {
        RuneBuffer input = Decode("X\u00C5\U0001F600");

        RuneBuffer result = Normalize(
            input,
            UnicodeNormalizationMode.Nfd,
            4);

        Assert.That(ToText(result), Is.EqualTo("XA\u030A\U0001F600"));
        Assert.That(result.GetCount(), Is.EqualTo(4));
        AssertRange(result, 0, 0, 1);
        AssertRange(result, 1, 1, 1);
        AssertRange(result, 2, 1, 1);
        AssertRange(result, 3, 2, 2);
        Assert.That(result.GetSourceLength(), Is.EqualTo(4));
    }

    [Test]
    public void CanonicalOrderingMovesEachRunesOriginalRangeWithIt()
    {
        RuneBuffer input = Decode("A\u0315\u0300");

        RuneBuffer result = Normalize(
            input,
            UnicodeNormalizationMode.Nfd,
            3);

        Assert.That(ToText(result), Is.EqualTo("A\u0300\u0315"));
        AssertRange(result, 0, 0, 1);
        AssertRange(result, 1, 2, 1);
        AssertRange(result, 2, 1, 1);
    }

    [Test]
    public void CanonicalCompositionMergesContributingSourceRanges()
    {
        RuneBuffer adjacent = Normalize(
            Decode("A\u030A"),
            UnicodeNormalizationMode.Nfc,
            2);
        RuneBuffer reordered = Normalize(
            Decode("A\u0327\u0301"),
            UnicodeNormalizationMode.Nfc,
            3);
        RuneBuffer chained = Normalize(
            Decode("A\u030A\u0301"),
            UnicodeNormalizationMode.Nfc,
            3);

        Assert.That(ToText(adjacent), Is.EqualTo("\u00C5"));
        AssertRange(adjacent, 0, 0, 2);
        Assert.That(ToText(reordered), Is.EqualTo("\u00C1\u0327"));
        AssertRange(reordered, 0, 0, 3);
        AssertRange(reordered, 1, 1, 1);
        Assert.That(ToText(chained), Is.EqualTo("\u01FA"));
        AssertRange(chained, 0, 0, 3);
    }

    [Test]
    public void SupportsCompatibilityAndHangulNormalization()
    {
        RuneBuffer compatibilityDecomposition = Normalize(
            Decode("\uFB03"),
            UnicodeNormalizationMode.Nfkd,
            3);
        RuneBuffer compatibilityComposition = Normalize(
            Decode("\uFB03"),
            UnicodeNormalizationMode.Nfkc,
            3);
        RuneBuffer hangulDecomposition = Normalize(
            Decode("\uAC01"),
            UnicodeNormalizationMode.Nfd,
            3);
        RuneBuffer hangulComposition = Normalize(
            Decode("\u1100\u1161\u11A8"),
            UnicodeNormalizationMode.Nfc,
            3);

        Assert.That(ToText(compatibilityDecomposition), Is.EqualTo("ffi"));
        Assert.That(ToText(compatibilityComposition), Is.EqualTo("ffi"));
        AssertRange(compatibilityDecomposition, 0, 0, 1);
        AssertRange(compatibilityDecomposition, 1, 0, 1);
        AssertRange(compatibilityDecomposition, 2, 0, 1);
        Assert.That(ToText(hangulDecomposition),
            Is.EqualTo("\u1100\u1161\u11A8"));
        AssertRange(hangulDecomposition, 0, 0, 1);
        AssertRange(hangulDecomposition, 1, 0, 1);
        AssertRange(hangulDecomposition, 2, 0, 1);
        Assert.That(ToText(hangulComposition), Is.EqualTo("\uAC01"));
        AssertRange(hangulComposition, 0, 0, 3);
    }

    [Test]
    public void PreservesReplacementMetadataAndSupportsNoNormalization()
    {
        string malformed = new string(new char[] { 'A', '\uD800' });
        RuneBuffer input = Utf16Decoder.Decode(
            malformed,
            Utf16DecodingMode.Replacement,
            2);

        RuneBuffer normalized = Normalize(
            input,
            UnicodeNormalizationMode.Nfc,
            2);
        RuneBuffer unchanged = Normalize(
            input,
            UnicodeNormalizationMode.None,
            2);

        Assert.That(ToText(normalized), Is.EqualTo("A\uFFFD"));
        Assert.That(normalized.GetReplacementCount(), Is.EqualTo(1));
        Assert.That(normalized.HadReplacements(), Is.True);
        AssertRange(normalized, 1, 1, 1);
        Assert.That(unchanged, Is.SameAs(input));
    }

    [Test]
    public void EnforcesIntermediateRuneLimits()
    {
        RuneBuffer compatibilityLigature = Decode("\uFB03");
        RuneBuffer twoRunes = Decode("AB");

        FontLimitException? exception = Assert.Throws<FontLimitException>(() =>
            Normalize(
                compatibilityLigature,
                UnicodeNormalizationMode.Nfkc,
                2));
        Assert.Throws<FontLimitException>(() => Normalize(
            twoRunes,
            UnicodeNormalizationMode.None,
            1));

        Assert.That(exception!.Message, Is.EqualTo(
            "Unicode normalization exceeds the configured rune limit."));
    }

    [Test]
    public void ValidatesArguments()
    {
        RuneBuffer input = Decode("A");
        UnicodeData unicodeData = UnicodeData.LoadDefault();

        Assert.Throws<ArgumentNullException>(() => UnicodeNormalizer.Normalize(
            null!,
            unicodeData,
            UnicodeNormalizationMode.Nfc,
            1));
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalizer.Normalize(
            input,
            null!,
            UnicodeNormalizationMode.Nfc,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UnicodeNormalizer.Normalize(
                input,
                unicodeData,
                (UnicodeNormalizationMode)5,
                1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UnicodeNormalizer.Normalize(
                input,
                unicodeData,
                UnicodeNormalizationMode.Nfc,
                -1));
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static RuneBuffer Normalize(
        RuneBuffer input,
        UnicodeNormalizationMode mode,
        int maximumRuneCount)
    {
        return UnicodeNormalizer.Normalize(
            input,
            UnicodeData.LoadDefault(),
            mode,
            maximumRuneCount);
    }

    static string ToText(RuneBuffer buffer)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;

        while (index < buffer.GetCount())
        {
            text.Append(buffer.GetRune(index).ToString());
            index++;
        }

        return text.ToString();
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

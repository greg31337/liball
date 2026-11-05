namespace ttf.Tests;

public class Utf16DecoderTests
{
    [Test]
    public void DecodesBmpAndSupplementaryRunesWithOriginalRanges()
    {
        RuneBuffer result = Utf16Decoder.Decode(
            "A\U0001F600\u0301",
            Utf16DecodingMode.Strict,
            3);

        Assert.That(result.GetCount(), Is.EqualTo(3));
        Assert.That(result.GetSourceLength(), Is.EqualTo(4));
        Assert.That(result.GetScalarValue(0), Is.EqualTo(0x41));
        Assert.That(result.GetScalarValue(1), Is.EqualTo(0x1F600));
        Assert.That(result.GetRune(2).Value, Is.EqualTo(0x301));
        Assert.That(result.GetUtf16Start(0), Is.EqualTo(0));
        Assert.That(result.GetUtf16Length(0), Is.EqualTo(1));
        Assert.That(result.GetUtf16Start(1), Is.EqualTo(1));
        Assert.That(result.GetUtf16Length(1), Is.EqualTo(2));
        Assert.That(result.GetUtf16End(1), Is.EqualTo(3));
        Assert.That(result.GetUtf16Start(2), Is.EqualTo(3));
        Assert.That(result.GetUtf16Length(2), Is.EqualTo(1));
        Assert.That(result.GetReplacementCount(), Is.Zero);
        Assert.That(result.HadReplacements(), Is.False);
    }

    [Test]
    public void ReplacementModeReplacesUnpairedSurrogatesAndRetainsOffsets()
    {
        string text = new string(new char[]
        {
            '\uD800',
            'A',
            '\uDC00',
            '\uD801',
            '\uD83D',
            '\uDE00'
        });

        RuneBuffer result = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Replacement,
            5);

        Assert.That(result.GetCount(), Is.EqualTo(5));
        Assert.That(result.GetSourceLength(), Is.EqualTo(6));
        Assert.That(result.GetScalarValue(0), Is.EqualTo(0xFFFD));
        Assert.That(result.GetScalarValue(1), Is.EqualTo(0x41));
        Assert.That(result.GetScalarValue(2), Is.EqualTo(0xFFFD));
        Assert.That(result.GetScalarValue(3), Is.EqualTo(0xFFFD));
        Assert.That(result.GetScalarValue(4), Is.EqualTo(0x1F600));
        Assert.That(result.GetUtf16Start(0), Is.EqualTo(0));
        Assert.That(result.GetUtf16Start(1), Is.EqualTo(1));
        Assert.That(result.GetUtf16Start(2), Is.EqualTo(2));
        Assert.That(result.GetUtf16Start(3), Is.EqualTo(3));
        Assert.That(result.GetUtf16Start(4), Is.EqualTo(4));
        Assert.That(result.GetUtf16Length(3), Is.EqualTo(1));
        Assert.That(result.GetUtf16Length(4), Is.EqualTo(2));
        Assert.That(result.GetReplacementCount(), Is.EqualTo(3));
        Assert.That(result.HadReplacements(), Is.True);
    }

    [Test]
    public void StrictModeRejectsEveryUnpairedSurrogateForm()
    {
        string leadingLow = new string(new char[] { '\uDC00', 'A' });
        string trailingHigh = new string(new char[] { 'A', '\uD800' });
        string interruptedPair = new string(new char[] { '\uD800', 'A' });

        System.Text.DecoderFallbackException? leadingException =
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                Utf16Decoder.Decode(leadingLow, Utf16DecodingMode.Strict, 2));
        System.Text.DecoderFallbackException? trailingException =
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                Utf16Decoder.Decode(trailingHigh, Utf16DecodingMode.Strict, 2));
        System.Text.DecoderFallbackException? interruptedException =
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                Utf16Decoder.Decode(interruptedPair, Utf16DecodingMode.Strict, 2));

        Assert.That(leadingException!.Message, Does.EndWith("Index: 0."));
        Assert.That(trailingException!.Message, Does.EndWith("Index: 1."));
        Assert.That(interruptedException!.Message, Does.EndWith("Index: 0."));
    }

    [Test]
    public void EnforcesTheDecodedRuneLimitRatherThanTheUtf16Length()
    {
        string text = "\U0001F600\U0001F642A";

        RuneBuffer result = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            3);
        FontLimitException? exception = Assert.Throws<FontLimitException>(() =>
            Utf16Decoder.Decode(text, Utf16DecodingMode.Strict, 2));

        Assert.That(result.GetCount(), Is.EqualTo(3));
        Assert.That(result.GetSourceLength(), Is.EqualTo(5));
        Assert.That(exception!.Message, Is.EqualTo(
            "UTF-16 decoding exceeds the configured rune limit."));
    }

    [Test]
    public void HandlesEmptyTextWithAZeroRuneLimit()
    {
        RuneBuffer result = Utf16Decoder.Decode(
            string.Empty,
            Utf16DecodingMode.Strict,
            0);

        Assert.That(result.GetCount(), Is.Zero);
        Assert.That(result.GetSourceLength(), Is.Zero);
        Assert.Throws<FontLimitException>(() => Utf16Decoder.Decode(
            "A",
            Utf16DecodingMode.Strict,
            0));
    }

    [Test]
    public void ValidatesArgumentsAndRuneIndexes()
    {
        RuneBuffer result = Utf16Decoder.Decode(
            "A",
            Utf16DecodingMode.Strict,
            1);

        Assert.Throws<ArgumentNullException>(() => Utf16Decoder.Decode(
            null!,
            Utf16DecodingMode.Strict,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Utf16Decoder.Decode(
            "A",
            Utf16DecodingMode.Strict,
            -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Utf16Decoder.Decode(
            "A",
            (Utf16DecodingMode)2,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRune(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUtf16Start(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUtf16Length(1));
    }
}

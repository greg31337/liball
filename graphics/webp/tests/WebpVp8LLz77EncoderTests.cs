using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LLz77EncoderTests
{
    [Test]
    public void EncodePrefixValue_RoundTripsThroughDecoder()
    {
        AssertPrefixRoundTrip(1);
        AssertPrefixRoundTrip(4);
        AssertPrefixRoundTrip(6);
        AssertPrefixRoundTrip(4096);
    }

    [Test]
    public void EncodeLengthSymbol_MapsLengthsToGreenSymbols()
    {
        Assert.That(WebpVp8LLz77Encoder.EncodeLengthSymbol(1), Is.EqualTo(256));
        Assert.That(WebpVp8LLz77Encoder.EncodeLengthSymbol(4), Is.EqualTo(259));
        Assert.That(WebpVp8LLz77Encoder.EncodeLengthSymbol(4096), Is.EqualTo(279));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Encoder.EncodeLengthSymbol(4097));
    }

    [Test]
    public void EncodeDistanceCode_PrefersSpecialCodesAndFallsBackToLinearDistance()
    {
        Assert.That(WebpVp8LLz77Encoder.EncodeDistanceCode(10, 10), Is.EqualTo(1));
        Assert.That(WebpVp8LLz77Encoder.EncodeDistanceCode(1, 10), Is.EqualTo(2));
        Assert.That(WebpVp8LLz77Encoder.EncodeDistanceCode(78, 10), Is.EqualTo(120));
        Assert.That(WebpVp8LLz77Encoder.EncodeDistanceCode(200, 10), Is.EqualTo(320));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(WebpVp8LLz77Encoder.EncodeDistanceCode(200, 10), 10), Is.EqualTo(200));
    }

    [Test]
    public void FindMatches_EmitsLongestBackwardReference()
    {
        WebpVp8LColor a = NewColor(1);
        WebpVp8LColor b = NewColor(2);
        WebpVp8LColor c = NewColor(3);
        WebpVp8LColor[] pixels = new WebpVp8LColor[] { a, b, c, a, b, c, a, b };

        List<WebpVp8LLz77Token> tokens = WebpVp8LLz77Encoder.FindMatches(pixels, 8, 1, 3, 32);

        Assert.That(tokens, Has.Count.EqualTo(4));
        Assert.That(tokens[0].IsLiteral, Is.True);
        Assert.That(tokens[1].IsLiteral, Is.True);
        Assert.That(tokens[2].IsLiteral, Is.True);
        Assert.That(tokens[3].IsLiteral, Is.False);
        Assert.That(tokens[3].Length, Is.EqualTo(5));
        Assert.That(tokens[3].Distance, Is.EqualTo(3));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(tokens[3].DistanceCode, 8), Is.EqualTo(3));
    }

    [Test]
    public void FindMatches_PrefersNearestDistanceForEqualLengthMatches()
    {
        WebpVp8LColor a = NewColor(7);
        WebpVp8LColor[] pixels = new WebpVp8LColor[] { a, a, a, a };

        List<WebpVp8LLz77Token> tokens = WebpVp8LLz77Encoder.FindMatches(pixels, 4, 1, 3, 32);

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[1].Length, Is.EqualTo(3));
        Assert.That(tokens[1].Distance, Is.EqualTo(1));
    }

    [Test]
    public void FindMatches_LeavesShortMatchesAsLiterals()
    {
        WebpVp8LColor a = NewColor(1);
        WebpVp8LColor b = NewColor(2);
        WebpVp8LColor[] pixels = new WebpVp8LColor[] { a, b, a };

        List<WebpVp8LLz77Token> tokens = WebpVp8LLz77Encoder.FindMatches(pixels, 3, 1, 3, 32);

        Assert.That(tokens, Has.Count.EqualTo(3));
        Assert.That(tokens[2].IsLiteral, Is.True);
    }

    [Test]
    public void FindMatches_RejectsInvalidInputs()
    {
        WebpVp8LColor[] pixels = new WebpVp8LColor[] { NewColor(1) };

        Assert.Throws<ArgumentException>(() => WebpVp8LLz77Encoder.FindMatches(pixels, 2, 1, 3, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Encoder.FindMatches(pixels, 1, 1, 1, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Encoder.FindMatches(pixels, 1, 1, 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Encoder.EncodeDistanceCode(0, 1));
    }

    static void AssertPrefixRoundTrip(int value)
    {
        WebpVp8LPrefixValue encoded = WebpVp8LLz77Encoder.EncodePrefixValue(value);
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits((uint)encoded.ExtraValue, encoded.ExtraBitCount);

        int decoded = WebpVp8LLz77Decoder.DecodePrefixValue(new WebpVp8LBitReader(writer.ToArray()), encoded.PrefixCode);

        Assert.That(decoded, Is.EqualTo(value));
    }

    static WebpVp8LColor NewColor(byte green)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = green;
        color.Green = green;
        color.Blue = green;
        color.Alpha = 255;
        return color;
    }
}

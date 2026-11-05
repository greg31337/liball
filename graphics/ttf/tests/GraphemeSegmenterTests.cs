namespace ttf.Tests;

public class GraphemeSegmenterTests
{
    [Test]
    public void SegmentsCombiningFlagsAndCrLfWithUtf16Ranges()
    {
        string text = "A\u0301\U0001F1FA\U0001F1F8\r\n";
        RuneBuffer input = Decode(text);

        GraphemeClusterMap result = Segment(input, 6, 3);

        Assert.That(result.GetClusterCount(), Is.EqualTo(3));
        AssertCluster(result, 0, 0, 2, 0, 2);
        AssertCluster(result, 1, 2, 2, 2, 4);
        AssertCluster(result, 2, 4, 2, 6, 2);
        Assert.That(result.GetSourceLength(), Is.EqualTo(8));
        Assert.That(result.GetRuneEnd(2), Is.EqualTo(6));
        Assert.That(result.GetUtf16End(2), Is.EqualTo(8));
    }

    [Test]
    public void KeepsEmojiIndicHangulAndPrependSequencesTogether()
    {
        GraphemeClusterMap emoji = Segment(
            Decode("\U0001F469\U0001F3FD\u200D\U0001F4BB"),
            4,
            1);
        GraphemeClusterMap indic = Segment(
            Decode("\u0915\u094D\u200D\u0924"),
            4,
            1);
        GraphemeClusterMap hangul = Segment(
            Decode("\u1100\u1161\u11A8"),
            3,
            1);
        GraphemeClusterMap prepend = Segment(
            Decode("\u0600A\u0903"),
            3,
            1);

        AssertCluster(emoji, 0, 0, 4, 0, 7);
        AssertCluster(indic, 0, 0, 4, 0, 4);
        AssertCluster(hangul, 0, 0, 3, 0, 3);
        AssertCluster(prepend, 0, 0, 3, 0, 3);
    }

    [Test]
    public void PairsRegionalIndicatorsFromTheStartOfEachRun()
    {
        GraphemeClusterMap result = Segment(
            Decode("\U0001F1E6\U0001F1E7\U0001F1E8\U0001F1E9"),
            4,
            2);

        Assert.That(result.GetClusterCount(), Is.EqualTo(2));
        AssertCluster(result, 0, 0, 2, 0, 4);
        AssertCluster(result, 1, 2, 2, 4, 4);
    }

    [Test]
    public void CarriesNormalizedProvenanceIntoClusterRanges()
    {
        RuneBuffer decoded = Decode("\u00C0");
        RuneBuffer normalized = UnicodeNormalizer.Normalize(
            decoded,
            UnicodeData.LoadDefault(),
            UnicodeNormalizationMode.Nfd,
            2);

        GraphemeClusterMap result = Segment(normalized, 2, 1);

        AssertCluster(result, 0, 0, 2, 0, 1);
        Assert.That(result.GetSourceLength(), Is.EqualTo(1));
    }

    [Test]
    public void HandlesEmptyInputAndEnforcesLimits()
    {
        RuneBuffer empty = Decode(string.Empty);
        RuneBuffer twoClusters = Decode("AB");

        GraphemeClusterMap emptyResult = Segment(empty, 0, 0);
        FontLimitException? runeException = Assert.Throws<FontLimitException>(
            () => Segment(twoClusters, 1, 2));
        FontLimitException? clusterException = Assert.Throws<FontLimitException>(
            () => Segment(twoClusters, 2, 1));

        Assert.That(emptyResult.GetClusterCount(), Is.Zero);
        Assert.That(emptyResult.GetSourceLength(), Is.Zero);
        Assert.That(runeException!.Message, Is.EqualTo(
            "Grapheme segmentation exceeds the configured rune limit."));
        Assert.That(clusterException!.Message, Is.EqualTo(
            "Grapheme segmentation exceeds the configured cluster limit."));
    }

    [Test]
    public void ValidatesArgumentsAndClusterIndexes()
    {
        RuneBuffer input = Decode("A");
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        GraphemeClusterMap result = GraphemeSegmenter.Segment(
            input,
            unicodeData,
            1,
            1);

        Assert.Throws<ArgumentNullException>(() => GraphemeSegmenter.Segment(
            null!,
            unicodeData,
            1,
            1));
        Assert.Throws<ArgumentNullException>(() => GraphemeSegmenter.Segment(
            input,
            null!,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GraphemeSegmenter.Segment(input, unicodeData, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GraphemeSegmenter.Segment(input, unicodeData, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRuneStart(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRuneLength(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUtf16Start(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUtf16Length(1));
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static GraphemeClusterMap Segment(
        RuneBuffer input,
        int maximumRuneCount,
        int maximumClusterCount)
    {
        return GraphemeSegmenter.Segment(
            input,
            UnicodeData.LoadDefault(),
            maximumRuneCount,
            maximumClusterCount);
    }

    static void AssertCluster(
        GraphemeClusterMap result,
        int clusterIndex,
        int runeStart,
        int runeLength,
        int utf16Start,
        int utf16Length)
    {
        Assert.That(result.GetRuneStart(clusterIndex), Is.EqualTo(runeStart));
        Assert.That(result.GetRuneLength(clusterIndex), Is.EqualTo(runeLength));
        Assert.That(result.GetUtf16Start(clusterIndex), Is.EqualTo(utf16Start));
        Assert.That(result.GetUtf16Length(clusterIndex), Is.EqualTo(utf16Length));
    }
}

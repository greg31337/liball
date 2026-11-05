namespace ttf.Tests;

public class HyphenationDictionaryTests
{
    [Test]
    public void MatchesWeightedPatternsAndReportsUtf16Boundaries()
    {
        HyphenationDictionary dictionary = Load(
            "LEFTHYPHENMIN 1\n" +
            "RIGHTHYPHENMIN 1\n" +
            "hy1phen phen1a a1😀b\n");

        HyphenationMatch latin = dictionary.Match("HYPHENATION");
        HyphenationMatch supplementary = dictionary.Match("A😀BC");

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.GetPatternCount(), Is.EqualTo(3));
            Assert.That(dictionary.GetLeftMinimum(), Is.EqualTo(1));
            Assert.That(dictionary.GetRightMinimum(), Is.EqualTo(1));
            Assert.That(latin.GetSourceLength(), Is.EqualTo(11));
            Assert.That(latin.GetPointCount(), Is.EqualTo(2));
            Assert.That(latin.GetRuneIndex(0), Is.EqualTo(2));
            Assert.That(latin.GetUtf16Index(0), Is.EqualTo(2));
            Assert.That(latin.GetRuneIndex(1), Is.EqualTo(6));
            Assert.That(latin.GetUtf16Index(1), Is.EqualTo(6));
            Assert.That(supplementary.GetPointCount(), Is.EqualTo(1));
            Assert.That(supplementary.GetRuneIndex(0), Is.EqualTo(1));
            Assert.That(supplementary.GetUtf16Index(0), Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesBoundaryPatternsAndStrongestOverlappingWeight()
    {
        HyphenationDictionary anchored = Load(".re1\n");
        HyphenationDictionary overridden = Load("hy1phen hy2phen\n");

        Assert.Multiple(() =>
        {
            Assert.That(anchored.Match("record").GetPointCount(), Is.EqualTo(1));
            Assert.That(anchored.Match("record").GetUtf16Index(0), Is.EqualTo(2));
            Assert.That(anchored.Match("prerecord").GetPointCount(), Is.Zero);
            Assert.That(overridden.Match("hyphenation").GetPointCount(), Is.Zero);
        });
    }

    [Test]
    public void ExplicitExceptionsOverridePatternsAndStillHonorMinima()
    {
        HyphenationDictionary dictionary = Load(
            "leftmin 3\n" +
            "rightmin 2\n" +
            "h1y y1p p1h h1e e1n n1a a1t t1i i1o o1n\n" +
            "exception hy-phen-a-tion\n");

        HyphenationMatch result = dictionary.FindHyphenationPoints("Hyphenation");

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.GetExceptionCount(), Is.EqualTo(1));
            Assert.That(result.GetPointCount(), Is.EqualTo(2));
            Assert.That(result.GetUtf16Index(0), Is.EqualTo(6));
            Assert.That(result.GetUtf16Index(1), Is.EqualTo(7));
        });
    }

    [Test]
    public void AcceptsUtf8MarkerCommentsAndTexBlocks()
    {
        HyphenationDictionary dictionary = Load(
            "\uFEFFUTF-8\n" +
            "% dictionary comment\n" +
            "\\lefthyphenmin=1\n" +
            "\\righthyphenmin=1\n" +
            "\\patterns{\n" +
            "a1bc # inline comment\n" +
            "}\n" +
            "\\hyphenation{ ex-cep-tion }\n");

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Match("abcd").GetUtf16Index(0), Is.EqualTo(1));
            Assert.That(dictionary.Match("exception").GetPointCount(), Is.EqualTo(2));
            Assert.That(dictionary.Match("exception").GetUtf16Index(0), Is.EqualTo(2));
            Assert.That(dictionary.Match("exception").GetUtf16Index(1), Is.EqualTo(5));
        });
    }

    [Test]
    public void ReadsFromCurrentStreamPositionAndLeavesStreamOpen()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("xxxxhy1phen\n");
        using MemoryStream stream = new MemoryStream(bytes);
        stream.Position = 4;

        HyphenationDictionary dictionary = HyphenationDictionary.Load(stream);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Match("hyphenation").GetPointCount(), Is.EqualTo(1));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
        });
    }

    [Test]
    public void RejectsMalformedDictionaryData()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                HyphenationDictionary.Load(new byte[] { 0xFF }));
            Assert.Throws<HyphenationFormatException>(() => Load("a12b\n"));
            Assert.Throws<HyphenationFormatException>(() => Load("a.b\n"));
            Assert.Throws<HyphenationFormatException>(() => Load("exception word\n"));
            Assert.Throws<HyphenationFormatException>(() => Load("exception -word\n"));
            Assert.Throws<HyphenationFormatException>(() => Load("\\patterns{ a1b\n"));
            Assert.Throws<HyphenationFormatException>(() => Load("leftmin -1\n"));
        });
    }

    [Test]
    public void EnforcesEveryConfiguredResourceLimit()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() =>
                HyphenationDictionary.Load(
                    System.Text.Encoding.UTF8.GetBytes("123456789"),
                    Limits(8, 10, 10, 10, 20, 10)));
            Assert.Throws<FontLimitException>(() =>
                HyphenationDictionary.Load(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes("123456789")),
                    Limits(8, 10, 10, 10, 20, 10)));
            Assert.Throws<FontLimitException>(() =>
                Load("a1b c1d\n", Limits(100, 1, 10, 10, 20, 10)));
            Assert.Throws<FontLimitException>(() =>
                Load("ab1cd\n", Limits(100, 10, 4, 10, 20, 10)));
            Assert.Throws<FontLimitException>(() =>
                Load("ab1\n", Limits(100, 10, 10, 10, 2, 10)));
            Assert.Throws<FontLimitException>(() =>
                Load("exception ex-ample\n", Limits(100, 10, 10, 0, 20, 10)));

            HyphenationDictionary shortWords = Load(
                "a1b\n",
                Limits(100, 10, 10, 10, 20, 2));
            Assert.Throws<FontLimitException>(() => shortWords.Match("abc"));
        });
    }

    [Test]
    public void HandlesEmptyInputsAndValidatesPublicArguments()
    {
        HyphenationDictionary empty = Load(string.Empty);
        HyphenationMatch match = empty.Match(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(match.GetSourceLength(), Is.Zero);
            Assert.That(match.GetPointCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() => HyphenationDictionary.Load((byte[]?)null!));
            Assert.Throws<ArgumentNullException>(() => HyphenationDictionary.Load((Stream?)null!));
            Assert.Throws<ArgumentNullException>(() =>
                HyphenationDictionary.Load(Array.Empty<byte>(), null!));
            Assert.Throws<ArgumentNullException>(() => empty.Match(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => match.GetUtf16Index(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HyphenationLimits.Create(0, 1, 1, 0, 1, 1));
        });
    }

    static HyphenationDictionary Load(string text)
    {
        return HyphenationDictionary.Load(System.Text.Encoding.UTF8.GetBytes(text));
    }

    static HyphenationDictionary Load(string text, HyphenationLimits limits)
    {
        return HyphenationDictionary.Load(
            System.Text.Encoding.UTF8.GetBytes(text),
            limits);
    }

    static HyphenationLimits Limits(
        int maxInputBytes,
        int maxPatterns,
        int maxPatternLength,
        int maxExceptions,
        int maxTrieNodes,
        int maxWordLength)
    {
        return HyphenationLimits.Create(
            maxInputBytes,
            maxPatterns,
            maxPatternLength,
            maxExceptions,
            maxTrieNodes,
            maxWordLength);
    }
}

namespace ttf.Tests;

public class ScriptItemizerTests
{
    [Test]
    public void ProducesStableRunsForMixedSpecificScripts()
    {
        ScriptRunMap result = Itemize("abc \u03B1\u03B2 \u0436", 8, 3);

        Assert.That(result.GetRunCount(), Is.EqualTo(3));
        AssertRun(result, 0, "Latn", 0, 4, 0, 4, 0, 4);
        AssertRun(result, 1, "Grek", 4, 3, 4, 3, 4, 3);
        AssertRun(result, 2, "Cyrl", 7, 1, 7, 1, 7, 1);
        Assert.That(result.GetClusterEnd(2), Is.EqualTo(8));
        Assert.That(result.GetRuneEnd(2), Is.EqualTo(8));
        Assert.That(result.GetUtf16End(2), Is.EqualTo(8));
        Assert.That(result.GetSourceLength(), Is.EqualTo(8));
    }

    [Test]
    public void UsesScriptExtensionsWithoutExtendingIncompatibleRuns()
    {
        ScriptRunMap katakana = Itemize("\u30AB\u30FC\u30CA", 3, 1);
        ScriptRunMap afterLatin = Itemize("a\u30FC\u30CA", 3, 2);
        ScriptRunMap arabic = Itemize("\u0627\u060C\u0628", 3, 1);
        ScriptRunMap greek = Itemize("\u03B1\u00B7\u03B2", 3, 1);
        ScriptRunMap narrowed = Itemize("\u064B\u0640\u0710", 3, 1);

        AssertRun(katakana, 0, "Kana", 0, 3, 0, 3, 0, 3);
        AssertRun(afterLatin, 0, "Latn", 0, 1, 0, 1, 0, 1);
        AssertRun(afterLatin, 1, "Kana", 1, 2, 1, 2, 1, 2);
        Assert.That(arabic.GetScript(0), Is.EqualTo("Arab"));
        Assert.That(arabic.GetRuneLength(0), Is.EqualTo(3));
        Assert.That(greek.GetScript(0), Is.EqualTo("Grek"));
        Assert.That(greek.GetRuneLength(0), Is.EqualTo(3));
        Assert.That(narrowed.GetScript(0), Is.EqualTo("Syrc"));
        Assert.That(narrowed.GetRuneLength(0), Is.EqualTo(3));
    }

    [Test]
    public void AssignsMatchedPunctuationToTheEnclosingScript()
    {
        ScriptRunMap result = Itemize("a(\u03B1)b", 5, 3);

        Assert.That(result.GetRunCount(), Is.EqualTo(3));
        AssertRun(result, 0, "Latn", 0, 2, 0, 2, 0, 2);
        AssertRun(result, 1, "Grek", 2, 1, 2, 1, 2, 1);
        AssertRun(result, 2, "Latn", 3, 2, 3, 2, 3, 2);
    }

    [Test]
    public void KeepsGraphemeClustersInOneScriptRun()
    {
        ScriptRunMap combining = Itemize("a\u0301\u03B1", 3, 2);
        ScriptRunMap commonBase = Itemize("\u25CC\u05B0", 2, 1);

        AssertRun(combining, 0, "Latn", 0, 1, 0, 2, 0, 2);
        AssertRun(combining, 1, "Grek", 1, 1, 2, 1, 2, 1);
        AssertRun(commonBase, 0, "Hebr", 0, 1, 0, 2, 0, 2);
    }

    [Test]
    public void ResolvesLeadingAndAllCommonTextDeterministically()
    {
        ScriptRunMap leading = Itemize(".\u03B1", 2, 1);
        ScriptRunMap common = Itemize("123", 3, 1);

        AssertRun(leading, 0, "Grek", 0, 2, 0, 2, 0, 2);
        AssertRun(common, 0, "Zyyy", 0, 3, 0, 3, 0, 3);
    }

    [Test]
    public void HandlesEmptyInputAndEnforcesLimits()
    {
        ScriptRunMap empty = Itemize(string.Empty, 0, 0);
        FontLimitException? runeException = Assert.Throws<FontLimitException>(
            () => Itemize("ab", 1, 2));
        FontLimitException? runException = Assert.Throws<FontLimitException>(
            () => Itemize("a\u03B1", 2, 1));

        Assert.That(empty.GetRunCount(), Is.Zero);
        Assert.That(empty.GetSourceLength(), Is.Zero);
        Assert.That(runeException!.Message, Is.EqualTo(
            "Script itemization exceeds the configured rune limit."));
        Assert.That(runException!.Message, Is.EqualTo(
            "Script itemization exceeds the configured run limit."));
    }

    [Test]
    public void ValidatesArgumentsClusterMapsAndRunIndexes()
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        RuneBuffer input = Decode("A");
        GraphemeClusterMap clusters = Segment(input);
        ScriptRunMap result = ScriptItemizer.Itemize(
            input,
            clusters,
            unicodeData,
            1,
            1);
        GraphemeClusterMap mismatched = new GraphemeClusterMap(
            new int[] { 0 },
            new int[] { 1 },
            new int[] { 0 },
            new int[] { 1 },
            2);

        Assert.Throws<ArgumentNullException>(() => ScriptItemizer.Itemize(
            null!,
            clusters,
            unicodeData,
            1,
            1));
        Assert.Throws<ArgumentNullException>(() => ScriptItemizer.Itemize(
            input,
            null!,
            unicodeData,
            1,
            1));
        Assert.Throws<ArgumentNullException>(() => ScriptItemizer.Itemize(
            input,
            clusters,
            null!,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScriptItemizer.Itemize(
            input,
            clusters,
            unicodeData,
            -1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScriptItemizer.Itemize(
            input,
            clusters,
            unicodeData,
            1,
            -1));
        Assert.Throws<ArgumentException>(() => ScriptItemizer.Itemize(
            input,
            mismatched,
            unicodeData,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetScript(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetClusterStart(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRuneLength(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetUtf16Start(1));
    }

    static ScriptRunMap Itemize(
        string text,
        int maximumRuneCount,
        int maximumRunCount)
    {
        RuneBuffer input = Decode(text);
        GraphemeClusterMap clusters = Segment(input);
        return ScriptItemizer.Itemize(
            input,
            clusters,
            UnicodeData.LoadDefault(),
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

    static GraphemeClusterMap Segment(RuneBuffer input)
    {
        return GraphemeSegmenter.Segment(
            input,
            UnicodeData.LoadDefault(),
            input.GetCount(),
            input.GetCount());
    }

    static void AssertRun(
        ScriptRunMap result,
        int runIndex,
        string script,
        int clusterStart,
        int clusterLength,
        int runeStart,
        int runeLength,
        int utf16Start,
        int utf16Length)
    {
        Assert.That(result.GetScript(runIndex), Is.EqualTo(script));
        Assert.That(result.GetClusterStart(runIndex), Is.EqualTo(clusterStart));
        Assert.That(result.GetClusterLength(runIndex), Is.EqualTo(clusterLength));
        Assert.That(result.GetRuneStart(runIndex), Is.EqualTo(runeStart));
        Assert.That(result.GetRuneLength(runIndex), Is.EqualTo(runeLength));
        Assert.That(result.GetUtf16Start(runIndex), Is.EqualTo(utf16Start));
        Assert.That(result.GetUtf16Length(runIndex), Is.EqualTo(utf16Length));
    }
}

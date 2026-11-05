namespace svg.Tests;

public class SvgUnicodeTextTests
{
    [Test]
    public void DecodesScalarsReplacesUnpairedSurrogatesAndPreservesUtf16Mappings()
    {
        string text = "A\U0001F600\uD800B\uDC00\uFFFD";

        SvgUnicodeTextResult result = new SvgUnicodeTextAnalyzer().AnalyzeWithText(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnicodeText.SourceText, Is.EqualTo(text));
            Assert.That(result.UnicodeText.Text, Is.EqualTo("A\U0001F600\uFFFDB\uFFFD\uFFFD"));
            Assert.That(result.UnicodeText.ReplacementCount, Is.EqualTo(2));
            Assert.That(result.UnicodeText.Scalars.Select(value => value.Value), Is.EqualTo(new[] { 0x41, 0x1F600, 0xFFFD, 0x42, 0xFFFD, 0xFFFD }));
            Assert.That(result.UnicodeText.Scalars.Select(value => value.Utf16Start), Is.EqualTo(new[] { 0, 1, 3, 4, 5, 6 }));
            Assert.That(result.UnicodeText.Scalars.Select(value => value.Utf16Length), Is.EqualTo(new[] { 1, 2, 1, 1, 1, 1 }));
            Assert.That(result.UnicodeText.Scalars.Select(value => value.IsReplacement), Is.EqualTo(new[] { false, false, true, false, true, false }));
            Assert.That(result.UnicodeText.FindScalar(1), Is.SameAs(result.UnicodeText.Scalars[1]));
            Assert.That(result.UnicodeText.FindScalar(2), Is.SameAs(result.UnicodeText.Scalars[1]));
            Assert.That(result.UnicodeText.FindCluster(2), Is.SameAs(result.UnicodeText.Clusters[1]));
            Assert.That(result.UnicodeText.FindScalar(-1), Is.Null);
            Assert.That(result.UnicodeText.FindCluster(text.Length), Is.Null);
        });
    }

    [Test]
    public void ExposesVariationSelectorsCombiningMarksAndIteratorState()
    {
        string text = "A\u0301 \u2764\uFE0F \U00020000\U000E0100";
        SvgUnicodeTextResult result = new SvgUnicodeTextAnalyzer().AnalyzeWithText(text);
        SvgUnicodeScalarIterator scalars = result.UnicodeText.CreateScalarIterator();
        SvgGraphemeIterator clusters = result.UnicodeText.CreateGraphemeIterator();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnicodeText.Scalars[1].IsCombiningMark, Is.True);
            Assert.That(result.UnicodeText.Scalars[1].IsVariationSelector, Is.False);
            Assert.That(result.UnicodeText.Clusters[0].HasCombiningMarks, Is.True);
            Assert.That(result.UnicodeText.Clusters[0].ScalarCount, Is.EqualTo(2));
            Assert.That(result.UnicodeText.Clusters[0].BaseScalarIndex, Is.Zero);
            Assert.That(result.UnicodeText.Scalars[4].IsVariationSelector, Is.True);
            Assert.That(result.UnicodeText.Scalars[4].IsCombiningMark, Is.False);
            Assert.That(result.UnicodeText.Scalars[4].VariationBaseIndex, Is.EqualTo(3));
            Assert.That(result.UnicodeText.Scalars[7].IsVariationSelector, Is.True);
            Assert.That(result.UnicodeText.Scalars[7].VariationBaseIndex, Is.EqualTo(6));
            Assert.That(result.UnicodeText.Clusters[2].VariationSelectorIndices, Is.EqualTo(new[] { 4 }));
            Assert.That(result.UnicodeText.Clusters[4].VariationSelectorIndices, Is.EqualTo(new[] { 7 }));
            Assert.That(result.UnicodeText.Clusters[2].IsEmojiSequence, Is.True);
            Assert.That(scalars.Index, Is.EqualTo(-1));
            Assert.That(scalars.MoveNext(), Is.True);
            Assert.That(scalars.Current.Value, Is.EqualTo(0x41));
            Assert.That(clusters.MoveNext(), Is.True);
            Assert.That(clusters.Current.Text, Is.EqualTo("A\u0301"));
        });

        while (scalars.MoveNext())
        {
        }
        scalars.Reset();
        clusters.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(scalars.Index, Is.EqualTo(-1));
            Assert.That(scalars.Current, Is.Null);
            Assert.That(clusters.Index, Is.EqualTo(-1));
            Assert.That(clusters.Current, Is.Null);
        });
    }

    [TestCase("\r\n")]
    [TestCase("\u1100\u1161\u11A8")]
    [TestCase("\U0001F1FA\U0001F1F8")]
    [TestCase("\U0001F469\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466")]
    public void KeepsExtendedGraphemeSequencesTogether(string text)
    {
        SvgUnicodeTextResult result = new SvgUnicodeTextAnalyzer().AnalyzeWithText(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnicodeText.Clusters, Has.Count.EqualTo(1));
            Assert.That(result.UnicodeText.Clusters[0].Text, Is.EqualTo(text));
            Assert.That(result.UnicodeText.Clusters[0].Utf16Length, Is.EqualTo(text.Length));
            Assert.That(result.UnicodeText.Clusters[0].ScalarCount, Is.EqualTo(result.UnicodeText.Scalars.Count));
        });
    }

    [Test]
    public void ItemizesResolvedScriptsWithoutSplittingCommonClusters()
    {
        string text = "(abc 123 Ελληνικά العربية हिंदी 漢字";

        SvgUnicodeTextResult result = new SvgUnicodeTextAnalyzer().AnalyzeWithText(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnicodeText.ScriptRuns.Select(run => run.Script), Is.EqualTo(new[] { "Latin", "Greek", "Arabic", "Devanagari", "Han" }));
            Assert.That(result.UnicodeText.ScriptRuns[0].Utf16Start, Is.Zero);
            Assert.That(result.UnicodeText.ScriptRuns[0].ClusterStart, Is.Zero);
            Assert.That(result.UnicodeText.ScriptRuns.Sum(run => run.Utf16Length), Is.EqualTo(text.Length));
            Assert.That(result.UnicodeText.ScriptRuns.Sum(run => run.ScalarCount), Is.EqualTo(result.UnicodeText.Scalars.Count));
            Assert.That(result.UnicodeText.ScriptRuns.Sum(run => run.ClusterCount), Is.EqualTo(result.UnicodeText.Clusters.Count));
            Assert.That(result.UnicodeText.Clusters[0].Script, Is.EqualTo("Latin"));
        });
    }

    [Test]
    public void AppliesOverlappingLanguageSpansAndBuildsCombinedItemRuns()
    {
        SvgUnicodeTextOptions options = new SvgUnicodeTextOptions();
        options.DefaultLanguage = "en";
        options.LanguageSpans.Add(new SvgLanguageSpan(3, 3, "el"));
        options.LanguageSpans.Add(new SvgLanguageSpan(4, 1, "el-CY"));

        SvgUnicodeTextResult result = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("abcαβγ", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnicodeText.Clusters.Select(cluster => cluster.Language), Is.EqualTo(new[] { "en", "en", "en", "el", "el-CY", "el" }));
            Assert.That(result.UnicodeText.LanguageRuns.Select(run => run.Language), Is.EqualTo(new[] { "en", "el", "el-CY", "el" }));
            Assert.That(result.UnicodeText.ScriptRuns.Select(run => run.Script), Is.EqualTo(new[] { "Latin", "Greek" }));
            Assert.That(result.UnicodeText.ItemRuns.Select(run => run.Script + "/" + run.Language), Is.EqualTo(new[] { "Latin/en", "Greek/el", "Greek/el-CY", "Greek/el" }));
            Assert.That(result.UnicodeText.ItemRuns.Sum(run => run.ClusterCount), Is.EqualTo(result.UnicodeText.Clusters.Count));
        });
    }

    [Test]
    public void HonorsValidationCancellationAndConfiguredBounds()
    {
        SvgUnicodeTextOptions scalarLimit = new SvgUnicodeTextOptions();
        scalarLimit.MaxScalars = 1;
        SvgUnicodeTextOptions clusterLimit = new SvgUnicodeTextOptions();
        clusterLimit.MaxClusters = 1;
        SvgUnicodeTextOptions runLimit = new SvgUnicodeTextOptions();
        runLimit.MaxRuns = 1;
        SvgUnicodeTextOptions invalidSpan = new SvgUnicodeTextOptions();
        invalidSpan.LanguageSpans.Add(new SvgLanguageSpan(1, 2, "en"));
        SvgUnicodeTextOptions cancelled = new SvgUnicodeTextOptions();
        System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;

        SvgUnicodeTextResult nullText = new SvgUnicodeTextAnalyzer().AnalyzeWithText(null);
        SvgUnicodeTextResult invalidLanguageSpan = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("a", invalidSpan);
        SvgUnicodeTextResult tooManyScalars = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("ab", scalarLimit);
        SvgUnicodeTextResult tooManyClusters = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("ab", clusterLimit);
        SvgUnicodeTextResult tooManyRuns = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("aα", runLimit);
        SvgUnicodeTextResult stopped = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions("a", cancelled);
        SvgUnicodeTextResult empty = new SvgUnicodeTextAnalyzer().AnalyzeWithText("");

        Assert.Multiple(() =>
        {
            Assert.That(nullText.Error, Is.EqualTo(SvgUnicodeTextError.InvalidArgument));
            Assert.That(invalidLanguageSpan.Error, Is.EqualTo(SvgUnicodeTextError.InvalidArgument));
            Assert.That(tooManyScalars.Error, Is.EqualTo(SvgUnicodeTextError.LimitExceeded));
            Assert.That(tooManyClusters.Error, Is.EqualTo(SvgUnicodeTextError.LimitExceeded));
            Assert.That(tooManyRuns.Error, Is.EqualTo(SvgUnicodeTextError.LimitExceeded));
            Assert.That(stopped.Error, Is.EqualTo(SvgUnicodeTextError.Cancelled));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.UnicodeText.Scalars, Is.Empty);
            Assert.That(empty.UnicodeText.Clusters, Is.Empty);
            Assert.That(empty.UnicodeText.ItemRuns, Is.Empty);
        });
    }
}

namespace svg.Tests;

public class SvgBidiTests
{
    [Test]
    public void ReordersMixedDirectionTextAndBuildsInverseScalarMaps()
    {
        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithText("abc אבג 123");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VisualText, Is.EqualTo("abc 123 גבא"));
            Assert.That(result.Paragraphs, Has.Count.EqualTo(1));
            Assert.That(result.Paragraphs[0].EmbeddingLevel, Is.Zero);
            Assert.That(result.Scalars.Select(value => value.Level), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2 }));
            Assert.That(result.VisualToLogical, Is.EqualTo(new[] { 0, 1, 2, 3, 8, 9, 10, 7, 6, 5, 4 }));
        });

        for (int logical = 0; logical < result.LogicalToVisual.Count; logical++)
        {
            Assert.That(result.VisualToLogical[result.LogicalToVisual[logical]], Is.EqualTo(logical));
        }
    }

    [Test]
    public void HonorsForcedParagraphDirectionAndResolvesWeakNumberTypes()
    {
        SvgBidiOptions options = new SvgBidiOptions();
        options.Direction = SvgBidiDirection.RightToLeft;

        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions("abc אבג 12", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Paragraphs[0].EmbeddingLevel, Is.EqualTo(1));
            Assert.That(result.VisualText, Is.EqualTo("12 גבא abc"));
            Assert.That(result.Scalars[8].OriginalClass, Is.EqualTo(SvgBidiClass.EuropeanNumber));
            Assert.That(result.Scalars[8].ResolvedClass, Is.EqualTo(SvgBidiClass.EuropeanNumber));
            Assert.That(result.Scalars[8].Level, Is.EqualTo(2));
        });
    }

    [Test]
    public void AppliesExplicitOverridesAndOmitsFormattingControlsFromVisualMaps()
    {
        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithText("A\u202Eabc\u202C Z");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VisualText, Is.EqualTo("Acba Z"));
            Assert.That(result.Scalars[1].OriginalClass, Is.EqualTo(SvgBidiClass.RightToLeftOverride));
            Assert.That(result.Scalars[2].ResolvedClass, Is.EqualTo(SvgBidiClass.RightToLeft));
            Assert.That(result.Scalars[2].Level, Is.EqualTo(1));
            Assert.That(result.Scalars[1].RemovedControl, Is.True);
            Assert.That(result.Scalars[5].RemovedControl, Is.True);
            Assert.That(result.LogicalToVisual[1], Is.EqualTo(-1));
            Assert.That(result.LogicalToVisual[5], Is.EqualTo(-1));
            Assert.That(result.VisualToLogical, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public void ResolvesIsolatesWithoutLeakingTheirStrongDirectionToTheParagraph()
    {
        string text = "\u2067אבג 123\u2069 abc";

        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithText(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Paragraphs[0].EmbeddingLevel, Is.Zero);
            Assert.That(result.Scalars[0].MatchingControlIndex, Is.EqualTo(8));
            Assert.That(result.Scalars[8].MatchingControlIndex, Is.Zero);
            Assert.That(result.VisualText, Is.EqualTo("123 גבא abc"));
            Assert.That(result.LogicalToVisual[0], Is.EqualTo(-1));
            Assert.That(result.LogicalToVisual[8], Is.EqualTo(-1));
        });
    }

    [Test]
    public void UsesFirstStrongDirectionAndProcessesUnmatchedIsolatesToParagraphEnd()
    {
        SvgBidiResult firstStrong = new SvgBidiAnalyzer().AnalyzeWithText("A \u2068אב\u2069 B");
        SvgBidiResult unmatched = new SvgBidiAnalyzer().AnalyzeWithText("\u2067אב");

        Assert.Multiple(() =>
        {
            Assert.That(firstStrong.Success, Is.True);
            Assert.That(firstStrong.Scalars[2].OriginalClass, Is.EqualTo(SvgBidiClass.FirstStrongIsolate));
            Assert.That(firstStrong.Scalars[2].MatchingControlIndex, Is.EqualTo(5));
            Assert.That(firstStrong.VisualText, Is.EqualTo("A בא B"));
            Assert.That(unmatched.Success, Is.True);
            Assert.That(unmatched.Paragraphs[0].EmbeddingLevel, Is.Zero);
            Assert.That(unmatched.Scalars[0].MatchingControlIndex, Is.EqualTo(-1));
            Assert.That(unmatched.VisualText, Is.EqualTo("בא"));
        });
    }

    [Test]
    public void ResolvesBracketPairsAndMirrorsOddLevelGlyphs()
    {
        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithText("אבג (abc) דהו");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VisualText, Is.EqualTo("והד (abc) גבא"));
            Assert.That(result.Scalars[4].PairedBracketIndex, Is.EqualTo(8));
            Assert.That(result.Scalars[8].PairedBracketIndex, Is.EqualTo(4));
            Assert.That(result.Scalars[4].ResolvedClass, Is.EqualTo(SvgBidiClass.RightToLeft));
            Assert.That(result.Scalars[4].Mirrored, Is.True);
            Assert.That(result.Scalars[4].DisplayValue, Is.EqualTo(')'));
            Assert.That(result.Scalars[8].DisplayValue, Is.EqualTo('('));
        });
    }

    [Test]
    public void ResolvesParagraphsIndependentlyAndKeepsSeparatorsAtTheirEnds()
    {
        SvgBidiResult result = new SvgBidiAnalyzer().AnalyzeWithText("אב\nabc");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VisualText, Is.EqualTo("בא\nabc"));
            Assert.That(result.Paragraphs, Has.Count.EqualTo(2));
            Assert.That(result.Paragraphs.Select(value => value.EmbeddingLevel), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(result.Paragraphs.Select(value => value.VisualCount), Is.EqualTo(new[] { 3, 3 }));
            Assert.That(result.VisualToLogical, Is.EqualTo(new[] { 1, 0, 2, 3, 4, 5 }));
        });
    }

    [Test]
    public void ReplacesInvalidUtf16AndHonorsValidationLimitsAndCancellation()
    {
        SvgBidiOptions limited = new SvgBidiOptions();
        limited.MaxScalars = 1;
        SvgBidiOptions invalid = new SvgBidiOptions();
        invalid.MaxExplicitLevel = 126;
        SvgBidiOptions cancelled = new SvgBidiOptions();
        System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;

        SvgBidiResult replacement = new SvgBidiAnalyzer().AnalyzeWithText("A\uD800B");
        SvgBidiResult nullText = new SvgBidiAnalyzer().AnalyzeWithText(null);
        SvgBidiResult invalidOptions = new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions("a", invalid);
        SvgBidiResult limit = new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions("ab", limited);
        SvgBidiResult stopped = new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions("a", cancelled);
        SvgBidiResult empty = new SvgBidiAnalyzer().AnalyzeWithText("");

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Success, Is.True);
            Assert.That(replacement.LogicalText, Is.EqualTo("A\uFFFDB"));
            Assert.That(replacement.VisualText, Is.EqualTo("A\uFFFDB"));
            Assert.That(nullText.Error, Is.EqualTo(SvgBidiError.InvalidArgument));
            Assert.That(invalidOptions.Error, Is.EqualTo(SvgBidiError.InvalidArgument));
            Assert.That(limit.Error, Is.EqualTo(SvgBidiError.LimitExceeded));
            Assert.That(stopped.Error, Is.EqualTo(SvgBidiError.Cancelled));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.VisualText, Is.Empty);
            Assert.That(empty.VisualToLogical, Is.Empty);
        });
    }
}

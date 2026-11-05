using System;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleReadabilityTests
{
    [Test]
    public void RecognizesLongSemanticArticleContent()
    {
        string paragraph = "This is a substantial article paragraph, "
            + "with several clauses, useful details, and enough prose to "
            + "look like authored editorial content. It continues with "
            + "context, explanation, and a clear concluding sentence.";
        HtmlDocument document = Html.Parse(
            "<main><article><h1>Headline</h1><p>" + paragraph
            + "</p><p>" + paragraph + "</p></article></main>");

        bool readable = HtmlArticleExtractor.IsProbablyReadable(document,
            HtmlArticleOptions.CreateDefault());

        Assert.That(readable, Is.True);
    }

    [Test]
    public void SemanticContainerMakesPreflightConservativeForGenericLayouts()
    {
        string text = new string('a', 320);
        HtmlDocument semantic = Html.Parse(
            "<article>" + text + "</article>");
        HtmlDocument generic = Html.Parse("<div>" + text + "</div>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                semantic, options), Is.True);
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                generic, options), Is.False);
        });
    }

    [Test]
    public void RejectsExcludedNavigationAndLinkDenseContent()
    {
        string text = new string('n', 500);
        HtmlDocument navigation = Html.Parse(
            "<nav><article><p>" + text + "</p></article></nav>");
        HtmlDocument links = Html.Parse(
            "<article><p><a href='/all'>" + text
            + "</a></p></article>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                navigation, options), Is.False);
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                links, options), Is.False);
        });
    }

    [Test]
    public void PunctuationRaisesOtherwiseEqualTextAboveConfiguredThreshold()
    {
        HtmlDocument plain = Html.Parse(
            "<p>" + new string('a', 180) + "</p>");
        HtmlDocument punctuated = Html.Parse(
            "<p>" + new string('a', 168) + new string(',', 12) + "</p>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        options.SetScoreThreshold(15);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                plain, options), Is.False);
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                punctuated, options), Is.True);
        });
    }

    [Test]
    public void ConfiguredContentAndScoreThresholdsControlPreflight()
    {
        HtmlDocument document = Html.Parse(
            "<article><p>" + new string('x', 400)
            + ". This is the ending.</p></article>");
        HtmlArticleOptions permissive = HtmlArticleOptions.CreateDefault();
        permissive.SetMinimumContentCharacters(100);
        permissive.SetScoreThreshold(10);
        HtmlArticleOptions contentLimited = permissive.Copy();
        contentLimited.SetMinimumContentCharacters(1000);
        HtmlArticleOptions scoreLimited = permissive.Copy();
        scoreLimited.SetScoreThreshold(1000);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                document, permissive), Is.True);
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                document, contentLimited), Is.False);
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                document, scoreLimited), Is.False);
        });
    }

    [Test]
    public void InternalPreflightHonorsVisitLimitsAndCancellation()
    {
        HtmlDocument document = Html.Parse(
            "<html><body><article>" + new string('x', 500)
            + "</article></body></html>");
        HtmlArticleOptions limited = HtmlArticleOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(2);
        limited.SetLimits(limits);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlArticleExtractor.IsProbablyReadable(
                document, limited, CancellationToken.None), Is.False);
            Assert.Throws<OperationCanceledException>(() =>
                HtmlArticleExtractor.IsProbablyReadable(document,
                    HtmlArticleOptions.CreateDefault(), source.Token));
        });
    }

    [Test]
    public void ValidatesRequiredArguments()
    {
        HtmlDocument document = Html.Parse("<p>text</p>");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.IsProbablyReadable(null,
                    HtmlArticleOptions.CreateDefault()));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.IsProbablyReadable(document, null));
        });
    }
}

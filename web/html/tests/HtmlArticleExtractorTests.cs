using System;
using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleExtractorTests
{
    [Test]
    public void AssemblesMetadataAndCleanedContentIntoArticle()
    {
        HtmlDocument document = ParseWithBase(
            "<html lang=en-US dir=ltr><head>"
            + "<title>Document title</title>"
            + "<meta property=og:title content='Metadata title'>"
            + "<meta property=og:description content='Metadata excerpt'>"
            + "<meta property=og:site_name content='Example News'>"
            + "<meta property=article:author content='Jane Reporter'>"
            + "<meta property=article:published_time "
            + "content='2026-08-11T12:30:00Z'>"
            + "<meta property=og:image content='/lead.jpg'>"
            + "<link rel=canonical href='/story'></head><body>"
            + "<article id=source><h1>Content heading</h1>"
            + "<p>" + Prose() + "</p><aside>Related links</aside>"
            + "</article></body></html>",
            "https://example.test/news/page.html");

        HtmlArticleResult result = HtmlArticleExtractor.Extract(document,
            HtmlArticleOptions.CreateDefault(), CancellationToken.None);
        HtmlArticle article = result.GetArticle();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(article.GetTitle(), Is.EqualTo("Metadata title"));
            Assert.That(article.GetByline(), Is.EqualTo("Jane Reporter"));
            Assert.That(article.GetExcerpt(),
                Is.EqualTo("Metadata excerpt"));
            Assert.That(article.GetSiteName(), Is.EqualTo("Example News"));
            Assert.That(article.GetLanguage(), Is.EqualTo("en-US"));
            Assert.That(article.GetDirection(), Is.EqualTo("ltr"));
            Assert.That(article.GetPublishedTimeText(),
                Is.EqualTo("2026-08-11T12:30:00Z"));
            Assert.That(article.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/story"));
            Assert.That(article.GetImageUrl(),
                Is.EqualTo("https://example.test/lead.jpg"));
            Assert.That(article.GetContent().GetOwnerDocument(),
                Is.Not.SameAs(document));
            Assert.That(article.GetContent().FindByTag("aside"), Is.Empty);
            Assert.That(article.GetLength(), Is.EqualTo(article.GetContent()
                .GetNormalizedTextContent().Length));
            Assert.That(article.GetSanitizedHtml(), Is.Null);
            Assert.That(article.GetText(), Is.Null);
            Assert.That(article.GetMarkdown(), Is.Null);
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Has.None.EqualTo(
                    HtmlDiagnosticCode.ArticleExtractionFailed));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Has.None.EqualTo(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void FallsBackToArticleMarkupForOptionalFields()
    {
        string summary = "A concise opening paragraph, with enough useful "
            + "detail to serve as the article excerpt.";
        HtmlDocument document = ParseWithBase(
            "<article id=article lang=fr_ca dir=RTL>"
            + "<h1>Fallback heading</h1>"
            + "<p class=article-byline>Marie Example</p>"
            + "<time datetime='2026-08-10'>Yesterday</time>"
            + "<p id=summary>" + summary + "</p>"
            + "<p>" + Prose() + "</p>"
            + "<figure><img src='images/lead.jpg' alt=Lead></figure>"
            + "</article>", "https://example.test/news/");

        HtmlArticleResult result = HtmlArticleExtractor.Extract(document,
            HtmlArticleOptions.CreateDefault());
        HtmlArticle article = result.GetArticle();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(article.GetTitle(), Is.EqualTo("Fallback heading"));
            Assert.That(article.GetByline(), Is.EqualTo("Marie Example"));
            Assert.That(article.GetExcerpt(), Is.EqualTo(summary));
            Assert.That(article.GetSiteName(), Is.Null);
            Assert.That(article.GetLanguage(), Is.EqualTo("fr-CA"));
            Assert.That(article.GetDirection(), Is.EqualTo("rtl"));
            Assert.That(article.GetPublishedTimeText(),
                Is.EqualTo("2026-08-10"));
            Assert.That(article.GetCanonicalUrl(), Is.Null);
            Assert.That(article.GetImageUrl(),
                Is.EqualTo("https://example.test/news/images/lead.jpg"));
        });
    }

    [Test]
    public void NoCandidateReturnsCompleteUnsuccessfulResult()
    {
        HtmlDocument document = Html.Parse(
            "<html><body><p>tiny</p><nav>Navigation</nav></body></html>");

        HtmlArticleResult result = HtmlArticleExtractor.Extract(document,
            HtmlArticleOptions.CreateDefault());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetArticle(), Is.Null);
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ArticleExtractionFailed));
        });
    }

    [Test]
    public void ResourceLimitsReturnIncompleteWithoutPartialArticle()
    {
        HtmlDocument document = Html.Parse(
            "<article><p>" + Prose() + "</p></article>");
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumHasWork(1);
        options.SetLimits(limits);

        HtmlArticleResult result = HtmlArticleExtractor.Extract(document,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetArticle(), Is.Null);
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void NonFatalMetadataDiagnosticsRemainOnSuccessfulResults()
    {
        HtmlDocument document = ParseWithBase(
            "<head><meta property=og:image content='https://[::1'></head>"
            + "<body><article><p>" + Prose()
            + "</p></article></body>", "https://example.test/");

        HtmlArticleResult result = HtmlArticleExtractor.Extract(document,
            HtmlArticleOptions.CreateDefault());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(HtmlDiagnosticCode.UrlInvalid));
            Assert.That(result.GetDiagnosticCount(),
                Is.EqualTo(result.GetDiagnostics().Length));
        });
    }

    [Test]
    public void StringFacadePropagatesParseStatusCancellationAndArguments()
    {
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        HtmlArticleResult extracted = Html.ExtractArticle(
            "<article><h1>Title</h1><p>" + Prose() + "</p></article>",
            options, CancellationToken.None);
        HtmlArticleOptions limitedOptions = options.Copy();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(20);
        limitedOptions.SetLimits(limits);
        HtmlArticleResult limited = Html.ExtractArticle(
            "<article><p>" + Prose() + "</p></article>", limitedOptions,
            CancellationToken.None);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(extracted.IsSuccess(), Is.True);
            Assert.That(extracted.GetArticle().GetTitle(),
                Is.EqualTo("Title"));
            Assert.That(limited.IsSuccess(), Is.False);
            Assert.That(limited.IsComplete(), Is.False);
            Assert.That(limited.WasTruncated(), Is.True);
            Assert.That(limited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.InputLimitExceeded));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlArticleExtractor.Extract(document: Html.Parse(
                    "<article><p>" + Prose() + "</p></article>"),
                    options: options, cancellationToken: source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.Extract(null, options));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlArticleExtractor.Extract(Html.Parse("<p>x</p>"), null));
            Assert.Throws<ArgumentNullException>(() =>
                Html.ExtractArticle(null, options, CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                Html.ExtractArticle("<p>x</p>", null,
                    CancellationToken.None));
        });
    }

    static HtmlDocument ParseWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }

    static string Prose()
    {
        return new string('a', 260) + ",,,,,,,,,, useful ending.";
    }
}

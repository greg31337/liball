using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleApiCompletionTests
{
    [Test]
    public void OptionalOutputsAreSanitizedAndPreserveReadableStructures()
    {
        HtmlDocument document = ParseWithBase(
            "<article><h1>Readable title</h1>"
            + "<p>" + Prose() + "</p>"
            + "<p><a href='javascript:alert(1)'>unsafe link</a></p>"
            + "<figure><img src='lead.jpg' onerror='alert(1)'>"
            + "<figcaption>Lead caption</figcaption></figure>"
            + "<table><tr><th>Key</th><td>Value</td></tr></table>"
            + "<pre><code>sample()</code></pre>"
            + "<video src='clip.mp4' controls></video></article>",
            "https://example.test/news/");
        HtmlArticleOptions options = new HtmlArticleOptions();
        options.SetProduceSanitizedHtml(true);
        options.SetProduceText(true);
        options.SetProduceMarkdown(true);

        HtmlArticleResult result = new HtmlArticleExtractorApi().Extract(
            document, options);
        HtmlArticle article = result.GetArticle();
        string sanitized = article.GetSanitizedHtml();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(article.IsContentSanitized(), Is.True);
            Assert.That(sanitized, Does.Contain("<figure>"));
            Assert.That(sanitized, Does.Contain("<table>"));
            Assert.That(sanitized, Does.Contain("<pre><code>sample()"));
            Assert.That(sanitized, Does.Contain("<video"));
            Assert.That(sanitized, Does.Contain(
                "https://example.test/news/lead.jpg"));
            Assert.That(sanitized, Does.Not.Contain("javascript:"));
            Assert.That(sanitized, Does.Not.Contain("onerror"));
            Assert.That(article.GetText(), Does.Contain("Readable title"));
            Assert.That(article.GetText(), Does.Contain("Lead caption"));
            Assert.That(article.GetMarkdown(),
                Does.Contain("Readable title"));
            Assert.That(article.GetMarkdown(), Does.Contain("sample()"));
        });
    }

    [Test]
    public void OutputOptionsAndNestedSettingsCopyDefensively()
    {
        HtmlArticleOptions options = new HtmlArticleOptions();
        HtmlTextOptions textOptions = new HtmlTextOptions();
        textOptions.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        HtmlMarkdownOptions markdownOptions = new HtmlMarkdownOptions();
        markdownOptions.SetNewLine("\r\n");
        options.SetTextOptions(textOptions);
        options.SetMarkdownOptions(markdownOptions);
        options.SetProduceSanitizedHtml(true);
        options.SetProduceText(true);
        options.SetProduceMarkdown(true);

        HtmlArticleOptions copy = options.Copy();
        textOptions.SetLinkStyle(HtmlTextLinkStyle.UrlOnly);
        markdownOptions.SetNewLine("\n");
        HtmlTextOptions exposedText = copy.GetTextOptions();
        exposedText.SetLinkStyle(HtmlTextLinkStyle.TextOnly);
        HtmlMarkdownOptions exposedMarkdown = copy.GetMarkdownOptions();
        exposedMarkdown.SetNewLine("\n");

        Assert.Multiple(() =>
        {
            Assert.That(copy.GetProduceSanitizedHtml(), Is.True);
            Assert.That(copy.GetProduceText(), Is.True);
            Assert.That(copy.GetProduceMarkdown(), Is.True);
            Assert.That(copy.GetTextOptions().GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextAndUrl));
            Assert.That(copy.GetMarkdownOptions().GetNewLine(),
                Is.EqualTo("\r\n"));
            Assert.That(copy.GetSanitizerPolicy().IsElementAllowed("video"),
                Is.True);
            Assert.Throws<ArgumentNullException>(() =>
                options.SetSanitizerPolicy(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetTextOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetMarkdownOptions(null));
        });
    }

    [Test]
    public void CancellablePublicReadabilityPreflightUsesConfiguredScoring()
    {
        HtmlDocument document = Html.Parse(
            "<article><p>" + Prose() + "</p></article>");
        HtmlArticleOptions options = new HtmlArticleOptions();
        options.SetMinimumContentCharacters(10);
        options.SetScoreThreshold(1);
        IHtmlArticleExtractorApi api = new HtmlArticleExtractorApi();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(api.IsProbablyReadableWithCancellation(document,
                options, CancellationToken.None), Is.True);
            Assert.Throws<OperationCanceledException>(() =>
                api.IsProbablyReadableWithCancellation(document, options,
                    cancellation.Token));
        });
    }

    [Test]
    public void TruncatedOptionalTextRetainsAnObservablePartialArticle()
    {
        HtmlDocument document = Html.Parse(
            "<article><h1>Title</h1><p>" + Prose()
            + "</p></article>");
        HtmlArticleOptions options = new HtmlArticleOptions();
        options.SetProduceText(true);
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(24);
        options.SetLimits(limits);

        HtmlArticleResult result = new HtmlArticleExtractorApi().Extract(
            document, options);
        HtmlArticle article = result.GetArticle();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.IsPartial(), Is.True);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.TextConversionLimitExceeded));
            Assert.That(article.IsContentSanitized(), Is.True);
            Assert.That(article.GetSanitizedHtml(), Is.Null);
            Assert.That(article.GetText().Length, Is.LessThanOrEqualTo(24));
            Assert.That(article.GetMarkdown(), Is.Null);
        });
    }

    [Test]
    public void SanitizedHtmlLimitFailsClosedWithObservableDiagnostics()
    {
        HtmlDocument document = Html.Parse(
            "<article><p>" + Prose() + "</p></article>");
        HtmlArticleOptions options = new HtmlArticleOptions();
        options.SetProduceSanitizedHtml(true);
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(24);
        options.SetLimits(limits);

        HtmlArticleResult result = new HtmlArticleExtractorApi().Extract(
            document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.IsPartial(), Is.False);
            Assert.That(result.GetArticle(), Is.Null);
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    static HtmlDocument ParseWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None)
            .GetDocument();
    }

    static string Prose()
    {
        return new string('a', 260) + ",,,,,,,,,, useful ending.";
    }
}

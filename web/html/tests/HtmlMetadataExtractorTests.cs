using System;
using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMetadataExtractorTests
{
    [Test]
    public void ExtractsDocumentBaseLinksAndResolvedUrls()
    {
        HtmlDocument document = ParseDocument(
            "<html lang='fr-CA' dir=rtl><head>"
            + "<title>  Example\n Story </title>"
            + "<base href='/news/'>"
            + "<link rel=canonical href='story?id=1'>"
            + "<link rel=alternate hreflang=en href='../en/story'>"
            + "<link rel=alternate hreflang=fr href='../fr/story'>"
            + "<link rel='alternate stylesheet' href='theme.css'>"
            + "<link rel=apple-touch-icon href='apple.png'>"
            + "<link rel=icon href='../favicon.ico'>"
            + "<link rel=image_src href='images/lead.jpg'>"
            + "</head><body></body></html>",
            "https://example.test/articles/page.html");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Example Story"));
            Assert.That(metadata.GetBaseUrl(),
                Is.EqualTo("https://example.test/news/"));
            Assert.That(metadata.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/news/story?id=1"));
            Assert.That(metadata.GetAlternateUrls(), Is.EqualTo(new[]
            {
                "https://example.test/en/story",
                "https://example.test/fr/story"
            }));
            Assert.That(metadata.GetFaviconUrl(),
                Is.EqualTo("https://example.test/favicon.ico"));
            Assert.That(metadata.GetImageUrl(),
                Is.EqualTo("https://example.test/news/images/lead.jpg"));
            Assert.That(metadata.GetLanguage(), Is.EqualTo("fr-CA"));
            Assert.That(metadata.GetDirection(), Is.EqualTo("rtl"));
            Assert.That(metadata.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ExtractsMetaFamiliesWithDeterministicPrecedence()
    {
        HtmlDocument document = ParseDocument(
            "<html><head><title>Document title</title>"
            + "<meta name=title content='Name title'>"
            + "<meta name='dc.title' content='DC title'>"
            + "<meta name='twitter:title' content='Twitter title'>"
            + "<meta property='og:title' content='OG title'>"
            + "<meta name=description content='Name description'>"
            + "<meta name='twitter:description' content='Twitter desc'>"
            + "<meta property='og:description' content='OG description'>"
            + "<meta property='og:site_name' content='Example News'>"
            + "<meta name=author content='Named Author'>"
            + "<meta name='dc.creator' content='DC Author'>"
            + "<meta property='article:author' content='Article Author'>"
            + "<meta name='twitter:creator' content='@author'>"
            + "<meta name='citation_publication_date' "
            + "content=' 2026-08-10 '><meta property='article:published_time' "
            + "content='2026-08-11T10:30:00Z'>"
            + "<meta name='dcterms.modified' content='2026-08-12'>"
            + "<meta property='article:modified_time' "
            + "content='2026-08-13T09:00:00Z'>"
            + "<meta name='twitter:image' content='../twitter.jpg'>"
            + "<meta property='og:image' content='../lead.jpg'>"
            + "<meta property='og:url' content='../og-story'>"
            + "</head><body></body></html>",
            "https://example.test/news/page.html");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("OG title"));
            Assert.That(metadata.GetDescription(),
                Is.EqualTo("OG description"));
            Assert.That(metadata.GetSiteName(), Is.EqualTo("Example News"));
            Assert.That(metadata.GetAuthor(), Is.EqualTo("Article Author"));
            Assert.That(metadata.GetPublishedTime(),
                Is.EqualTo("2026-08-11T10:30:00Z"));
            Assert.That(metadata.GetModifiedTime(),
                Is.EqualTo("2026-08-13T09:00:00Z"));
            Assert.That(metadata.GetImageUrl(),
                Is.EqualTo("https://example.test/lead.jpg"));
            Assert.That(metadata.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/og-story"));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Title)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.OpenGraph));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Title)
                .Select(value => value.GetValue()), Is.EqualTo(new[]
                {
                    "OG title", "Twitter title", "DC title", "Name title",
                    "Document title"
                }));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.ImageUrl)
                .GetRawValue(), Is.EqualTo("../lead.jpg"));
        });
    }

    [Test]
    public void ExtractsHttpEquivLanguageAndBodyDirectionFallbacks()
    {
        HtmlDocument document = Html.Parse(
            "<html><head>"
            + "<meta http-equiv='Content-Language' content='de-DE, en'>"
            + "<meta http-equiv='Last-Modified' "
            + "content='Wed, 12 Aug 2026 10:00:00 GMT'>"
            + "<meta property=dir content=rtl>"
            + "</head><body dir=ltr></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetLanguage(), Is.EqualTo("de-DE"));
            Assert.That(metadata.GetDirection(), Is.EqualTo("ltr"));
            Assert.That(metadata.GetModifiedTime(),
                Is.EqualTo("Wed, 12 Aug 2026 10:00:00 GMT"));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Language)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.HttpEquiv));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Direction)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.Document));
        });
    }

    [Test]
    public void CanonicalLinkPrecedesOpenGraphAndInvalidUrlsAreIgnored()
    {
        HtmlDocument document = ParseDocument(
            "<html><head>"
            + "<link rel=canonical href='/canonical'>"
            + "<meta property=og:url content='/open-graph'>"
            + "<meta property=og:image content='https://[::1'>"
            + "</head><body></body></html>",
            "https://example.test/page");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/canonical"));
            Assert.That(metadata.GetCandidatesForField(
                HtmlMetadataField.CanonicalUrl).Select(value =>
                    value.GetSource()), Is.EqualTo(new[]
                {
                    HtmlMetadataSource.Link, HtmlMetadataSource.OpenGraph
                }));
            Assert.That(metadata.GetImageUrl(), Is.Null);
            Assert.That(metadata.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(HtmlDiagnosticCode.UrlInvalid));
        });
    }

    [Test]
    public void InternalExtractionHonorsVisitLimitsAndCancellation()
    {
        HtmlDocument document = Html.Parse(
            "<html><head><title>Too late</title></head><body></body></html>");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(2);

        HtmlMetadata limited = HtmlMetadataExtractor.Extract(document,
            limits, CancellationToken.None);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(limited.GetTitle(), Is.Null);
            Assert.That(limited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlMetadataExtractor.Extract(document,
                    HtmlLimits.CreateDefault(), source.Token));
        });
    }

    [Test]
    public void ValidatesRequiredArguments()
    {
        HtmlDocument document = Html.Parse("<p>x</p>");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMetadataExtractor.Extract(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMetadataExtractor.Extract(document, null,
                    CancellationToken.None));
        });
    }

    static HtmlDocument ParseDocument(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }
}

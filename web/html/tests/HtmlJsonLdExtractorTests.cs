using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlJsonLdExtractorTests
{
    [Test]
    public void ExtractsSelectedArticleFieldsFromGraphAndNestedValues()
    {
        HtmlDocument document = ParseDocument(
            "<html><head><base href='/news/'>"
            + "<script type='application/ld+json; charset=utf-8'>"
            + "{\"@context\":\"https://schema.org\",\"@graph\":["
            + "{\"@type\":\"Organization\",\"name\":\"Wrong Site\","
            + "\"image\":\"wrong.jpg\"},"
            + "{\"@type\":\"NewsArticle\","
            + "\"headline\":\"  JSON-LD   headline \","
            + "\"description\":\"Not a selected field\","
            + "\"url\":\"ignored-canonical\","
            + "\"author\":[{\"@type\":\"Person\","
            + "\"name\":\"Alice Author\"},\"Bob Writer\"],"
            + "\"publisher\":{\"@type\":\"Organization\","
            + "\"name\":\"Example News\"},"
            + "\"datePublished\":\"2026-08-10T09:00:00Z\","
            + "\"dateModified\":\"2026-08-11T10:00:00Z\","
            + "\"image\":{\"@type\":\"ImageObject\","
            + "\"contentUrl\":\"images/lead.jpg\"}}]}"
            + "</script></head><body></body></html>",
            "https://example.test/page.html");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(),
                Is.EqualTo("JSON-LD headline"));
            Assert.That(metadata.GetAuthor(), Is.EqualTo("Alice Author"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Author)
                .Select(value => value.GetValue()), Is.EqualTo(new[]
                {
                    "Alice Author", "Bob Writer"
                }));
            Assert.That(metadata.GetSiteName(), Is.EqualTo("Example News"));
            Assert.That(metadata.GetPublishedTime(),
                Is.EqualTo("2026-08-10T09:00:00Z"));
            Assert.That(metadata.GetModifiedTime(),
                Is.EqualTo("2026-08-11T10:00:00Z"));
            Assert.That(metadata.GetImageUrl(), Is.EqualTo(
                "https://example.test/news/images/lead.jpg"));
            Assert.That(metadata.GetDescription(), Is.Null);
            Assert.That(metadata.GetCanonicalUrl(), Is.Null);
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Title)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.JsonLd));
            Assert.That(metadata.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void OpenGraphPrecedesJsonLdWhichPrecedesTwitter()
    {
        HtmlDocument document = Html.Parse(
            "<html><head>"
            + "<meta name=twitter:title content='Twitter title'>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"headline\":\"JSON title\"}"
            + "</script><meta property=og:title content='OG title'>"
            + "</head><body></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("OG title"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Title)
                .Select(value => value.GetSource()), Is.EqualTo(new[]
                {
                    HtmlMetadataSource.OpenGraph,
                    HtmlMetadataSource.JsonLd,
                    HtmlMetadataSource.Twitter
                }));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Title)
                .GetPrecedence(), Is.EqualTo(10));
        });
    }

    [Test]
    public void SupportsSchemaPrefixesAndFullSchemaTermsWithoutContexts()
    {
        HtmlDocument document = ParseDocument(
            "<html><head><script type='APPLICATION/LD+JSON'>"
            + "{\"@context\":\"https://example.invalid/context\","
            + "\"@type\":\"https://schema.org/BlogPosting\","
            + "\"schema:headline\":\"Prefixed headline\","
            + "\"https://schema.org/datePublished\":\"2026-07-01\","
            + "\"schema:image\":\"/prefixed.jpg\"}"
            + "</script></head><body></body></html>",
            "https://example.test/page");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(),
                Is.EqualTo("Prefixed headline"));
            Assert.That(metadata.GetPublishedTime(),
                Is.EqualTo("2026-07-01"));
            Assert.That(metadata.GetImageUrl(),
                Is.EqualTo("https://example.test/prefixed.jpg"));
            Assert.That(metadata.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DoesNotInterpretContextAliasesOrUnrelatedTypedObjects()
    {
        HtmlDocument document = Html.Parse(
            "<html><head><script type=application/ld+json>"
            + "{\"@context\":{\"h\":\"https://schema.org/headline\","
            + "\"headline\":\"Context trap\"},"
            + "\"@graph\":[{\"@type\":\"Person\","
            + "\"headline\":\"Person headline\","
            + "\"image\":\"person.jpg\"},"
            + "{\"@type\":\"Article\",\"h\":\"Aliased headline\","
            + "\"description\":\"Ignored description\"}]}"
            + "</script><script type=application/json>not valid json"
            + "</script><script type=application/ld+json src='remote.json'>"
            + "</script></head><body></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.Null);
            Assert.That(metadata.GetImageUrl(), Is.Null);
            Assert.That(metadata.GetDescription(), Is.Null);
            Assert.That(metadata.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void InvalidBlockDoesNotPreventLaterValidBlock()
    {
        HtmlDocument document = Html.Parse(
            "<html><head><script type=application/ld+json>"
            + "{\"@type\":\"Article\", broken}</script>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"headline\":\"Recovered\"}"
            + "</script></head><body></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Recovered"));
            Assert.That(metadata.GetDiagnostics().Count(value =>
                value.GetCode()
                    == HtmlDiagnosticCode.MetadataInvalidJsonLd),
                Is.EqualTo(1));
            Assert.That(metadata.GetDiagnostics()[0].GetSpan().IsKnown(),
                Is.True);
        });
    }

    [Test]
    public void EnforcesJsonInputDepthAndPropertyLimits()
    {
        HtmlDocument inputDocument = Html.Parse(
            "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"headline\":\"Too large\"}"
            + "</script>");
        HtmlLimits inputLimits = HtmlLimits.CreateDefault();
        inputLimits.SetMaximumTokenCharacters(16);
        HtmlMetadata inputLimited = HtmlMetadataExtractor.Extract(
            inputDocument, inputLimits, CancellationToken.None);

        HtmlDocument depthDocument = Html.Parse(
            "<script type=application/ld+json>"
            + "{\"@graph\":[{\"nested\":{\"nested\":{"
            + "\"headline\":\"Too deep\"}}}]}"
            + "</script>");
        HtmlLimits depthLimits = HtmlLimits.CreateDefault();
        depthLimits.SetMaximumJsonDepth(2);
        HtmlMetadata depthLimited = HtmlMetadataExtractor.Extract(
            depthDocument, depthLimits, CancellationToken.None);

        HtmlDocument propertyDocument = Html.Parse(
            "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"headline\":\"Too many\"}"
            + "</script>");
        HtmlLimits propertyLimits = HtmlLimits.CreateDefault();
        propertyLimits.SetMaximumJsonProperties(1);
        HtmlMetadata propertyLimited = HtmlMetadataExtractor.Extract(
            propertyDocument, propertyLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(inputLimited.GetTitle(), Is.Null);
            Assert.That(inputLimited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(depthLimited.GetTitle(), Is.Null);
            Assert.That(depthLimited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.MetadataInvalidJsonLd));
            Assert.That(propertyLimited.GetTitle(), Is.Null);
            Assert.That(propertyLimited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    static HtmlDocument ParseDocument(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }
}

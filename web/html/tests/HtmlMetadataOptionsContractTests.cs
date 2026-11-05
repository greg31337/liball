using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMetadataOptionsContractTests
{
    [Test]
    public void DefaultsIncludeEveryFieldAndSourceAndCopyDefensively()
    {
        HtmlMetadataOptions options = new HtmlMetadataOptions();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(123);
        options.SetLimits(limits);
        limits.SetMaximumVisits(456);
        options.SetIncludeField(HtmlMetadataField.ImageUrl, false);
        options.SetIncludeSource(HtmlMetadataSource.JsonLd, false);

        HtmlMetadataOptions copy = options.Copy();
        options.SetIncludeField(HtmlMetadataField.ImageUrl, true);
        options.SetIncludeSource(HtmlMetadataSource.JsonLd, true);
        HtmlLimits exposed = copy.GetLimits();
        exposed.SetMaximumVisits(789);

        Assert.Multiple(() =>
        {
            Assert.That(new HtmlMetadataOptions().GetIncludeField(
                HtmlMetadataField.Title), Is.True);
            Assert.That(new HtmlMetadataOptions().GetIncludeSource(
                HtmlMetadataSource.OpenGraph), Is.True);
            Assert.That(copy.GetIncludeField(HtmlMetadataField.ImageUrl),
                Is.False);
            Assert.That(copy.GetIncludeSource(HtmlMetadataSource.JsonLd),
                Is.False);
            Assert.That(copy.GetLimits().GetMaximumVisits(), Is.EqualTo(123));
            Assert.That(options.GetLimits().GetMaximumVisits(),
                Is.EqualTo(123));
        });
    }

    [Test]
    public void SourceAndFieldControlsFilterCandidatesBeforeResolution()
    {
        HtmlDocument document = Html.Parse(
            "<html><head><title>Document</title>"
            + "<meta name=title content='Standard'>"
            + "<meta name='dc.title' content='Dublin'>"
            + "<meta name='twitter:title' content='Twitter'>"
            + "<meta property='og:title' content='Open Graph'>"
            + "<meta property='og:description' content='Description'>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"headline\":\"JSON-LD\"}"
            + "</script></head><body></body></html>");
        HtmlMetadataOptions sourceOptions = new HtmlMetadataOptions();
        sourceOptions.SetIncludeSource(HtmlMetadataSource.OpenGraph, false);
        sourceOptions.SetIncludeSource(HtmlMetadataSource.JsonLd, false);
        IHtmlMetadataExtractorApi api = new HtmlMetadataExtractorApi();

        HtmlMetadata sourceFiltered = api.ExtractWithOptions(document,
            sourceOptions);
        HtmlMetadataOptions fieldOptions = new HtmlMetadataOptions();
        fieldOptions.SetIncludeField(HtmlMetadataField.Title, false);
        HtmlMetadata fieldFiltered = api.ExtractWithOptions(document,
            fieldOptions);

        Assert.Multiple(() =>
        {
            Assert.That(sourceFiltered.GetTitle(), Is.EqualTo("Twitter"));
            Assert.That(sourceFiltered.GetCandidatesForField(
                HtmlMetadataField.Title).Select(value => value.GetSource()),
                Is.EqualTo(new[]
                {
                    HtmlMetadataSource.Twitter,
                    HtmlMetadataSource.DublinCore,
                    HtmlMetadataSource.MetaName,
                    HtmlMetadataSource.Document
                }));
            Assert.That(sourceFiltered.GetDescription(), Is.Null);
            Assert.That(fieldFiltered.GetTitle(), Is.Null);
            Assert.That(fieldFiltered.GetCandidatesForField(
                HtmlMetadataField.Title), Is.Empty);
            Assert.That(fieldFiltered.GetDescription(),
                Is.EqualTo("Description"));
        });
    }

    [Test]
    public void FieldControlsCoverLinkDocumentAndJsonLdCandidates()
    {
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetBaseUrl("https://example.test/page");
        HtmlDocument document = Html.Parse(
            "<html lang=en dir=rtl><head><base href='/news/'>"
            + "<link rel=canonical href=story>"
            + "<link rel=alternate href=other>"
            + "<link rel=icon href=icon.png>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"Article\",\"author\":\"Ada\","
            + "\"publisher\":\"Example\","
            + "\"datePublished\":\"2026-01-02\","
            + "\"dateModified\":\"2026-01-03\","
            + "\"image\":\"lead.jpg\"}</script>"
            + "</head><body></body></html>", parseOptions,
            CancellationToken.None).GetDocument();
        HtmlMetadataOptions options = new HtmlMetadataOptions();
        options.SetIncludeField(HtmlMetadataField.BaseUrl, false);
        options.SetIncludeField(HtmlMetadataField.AlternateUrl, false);
        options.SetIncludeField(HtmlMetadataField.Language, false);
        options.SetIncludeField(HtmlMetadataField.Direction, false);
        options.SetIncludeField(HtmlMetadataField.FaviconUrl, false);
        options.SetIncludeField(HtmlMetadataField.Author, false);
        options.SetIncludeField(HtmlMetadataField.PublishedTime, false);
        options.SetIncludeField(HtmlMetadataField.ModifiedTime, false);
        options.SetIncludeField(HtmlMetadataField.SiteName, false);
        options.SetIncludeField(HtmlMetadataField.ImageUrl, false);

        HtmlMetadata metadata = new HtmlMetadataExtractorApi()
            .ExtractWithOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetBaseUrl(), Is.Null);
            Assert.That(metadata.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/news/story"));
            Assert.That(metadata.GetAlternateUrls(), Is.Empty);
            Assert.That(metadata.GetLanguage(), Is.Null);
            Assert.That(metadata.GetDirection(), Is.Null);
            Assert.That(metadata.GetFaviconUrl(), Is.Null);
            Assert.That(metadata.GetAuthor(), Is.Null);
            Assert.That(metadata.GetPublishedTime(), Is.Null);
            Assert.That(metadata.GetModifiedTime(), Is.Null);
            Assert.That(metadata.GetSiteName(), Is.Null);
            Assert.That(metadata.GetImageUrl(), Is.Null);
            Assert.That(metadata.GetCandidates().Select(value =>
                value.GetField()), Is.EqualTo(new[]
                {
                    HtmlMetadataField.CanonicalUrl
                }));
        });
    }

    [Test]
    public void PublicOverloadsHonorLimitsCancellationAndArguments()
    {
        HtmlDocument document = Html.Parse(
            "<html><head><title>Too late</title></head><body></body></html>");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(2);
        HtmlMetadataOptions options = new HtmlMetadataOptions();
        options.SetLimits(limits);
        IHtmlMetadataExtractorApi api = new HtmlMetadataExtractorApi();
        HtmlMetadata limited = api.ExtractWithOptions(document, options);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(limited.GetTitle(), Is.Null);
            Assert.That(limited.IsPartial(), Is.True);
            Assert.That(limited.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.Throws<OperationCanceledException>(() =>
                api.ExtractWithCancellation(document,
                    new HtmlMetadataOptions(), cancellation.Token));
            Assert.Throws<ArgumentNullException>(() =>
                api.ExtractWithOptions(null, options));
            Assert.Throws<ArgumentNullException>(() =>
                api.ExtractWithOptions(document, null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetLimits(null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetIncludeField((HtmlMetadataField)0, true));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetIncludeSource((HtmlMetadataSource)0, true));
        });
    }
}

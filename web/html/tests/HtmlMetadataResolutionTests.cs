using System.Linq;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMetadataResolutionTests
{
    [Test]
    public void MultilingualNewsFixtureNormalizesUnicodeLanguageAndDirection()
    {
        HtmlDocument document = Html.Parse(
            "<html lang='ZH_hant_tw' dir='RTL'><head>"
            + "<title>Cafe\u0301 世界</title>"
            + "<meta property=og:title content='  Café\u00A0世界  '>"
            + "<meta property=og:locale content='zh_TW'>"
            + "<meta name=description content=' 日本語\u3000の   説明 '>"
            + "</head><body></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Café 世界"));
            Assert.That(metadata.GetLanguage(), Is.EqualTo("zh-Hant-TW"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Language)
                .Select(value => value.GetValue()), Is.EqualTo(new[]
                {
                    "zh-Hant-TW", "zh-TW"
                }));
            Assert.That(metadata.GetDirection(), Is.EqualTo("rtl"));
            Assert.That(metadata.GetDescription(),
                Is.EqualTo("日本語 の 説明"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Title),
                Has.Length.EqualTo(1));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Title)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.OpenGraph));
            Assert.That(metadata.GetDuplicateCandidateCount(), Is.EqualTo(1));
            Assert.That(metadata.WereCandidatesTruncated(), Is.False);
        });
    }

    [Test]
    public void RealWorldDuplicateMarkupRetainsOnlyHighestPriorityEvidence()
    {
        HtmlDocument document = ParseDocument(
            "<html><head><title> Same headline </title>"
            + "<meta property=og:title content='Same\u00A0headline'>"
            + "<meta name=twitter:title content='Same headline'>"
            + "<link rel=canonical "
            + "href='https://EXAMPLE.test/a/../story'>"
            + "<meta property=og:url content='/story'>"
            + "<link rel=alternate href='/story?lang=en'>"
            + "<link rel=alternate "
            + "href='https://EXAMPLE.test/story?lang=en'>"
            + "<meta property=og:image content='/lead.jpg'>"
            + "<meta name=twitter:image "
            + "content='https://EXAMPLE.test/lead.jpg'>"
            + "<meta name=author content='Jane Doe'>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"NewsArticle\","
            + "\"author\":{\"name\":\"Jane Doe\"}}"
            + "</script></head><body></body></html>",
            "https://example.test/page.html");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Same headline"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Title),
                Has.Length.EqualTo(1));
            Assert.That(metadata.GetCanonicalUrl(),
                Is.EqualTo("https://example.test/story"));
            Assert.That(metadata.GetCandidatesForField(
                HtmlMetadataField.CanonicalUrl), Has.Length.EqualTo(1));
            Assert.That(metadata.GetAlternateUrls(), Is.EqualTo(new[]
            {
                "https://example.test/story?lang=en"
            }));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.ImageUrl),
                Has.Length.EqualTo(1));
            Assert.That(metadata.GetAuthor(), Is.EqualTo("Jane Doe"));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Author)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.JsonLd));
            Assert.That(metadata.GetCandidateCount(), Is.EqualTo(12));
            Assert.That(metadata.GetRetainedCandidateCount(), Is.EqualTo(6));
            Assert.That(metadata.GetDuplicateCandidateCount(), Is.EqualTo(6));
            Assert.That(metadata.GetDroppedCandidateCount(), Is.Zero);
            Assert.That(metadata.WereCandidatesTruncated(), Is.False);
        });
    }

    [Test]
    public void RepresentativeSelectionPrefersNamesAndSecureImages()
    {
        HtmlDocument document = ParseDocument(
            "<html><head>"
            + "<meta property=article:author "
            + "content='https://example.test/authors/profile'>"
            + "<meta name=author content='Jane Editor'>"
            + "<script type=application/ld+json>"
            + "{\"@type\":\"Article\","
            + "\"author\":{\"name\":\"Jane Reporter\"}}"
            + "</script>"
            + "<meta property=og:image content='http://cdn.test/lead.jpg'>"
            + "<meta property=og:image:secure_url "
            + "content='https://cdn.test/lead.jpg'>"
            + "</head><body></body></html>",
            "https://example.test/page");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetAuthor(), Is.EqualTo("Jane Reporter"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Author)
                .Select(value => value.GetSource()), Is.EqualTo(new[]
                {
                    HtmlMetadataSource.JsonLd,
                    HtmlMetadataSource.MetaName,
                    HtmlMetadataSource.OpenGraph
                }));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Author)
                .Last().GetPrecedence(), Is.EqualTo(60));
            Assert.That(metadata.GetImageUrl(),
                Is.EqualTo("https://cdn.test/lead.jpg"));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.ImageUrl)
                .GetPrecedence(), Is.EqualTo(9));
        });
    }

    [Test]
    public void LanguageExtensionsNormalizeWithoutCultureSensitiveCasing()
    {
        HtmlDocument document = Html.Parse(
            "<html lang='EN_us_u_CA_gregory'><head>"
            + "<meta property=og:locale "
            + "content='en-US-u-ca-gregory'>"
            + "</head><body></body></html>");

        HtmlMetadata metadata = HtmlMetadataExtractor.Extract(document);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetLanguage(),
                Is.EqualTo("en-US-u-ca-gregory"));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Language),
                Has.Length.EqualTo(1));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Language)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.Document));
            Assert.That(metadata.GetDuplicateCandidateCount(), Is.EqualTo(1));
        });
    }

    static HtmlDocument ParseDocument(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }
}

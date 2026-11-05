using System;
using System.Linq;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlExtractionModelTests
{
    [Test]
    public void MetadataUsesExplicitPrecedenceThenDocumentAndSourceOrder()
    {
        HtmlMetadataCandidateCollector collector =
            new HtmlMetadataCandidateCollector(10, 100);
        collector.Add(HtmlMetadataField.Title, "Twitter", "twitter raw",
            HtmlMetadataSource.Twitter, 20, 0);
        collector.Add(HtmlMetadataField.Title, "Later", "later raw",
            HtmlMetadataSource.OpenGraph, 10, 5);
        collector.Add(HtmlMetadataField.Title, "Selected", "selected raw",
            HtmlMetadataSource.MetaName, 10, 1);
        collector.Add(HtmlMetadataField.Title, "Same position", "same raw",
            HtmlMetadataSource.OpenGraph, 10, 1);
        HtmlMetadata metadata = collector.CreateMetadata(
            Array.Empty<HtmlDiagnostic>(), 0, 0);

        HtmlMetadataCandidate[] candidates =
            metadata.GetCandidatesForField(HtmlMetadataField.Title);
        candidates[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Selected"));
            Assert.That(metadata.GetCandidate(HtmlMetadataField.Title)
                .GetSource(), Is.EqualTo(HtmlMetadataSource.MetaName));
            Assert.That(metadata.GetCandidatesForField(HtmlMetadataField.Title)
                .Select(value => value.GetValue()), Is.EqualTo(new[]
                {
                    "Selected", "Same position", "Later", "Twitter"
                }));
            Assert.That(metadata.GetDescription(), Is.Null);
            Assert.That(metadata.GetCandidate(
                HtmlMetadataField.Description), Is.Null);
        });
    }

    [Test]
    public void MetadataBoundsCandidateAndRawValueRetention()
    {
        HtmlMetadataCandidateCollector collector =
            new HtmlMetadataCandidateCollector(2, 5);
        Assert.That(collector.Add(HtmlMetadataField.Title, "Title",
            "abcdef", HtmlMetadataSource.Document, 0, 0), Is.True);
        Assert.That(collector.Add(HtmlMetadataField.Description,
            "Description", "secret", HtmlMetadataSource.MetaName, 0, 1),
            Is.True);
        Assert.That(collector.Add(HtmlMetadataField.Author, "Author",
            "another secret", HtmlMetadataSource.MetaName, 0, 2), Is.False);

        HtmlMetadata metadata = collector.CreateMetadata(
            Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlMetadataCandidate title = metadata.GetCandidate(
            HtmlMetadataField.Title);
        HtmlMetadataCandidate description = metadata.GetCandidate(
            HtmlMetadataField.Description);

        Assert.Multiple(() =>
        {
            Assert.That(title.GetRawValue(), Is.EqualTo("abcde"));
            Assert.That(title.WasRawValueTruncated(), Is.True);
            Assert.That(description.GetRawValue(), Is.Empty);
            Assert.That(description.WasRawValueTruncated(), Is.True);
            Assert.That(collector.GetRetainedRawCharacterCount(),
                Is.EqualTo(5));
            Assert.That(metadata.GetCandidateCount(), Is.EqualTo(3));
            Assert.That(metadata.GetDroppedCandidateCount(), Is.EqualTo(1));
            Assert.That(metadata.WereCandidatesTruncated(), Is.True);
            Assert.That(metadata.WereRawValuesTruncated(), Is.True);
            Assert.That(metadata.GetAuthor(), Is.Null);
        });
    }

    [Test]
    public void MetadataReturnsSnapshotsForAlternatesAndDiagnostics()
    {
        HtmlMetadataCandidateCollector collector =
            new HtmlMetadataCandidateCollector(4, 20);
        collector.Add(HtmlMetadataField.AlternateUrl,
            "https://example.test/en", null, HtmlMetadataSource.Link, 0, 1);
        collector.Add(HtmlMetadataField.AlternateUrl,
            "https://example.test/fr", null, HtmlMetadataSource.Link, 0, 2);
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.MetadataInvalidJsonLd,
            HtmlDiagnosticSeverity.Warning, "Ignored metadata block.",
            HtmlSourceSpan.CreateUnknown());
        HtmlMetadata metadata = collector.CreateMetadata(
            new[] { diagnostic }, 1, 0);

        string[] alternates = metadata.GetAlternateUrls();
        HtmlDiagnostic[] diagnostics = metadata.GetDiagnostics();
        HtmlMetadataCandidate[] candidates = metadata.GetCandidates();
        alternates[0] = "changed";
        diagnostics[0] = null;
        candidates[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetAlternateUrls(), Is.EqualTo(new[]
            {
                "https://example.test/en", "https://example.test/fr"
            }));
            Assert.That(metadata.GetDiagnostics()[0], Is.SameAs(diagnostic));
            Assert.That(metadata.GetCandidates(), Has.None.Null);
            Assert.That(metadata.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(metadata.WereDiagnosticsTruncated(), Is.False);
        });
    }

    [Test]
    public void ArticleOptionsCopyThresholdsAndLimitsDefensively()
    {
        HtmlArticleOptions options = HtmlArticleOptions.CreateDefault();
        options.SetMinimumContentCharacters(200);
        options.SetMinimumCandidateCharacters(30);
        options.SetScoreThreshold(25);
        options.SetMaximumTopCandidates(7);
        options.SetRemoveUnlikelyCandidates(false);
        options.SetWeightClassesAndIds(false);
        options.SetCleanConditionally(false);
        HtmlLimits limits = HtmlLimits.CreateUntrusted();
        limits.SetMaximumVisits(1234);
        options.SetLimits(limits);
        limits.SetMaximumVisits(9999);

        HtmlArticleOptions copy = options.Copy();
        copy.SetScoreThreshold(40);
        HtmlLimits exposed = copy.GetLimits();
        exposed.SetMaximumVisits(55);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetMinimumContentCharacters(),
                Is.EqualTo(200));
            Assert.That(options.GetMinimumCandidateCharacters(),
                Is.EqualTo(30));
            Assert.That(options.GetScoreThreshold(), Is.EqualTo(25));
            Assert.That(options.GetMaximumTopCandidates(), Is.EqualTo(7));
            Assert.That(options.GetRemoveUnlikelyCandidates(), Is.False);
            Assert.That(options.GetWeightClassesAndIds(), Is.False);
            Assert.That(options.GetCleanConditionally(), Is.False);
            Assert.That(options.GetLimits().GetMaximumVisits(),
                Is.EqualTo(1234));
            Assert.That(copy.GetScoreThreshold(), Is.EqualTo(40));
            Assert.That(copy.GetLimits().GetMaximumVisits(),
                Is.EqualTo(1234));
            Assert.DoesNotThrow(() => options.Validate());
        });
    }

    [Test]
    public void ArticleOptionsRejectInvalidThresholds()
    {
        HtmlArticleOptions options = new HtmlArticleOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMinimumContentCharacters(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMinimumCandidateCharacters(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetScoreThreshold(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumTopCandidates(0));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetLimits(null));
        });
    }

    [Test]
    public void ArticleAndResultExposeOptionalOutputsAndSnapshotDiagnostics()
    {
        HtmlNode content = Html.ParseFragment("<article>Text</article>",
            "body").GetFragment();
        HtmlArticle article = new HtmlArticle("Title", null, null, "Site",
            "en", "ltr", null, "https://example.test/article", null,
            content, null, "Text", null, 4);
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.OperationIncomplete,
            HtmlDiagnosticSeverity.Information, "Markdown not requested.",
            HtmlSourceSpan.CreateUnknown());
        HtmlArticleResult result = new HtmlArticleResult(true, true, false,
            article, new[] { diagnostic }, 1, 0);

        HtmlDiagnostic[] diagnostics = result.GetDiagnostics();
        diagnostics[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetArticle(), Is.SameAs(article));
            Assert.That(article.GetTitle(), Is.EqualTo("Title"));
            Assert.That(article.GetByline(), Is.Null);
            Assert.That(article.GetExcerpt(), Is.Null);
            Assert.That(article.GetContent(), Is.SameAs(content));
            Assert.That(article.GetSanitizedHtml(), Is.Null);
            Assert.That(article.GetText(), Is.EqualTo("Text"));
            Assert.That(article.GetMarkdown(), Is.Null);
            Assert.That(article.GetLength(), Is.EqualTo(4));
            Assert.That(result.GetDiagnostics()[0], Is.SameAs(diagnostic));
            Assert.That(result.WereDiagnosticsTruncated(), Is.False);
        });
    }

    [Test]
    public void UnsuccessfulArticleResultHasNoArticle()
    {
        HtmlArticleResult result = new HtmlArticleResult(false, true, false,
            null, Array.Empty<HtmlDiagnostic>(), 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetArticle(), Is.Null);
            Assert.Throws<ArgumentException>(() => new HtmlArticleResult(
                true, true, false, null, Array.Empty<HtmlDiagnostic>(),
                0, 0));
        });
    }
}

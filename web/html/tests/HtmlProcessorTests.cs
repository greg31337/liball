using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlProcessorTests
{
    [Test]
    public void RichTextPipelineSanitizesAndSerializesCorrectedMarkup()
    {
        string input = "<div><script>alert(1)</script>"
            + "<p>Hello <a href='javascript:alert(2)'>link</a></p></div>";

        HtmlProcessingResult result = Html.Process(input,
            HtmlProcessingOptions.CreateUntrustedRichText(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetDocument(), Is.Not.Null);
            Assert.That(result.GetSanitizedNode(), Is.Not.Null);
            Assert.That(result.GetSanitizerReport(), Is.Not.Null);
            Assert.That(result.GetHtml(), Does.Contain("<p>"));
            Assert.That(result.GetHtml(), Does.Not.Contain("script"));
            Assert.That(result.GetHtml(), Does.Not.Contain("javascript"));
            Assert.That(result.GetText(), Is.Null);
            Assert.That(result.GetMarkdown(), Is.Null);
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Decode),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Parse),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Sanitize),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Serialize),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Metadata),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
            Assert.That(result.GetStageTimings(), Has.Length.EqualTo(8));
            Assert.That(result.GetStageTimings().All(value =>
                value.GetElapsedTicks() >= 0), Is.True);
        });
    }

    [Test]
    public void SearchPipelineReturnsMetadataAndFocusedPlainText()
    {
        string input = "<!doctype html><html><head>"
            + "<title>Search title</title></head><body>"
            + "<nav>Navigation</nav><main><p>Indexed content</p>"
            + "<img alt='ignored' src='https://example.test/image.png'>"
            + "</main></body></html>";

        HtmlProcessingResult result = HtmlProcessor.Process(input,
            HtmlProcessingOptions.CreateSearchIndex(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetMetadata().GetTitle(),
                Is.EqualTo("Search title"));
            Assert.That(result.GetText(), Does.Contain("Indexed content"));
            Assert.That(result.GetText(), Does.Not.Contain("Navigation"));
            Assert.That(result.GetText(), Does.Not.Contain("ignored"));
            Assert.That(result.GetHtml(), Is.Null);
            Assert.That(result.GetArticle(), Is.Null);
            Assert.That(result.GetMarkdown(), Is.Null);
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Metadata),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Article),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Text),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Markdown),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
        });
    }

    [Test]
    public void ArticlePipelineProducesEveryRequestedRepresentation()
    {
        string paragraph = "This is substantial article content with enough "
            + "words, punctuation, and detail to satisfy readability scoring. ";
        string input = "<!doctype html><html lang='en'><head>"
            + "<title>Pipeline article</title>"
            + "<meta name='author' content='Ada Writer'></head><body>"
            + "<article><h1>Pipeline article</h1><p>"
            + paragraph + paragraph + paragraph
            + "</p><p>" + paragraph + paragraph
            + "</p></article></body></html>";

        HtmlProcessingResult result = HtmlProcessor.Process(input,
            HtmlProcessingOptions.CreateArticle(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetMetadata().GetTitle(),
                Is.EqualTo("Pipeline article"));
            Assert.That(result.GetArticleResult().IsSuccess(), Is.True);
            Assert.That(result.GetArticle().GetTitle(),
                Is.EqualTo("Pipeline article"));
            Assert.That(result.GetHtml(), Does.Contain("Pipeline article"));
            Assert.That(result.GetText(), Does.Contain("substantial article"));
            Assert.That(result.GetMarkdown(),
                Does.Contain("# Pipeline article"));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Article),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Text),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Markdown),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
        });
    }

    [Test]
    public void BytePipelineReportsDecodeStatusAndDetectedEncoding()
    {
        byte[] input = Encoding.UTF8.GetPreamble().Concat(
            Encoding.UTF8.GetBytes("<title>Bytes</title><p>decoded text</p>"))
            .ToArray();

        HtmlProcessingResult result = Html.Process(input,
            HtmlProcessingOptions.CreateSearchIndex(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDetectedEncodingLabel(),
                Is.EqualTo("UTF-8"));
            Assert.That(result.GetEncodingDetection(), Is.Not.Null);
            Assert.That(result.GetText(), Does.Contain("decoded text"));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Decode),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Parse),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageTiming(HtmlProcessingStage.Decode)
                .GetElapsedTicks(), Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void VisitBudgetAccumulatesAcrossRequestedStages()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateSearchIndex();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(7);
        options.SetLimits(limits);

        HtmlProcessingResult result = HtmlProcessor.Process(
            "<p>one</p>", options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Metadata),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Sanitize),
                Is.EqualTo(HtmlProcessingStageStatus.Incomplete));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Text),
                Is.EqualTo(HtmlProcessingStageStatus.Skipped));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.PipelineLimitExceeded));
        });
    }

    [Test]
    public void ResultsOwnTimingArraysAndArgumentsAreValidated()
    {
        HtmlProcessingResult result = HtmlProcessor.Process("<p>text</p>",
            HtmlProcessingOptions.CreateUntrustedRichText(),
            CancellationToken.None);
        HtmlProcessingStageTiming[] timings = result.GetStageTimings();
        timings[0] = null;
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetStageTimings()[0], Is.Not.Null);
            Assert.Throws<ArgumentNullException>(() =>
                HtmlProcessor.Process((string)null,
                    HtmlProcessingOptions.CreateUntrustedRichText(),
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlProcessor.Process("", null, CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlProcessor.Process((byte[])null,
                    HtmlProcessingOptions.CreateUntrustedRichText(),
                    CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlProcessor.Process("<p>text</p>",
                    HtmlProcessingOptions.CreateUntrustedRichText(),
                    cancellation.Token));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                result.GetStageTiming((HtmlProcessingStage)99));
        });
    }

    [Test]
    public void ArticlePipelineProjectsOnlySanitizedRenderableContent()
    {
        string paragraph = "This article contains enough meaningful prose, "
            + "punctuation, context, and detail for readable extraction. ";
        string input = "<html><head><title>Security article</title></head>"
            + "<body><article onclick='steal()'><h1>Security article</h1>"
            + "<p style='position:fixed'>" + paragraph + paragraph
            + paragraph + "<a href='javascript:steal()'>unsafe link</a>"
            + "</p><p>" + paragraph + paragraph
            + "</p></article></body></html>";
        HtmlArticleResult extraction = Html.ExtractArticle(input,
            HtmlArticleOptions.CreateDefault(), CancellationToken.None);
        HtmlArticle unsanitized = extraction.GetUnsanitizedArticle();
        string unsanitizedHtml = HtmlSerializer.Serialize(
            unsanitized.GetContent());

        HtmlProcessingResult result = Html.Process(input,
            HtmlProcessingOptions.CreateArticle(),
            CancellationToken.None);
        HtmlArticle processed = result.GetArticle();
        HtmlArticle retainedExtraction = result.GetExtractedArticle();
        string processedContent = HtmlSerializer.Serialize(
            processed.GetContent());

        Assert.Multiple(() =>
        {
            Assert.That(extraction.IsSuccess(), Is.True);
            Assert.That(unsanitized.IsContentSanitized(), Is.False);
            Assert.That(unsanitized.GetSanitizedHtml(), Is.Null);
            Assert.That(unsanitizedHtml, Does.Contain("onclick"));
            Assert.That(retainedExtraction.IsContentSanitized(), Is.False);
            Assert.That(processed, Is.Not.SameAs(retainedExtraction));
            Assert.That(processed.IsContentSanitized(), Is.True);
            Assert.That(processed.GetSanitizedHtml(),
                Is.EqualTo(result.GetHtml()));
            Assert.That(processed.GetText(), Is.EqualTo(result.GetText()));
            Assert.That(processed.GetMarkdown(),
                Is.EqualTo(result.GetMarkdown()));
            Assert.That(processedContent, Does.Not.Contain("onclick"));
            Assert.That(processedContent, Does.Not.Contain("javascript"));
            Assert.That(result.GetHtml(), Does.Not.Contain("onclick"));
            Assert.That(result.GetHtml(), Does.Not.Contain("javascript"));
            Assert.That(result.GetSanitizerReport().GetTotalCount(),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void LlmArticleRetainsSanitizedNodeWithoutRenderableHtml()
    {
        string paragraph = "Long-form prose gives the extractor enough "
            + "content, punctuation, and context to select this article. ";
        string input = "<article onclick='unsafe()'><h1>LLM article</h1>"
            + "<p>" + paragraph + paragraph + paragraph
            + paragraph + "</p></article>";

        HtmlProcessingResult result = HtmlProcessor.Process(input,
            HtmlProcessingOptions.CreateLlmIngestion(),
            CancellationToken.None);
        HtmlArticle article = result.GetArticle();
        string serializedNode = HtmlSerializer.Serialize(
            article.GetContent());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(article.IsContentSanitized(), Is.True);
            Assert.That(article.GetSanitizedHtml(), Is.Null);
            Assert.That(result.GetHtml(), Is.Null);
            Assert.That(article.GetText(), Is.EqualTo(result.GetText()));
            Assert.That(article.GetMarkdown(),
                Is.EqualTo(result.GetMarkdown()));
            Assert.That(serializedNode, Does.Not.Contain("onclick"));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Serialize),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
        });
    }

    [Test]
    public void StageDiagnosticsAndSanitizerReportRemainAvailable()
    {
        string input = "<title>Reports</title>"
            + "<script type='application/ld+json'>{broken</script>"
            + "<p onclick='unsafe()'>content</p></unexpected>";

        HtmlProcessingResult result = HtmlProcessor.Process(input,
            HtmlProcessingOptions.CreateSearchIndex(),
            CancellationToken.None);
        HtmlDiagnostic[] metadataDiagnostics =
            result.GetStageDiagnostics(HtmlProcessingStage.Metadata);
        metadataDiagnostics[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetStageDiagnosticCount(
                HtmlProcessingStage.Parse), Is.EqualTo(
                    result.GetParseResult().GetDiagnostics().Length));
            Assert.That(result.GetStageDiagnostics(
                HtmlProcessingStage.Metadata).Select(value =>
                    value.GetCode()), Does.Contain(
                        HtmlDiagnosticCode.MetadataInvalidJsonLd));
            Assert.That(result.GetStageDiagnosticCount(
                HtmlProcessingStage.Metadata), Is.EqualTo(
                    result.GetMetadata().GetDiagnosticCount()));
            Assert.That(result.GetStageDiagnostics(
                HtmlProcessingStage.Metadata)[0], Is.Not.Null);
            Assert.That(result.GetSanitizerReport().GetTotalCount(),
                Is.GreaterThan(0));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.MetadataInvalidJsonLd));
            Assert.That(result.GetDiagnosticCount(),
                Is.GreaterThanOrEqualTo(result.GetDiagnostics().Length));
        });
    }

    [Test]
    public void UnsafePartialSerializationIsWithheldAndReported()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateUntrustedRichText();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(5);
        options.SetLimits(limits);

        HtmlProcessingResult result = HtmlProcessor.Process(
            "<p>content</p>", options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetHtml(), Is.Null);
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Serialize),
                Is.EqualTo(HtmlProcessingStageStatus.Incomplete));
            Assert.That(result.GetStageDiagnostics(
                HtmlProcessingStage.Serialize).Select(value =>
                    value.GetCode()), Does.Contain(
                        HtmlDiagnosticCode.SerializationLimitExceeded));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.PipelineLimitExceeded));
        });
    }
}

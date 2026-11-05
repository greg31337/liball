using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicPipelineApiCompletionTests
{
    [Test]
    public void HtmlApiCompletesEveryWholeDocumentParseInputPath()
    {
        IHtmlApi api = new HtmlApi();
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        string source = "<title>Input</title><p>Hello</p>";
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        MemoryStream stream = new MemoryStream(bytes);
        StringReader reader = new StringReader(source);

        HtmlDocument characters = api.ParseCharacters(source.AsSpan());
        HtmlParseResult configuredString = api.ParseStringWithOptions(
            source, options, CancellationToken.None);
        HtmlParseResult byteSpan = api.ParseByteSpanWithOptions(
            bytes.AsSpan(), options, CancellationToken.None);
        HtmlParseResult streamed = api.ParseStream(stream, options,
            CancellationToken.None);
        HtmlParseResult read = api.ParseReader(reader, options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(characters.GetTitle(), Is.EqualTo("Input"));
            Assert.That(new[] { configuredString, byteSpan, streamed, read },
                Has.All.Matches<HtmlParseResult>(value =>
                    value.IsSuccess() && value.IsComplete()
                    && value.GetDocument().GetTitle() == "Input"));
            Assert.That(byteSpan.GetDetectedEncodingLabel(),
                Is.EqualTo("windows-1252"));
        });
    }

    [Test]
    public void HtmlApiCompletesSanitizeMarkdownAndArticleOperations()
    {
        IHtmlApi api = new HtmlApi();
        string sanitized = api.Sanitize(
            "<p onclick=x>safe<script>secret</script></p>");
        HtmlSanitizerPolicy sanitizer =
            new HtmlFactory().CreateRichTextSanitizerPolicy();
        HtmlSanitizeResult configuredSanitized = api.SanitizeWithOptions(
            "<p onclick=x>safe<script>secret</script></p>", sanitizer,
            HtmlParseOptions.CreateDefault(), CancellationToken.None);
        string markdown = api.ToMarkdown("<h1>Title</h1>");
        HtmlMarkdownOptions markdownOptions =
            new HtmlFactory().CreateCommonMarkOptions();
        markdownOptions.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        HtmlMarkdownResult configuredMarkdown =
            api.ToMarkdownWithOptions("<h1>Title</h1>", markdownOptions,
                CancellationToken.None);
        string prose = string.Join(" ", Enumerable.Repeat(
            "Substantial article prose includes useful context, detail, "
            + "punctuation, and readable words.", 8));
        HtmlArticleOptions articleOptions =
            new HtmlFactory().CreateArticleOptions();
        articleOptions.SetMinimumContentCharacters(10);
        articleOptions.SetMinimumCandidateCharacters(10);
        articleOptions.SetScoreThreshold(1);
        HtmlArticleResult article = api.ExtractArticle(
            "<article><h1>Title</h1><p>" + prose + "</p></article>",
            articleOptions, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sanitized, Is.EqualTo("<p>safe</p>"));
            Assert.That(configuredSanitized.IsSuccess(), Is.True);
            Assert.That(configuredSanitized.GetHtml(),
                Does.Contain("<p>safe</p>"));
            Assert.That(configuredSanitized.GetHtml(),
                Does.Not.Contain("script"));
            Assert.That(markdown, Is.EqualTo("# Title"));
            Assert.That(configuredMarkdown.GetMarkdown(),
                Is.EqualTo("Title\n==="));
            Assert.That(article.IsSuccess(), Is.True);
            Assert.That(article.GetArticle().GetTitle(),
                Is.EqualTo("Title"));
        });
    }

    [Test]
    public void HtmlApiProcessesEveryCharacterAndByteInputForm()
    {
        IHtmlApi api = new HtmlApi();
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.SetConvertText(true);
        string source = "<p>Hello <strong>pipeline</strong></p>";
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        MemoryStream stream = new MemoryStream(bytes);
        StringReader reader = new StringReader(source);

        HtmlProcessingResult fromString = api.ProcessString(source, options,
            CancellationToken.None);
        HtmlProcessingResult fromCharacters = api.ProcessCharacters(
            source.AsSpan(), options, CancellationToken.None);
        HtmlProcessingResult fromBytes = api.ProcessBytes(bytes, options,
            CancellationToken.None);
        HtmlProcessingResult fromByteSpan = api.ProcessByteSpan(
            bytes.AsSpan(), options, CancellationToken.None);
        HtmlProcessingResult fromStream = api.ProcessStream(stream, options,
            CancellationToken.None);
        HtmlProcessingResult fromReader = api.ProcessReader(reader, options,
            CancellationToken.None);

        HtmlProcessingResult[] results = [fromString, fromCharacters,
            fromBytes, fromByteSpan, fromStream, fromReader];
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.All.Matches<HtmlProcessingResult>(
                value => value.IsSuccess() && value.IsComplete()));
            Assert.That(results.Select(value => value.GetText()),
                Has.All.EqualTo("Hello pipeline"));
            Assert.That(fromCharacters.GetStageStatus(
                HtmlProcessingStage.Decode),
                Is.EqualTo(HtmlProcessingStageStatus.NotRequested));
            Assert.That(fromByteSpan.GetStageStatus(
                HtmlProcessingStage.Decode),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
        });
    }

    [Test]
    public void MetadataCandidatesExposeStableDocumentOrder()
    {
        IHtmlMetadataExtractorApi api = new HtmlMetadataExtractorApi();
        HtmlDocument document = Html.Parse(
            "<title>Document</title>"
            + "<meta property=og:title content='Open Graph'>"
            + "<meta name=twitter:title content='Twitter'>");

        HtmlMetadata metadata = api.Extract(document);
        HtmlMetadataCandidate[] titles = metadata.GetCandidatesForField(
            HtmlMetadataField.Title);
        HtmlMetadataCandidate documentTitle = titles.Single(value =>
            value.GetSource() == HtmlMetadataSource.Document);
        HtmlMetadataCandidate openGraphTitle = titles.Single(value =>
            value.GetSource() == HtmlMetadataSource.OpenGraph);
        HtmlMetadataCandidate twitterTitle = titles.Single(value =>
            value.GetSource() == HtmlMetadataSource.Twitter);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetTitle(), Is.EqualTo("Open Graph"));
            Assert.That(documentTitle.GetDocumentOrder(),
                Is.LessThan(openGraphTitle.GetDocumentOrder()));
            Assert.That(openGraphTitle.GetDocumentOrder(),
                Is.LessThan(twitterTitle.GetDocumentOrder()));
            Assert.That(titles.Select(value => value.GetDocumentOrder()),
                Is.All.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void PipelineResultExposesStageOutputsTimingsAndDroppedCounts()
    {
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.SetExtractMetadata(true);
        options.SetConvertText(true);
        options.SetConvertMarkdown(true);
        string source = "<title>Report</title>"
            + "<p onclick=unsafe>content</p>";

        HtmlProcessingResult result = new HtmlProcessorApi().ProcessString(
            source, options, CancellationToken.None);
        HtmlProcessingStageTiming[] timings = result.GetStageTimings();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSanitizeResult(), Is.Not.Null);
            Assert.That(result.GetSanitizeResult().IsSuccess(), Is.True);
            Assert.That(result.GetTextResult(), Is.Not.Null);
            Assert.That(result.GetTextResult().GetText(),
                Is.EqualTo(result.GetText()));
            Assert.That(result.GetMarkdownResult(), Is.Not.Null);
            Assert.That(result.GetMarkdownResult().GetMarkdown(),
                Is.EqualTo(result.GetMarkdown()));
            Assert.That(timings, Is.Not.Empty);
            Assert.That(timings.All(value => value.GetStage()
                != (HtmlProcessingStage)0), Is.True);
            Assert.That(timings.All(value => value.GetElapsed()
                == TimeSpan.FromTicks(value.GetElapsedTicks())), Is.True);
            Assert.That(result.GetStageDroppedDiagnosticCount(
                HtmlProcessingStage.Parse), Is.Zero);
            Assert.That(result.WereStageDiagnosticsTruncated(
                HtmlProcessingStage.Parse), Is.False);
            Assert.That(result.GetStageDiagnosticCount(
                HtmlProcessingStage.Parse), Is.EqualTo(
                    result.GetStageRetainedDiagnosticCount(
                        HtmlProcessingStage.Parse)
                    + result.GetStageDroppedDiagnosticCount(
                        HtmlProcessingStage.Parse)));
        });
    }
}

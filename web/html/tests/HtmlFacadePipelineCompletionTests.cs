using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlFacadePipelineCompletionTests
{
    [Test]
    public void FacadeProvidesTextAndConfiguredMetadataOperations()
    {
        HtmlApi api = new HtmlApi();
        HtmlTextOptions textOptions = new HtmlTextOptions();
        textOptions.SetLinkStyle(HtmlTextLinkStyle.TextOnly);
        HtmlMetadataOptions metadataOptions = new HtmlMetadataOptions();
        metadataOptions.SetIncludeSource(HtmlMetadataSource.Document, false);
        string input = "<html><head><title>Document title</title>"
            + "<meta property='og:title' content='Social title'>"
            + "</head><body><p>Hello <a href='https://example.test/'>"
            + "world</a></p></body></html>";

        string text = api.ToText(input);
        HtmlTextResult configuredText = api.ToTextWithOptions(input,
            textOptions, CancellationToken.None);
        HtmlMetadata metadata = api.ExtractMetadata(input,
            metadataOptions, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Hello world"));
            Assert.That(configuredText.IsSuccess(), Is.True);
            Assert.That(configuredText.GetText(), Does.Contain("Hello world"));
            Assert.That(configuredText.GetText(),
                Does.Not.Contain("https://example.test/"));
            Assert.That(metadata.GetTitle(), Is.EqualTo("Social title"));
            Assert.That(metadata.GetCandidate(
                HtmlMetadataField.Title).GetSource(),
                Is.EqualTo(HtmlMetadataSource.OpenGraph));
        });
    }

    [Test]
    public void PipelineAcceptsEveryCharacterAndByteInputForm()
    {
        HtmlProcessorApi api = new HtmlProcessorApi();
        HtmlProcessingOptions options = CreateTextOptions();
        string input = "<p>Hello <strong>pipeline</strong></p>";
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        MemoryStream stream = new MemoryStream(bytes);
        StringReader reader = new StringReader(input);

        HtmlProcessingResult fromString = api.ProcessString(input, options,
            CancellationToken.None);
        HtmlProcessingResult fromCharacters = api.ProcessCharacters(
            input.AsSpan(), options, CancellationToken.None);
        HtmlProcessingResult fromBytes = api.ProcessBytes(bytes, options,
            CancellationToken.None);
        HtmlProcessingResult fromByteSpan = api.ProcessByteSpan(
            bytes.AsSpan(), options, CancellationToken.None);
        HtmlProcessingResult fromStream = api.ProcessStream(stream, options,
            CancellationToken.None);
        HtmlProcessingResult fromReader = api.ProcessReader(reader, options,
            CancellationToken.None);
        HtmlProcessingResult[] results = new HtmlProcessingResult[]
        {
            fromString, fromCharacters, fromBytes, fromByteSpan,
            fromStream, fromReader
        };

        Assert.Multiple(() =>
        {
            Assert.That(results.All(value => value.IsSuccess()), Is.True);
            Assert.That(results.Select(value => value.GetText()),
                Is.All.EqualTo("Hello pipeline"));
            Assert.That(fromString.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(fromCharacters.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(fromReader.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(fromBytes.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.Succeeded));
            Assert.That(fromByteSpan.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.Succeeded));
            Assert.That(fromStream.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.Succeeded));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(reader.Peek(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void StreamAndReaderPipelineHonorLeaveOpen()
    {
        HtmlProcessorApi api = new HtmlProcessorApi();
        HtmlProcessingOptions options = CreateTextOptions();
        HtmlParseOptions parseOptions = options.GetParseOptions();
        parseOptions.SetLeaveOpen(false);
        options.SetParseOptions(parseOptions);
        MemoryStream stream = new MemoryStream(
            Encoding.UTF8.GetBytes("<p>stream</p>"));
        StringReader reader = new StringReader("<p>reader</p>");

        HtmlProcessingResult streamResult = api.ProcessStream(stream,
            options, CancellationToken.None);
        HtmlProcessingResult readerResult = api.ProcessReader(reader,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.GetText(), Is.EqualTo("stream"));
            Assert.That(readerResult.GetText(), Is.EqualTo("reader"));
            Assert.That(stream.CanRead, Is.False);
            Assert.Throws<ObjectDisposedException>(() => reader.Peek());
        });
    }

    [Test]
    public void DocumentFragmentAndNodePipelinesUseSuppliedTrees()
    {
        HtmlApi api = new HtmlApi();
        HtmlDocument document = api.ParseString(
            "<title>Tree title</title><main><p title='value'>Tree text</p>"
            + "</main>");
        HtmlParseResult parsedFragment = api.ParseFragment(
            "<em>Fragment text</em>", "div");
        HtmlNode fragment = parsedFragment.GetFragment();
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlProcessingOptions documentOptions = CreateTextOptions();
        documentOptions.SetExtractMetadata(true);
        HtmlProcessingOptions treeOptions = CreateTextOptions();

        HtmlProcessingResult documentResult = api.ProcessDocument(document,
            documentOptions, CancellationToken.None);
        HtmlProcessingResult fragmentResult = api.ProcessFragment(fragment,
            treeOptions, CancellationToken.None);
        HtmlProcessingResult nodeResult = api.ProcessNode(paragraph,
            treeOptions, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.GetMetadata().GetTitle(),
                Is.EqualTo("Tree title"));
            Assert.That(documentResult.GetText(), Is.EqualTo("Tree text"));
            Assert.That(fragmentResult.GetText(),
                Is.EqualTo("Fragment text"));
            Assert.That(nodeResult.GetText(), Is.EqualTo("Tree text"));
            Assert.That(documentResult.GetStageStatus(
                HtmlProcessingStage.Parse), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(fragmentResult.GetStageStatus(
                HtmlProcessingStage.Parse), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(nodeResult.GetStageStatus(
                HtmlProcessingStage.Decode), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(documentResult.GetStageStatus(
                HtmlProcessingStage.Metadata), Is.EqualTo(
                    HtmlProcessingStageStatus.Succeeded));
            Assert.That(fragmentResult.GetStageStatus(
                HtmlProcessingStage.Metadata), Is.EqualTo(
                    HtmlProcessingStageStatus.NotRequested));
            Assert.That(nodeResult.GetHtml(),
                Does.Contain("<p title=\"value\">Tree text</p>"));
        });
    }

    [Test]
    public void FragmentAndNodePipelinesRejectDocumentOnlyStages()
    {
        HtmlApi api = new HtmlApi();
        HtmlDocument document = api.ParseString("<p>text</p>");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode fragment = document.CreateFragment();
        fragment.Append(document.CreateElement("p"));
        HtmlProcessingOptions metadata = CreateTextOptions();
        metadata.SetExtractMetadata(true);
        HtmlProcessingOptions article = CreateTextOptions();
        article.SetExtractArticle(true);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => api.ProcessFragment(
                fragment, metadata, CancellationToken.None));
            Assert.Throws<ArgumentException>(() => api.ProcessNode(
                paragraph, article, CancellationToken.None));
        });
    }

    [Test]
    public void PipelineUsesConfiguredSerializerAndMetadataSnapshots()
    {
        HtmlApi api = new HtmlApi();
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.SetExtractMetadata(true);
        HtmlMetadataOptions metadataOptions = new HtmlMetadataOptions();
        metadataOptions.SetIncludeSource(HtmlMetadataSource.Document, false);
        HtmlSerializerOptions serializerOptions =
            new HtmlSerializerOptions();
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Single);
        serializerOptions.SetAsciiOnly(true);
        options.SetMetadataOptions(metadataOptions);
        options.SetSerializerOptions(serializerOptions);
        metadataOptions.SetIncludeSource(HtmlMetadataSource.Document, true);
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Double);
        serializerOptions.SetAsciiOnly(false);

        HtmlProcessingResult result = api.ProcessString(
            "<title>Document title</title>"
            + "<meta property='og:title' content='Social title'>"
            + "<p title='résumé'>café</p>", options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetMetadata().GetTitle(),
                Is.EqualTo("Social title"));
            Assert.That(result.GetHtml(),
                Does.Contain("title='r&eacute;sum&eacute;'"));
            Assert.That(result.GetHtml(), Does.Contain("caf&eacute;"));
        });
    }

    [Test]
    public void ExistingTreePipelineObservesCancellationBeforeStagesRun()
    {
        HtmlApi api = new HtmlApi();
        HtmlDocument document = api.ParseString("<p>text</p>");
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => api.ProcessDocument(
            document, CreateTextOptions(), source.Token));
    }

    static HtmlProcessingOptions CreateTextOptions()
    {
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.SetConvertText(true);
        return options;
    }
}

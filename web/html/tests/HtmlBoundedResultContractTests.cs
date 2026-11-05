namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlBoundedResultContractTests
{
    [Test]
    public void DecodingAndTokenizerFailuresExposeStableCodesAndCounts()
    {
        HtmlDecodedInput decoded = HtmlByteDecoder.Decode(
            new byte[] { 65 }, "not-an-encoding");
        HtmlTokenizer tokenizer = new HtmlTokenizer(
            new HtmlTokenizerOptions());
        tokenizer.SetReader(new InvalidCountContractReader(), true);
        HtmlTokenReadResult token = tokenizer.Read();

        Assert.Multiple(() =>
        {
            Assert.That(decoded.IsComplete(), Is.False);
            Assert.That(decoded.IsPartial(), Is.False);
            Assert.That(decoded.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.EncodingUnsupportedLabel));
            Assert.That(decoded.GetRetainedErrorCount(), Is.Zero);
            Assert.That(decoded.GetDroppedErrorCount(), Is.Zero);
            Assert.That(decoded.WereErrorsTruncated(), Is.False);
            Assert.That(token.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.InputReadFailed));
            Assert.That(token.GetRetainedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(token.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(token.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.InputReadFailed));
        });
    }

    [Test]
    public void CancelledParseReturnsCodedPartialState()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        HtmlParser parser = new HtmlParser(new HtmlParseOptions());
        parser.SetCancellationToken(cancellation.Token);
        cancellation.Cancel();

        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.IsPartial(), Is.True);
            Assert.That(result.WasCancelled(), Is.True);
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.OperationCancelled));
            Assert.That(result.GetRetainedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDroppedDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void ParseAndEntityResultsPreserveDroppedDiagnosticCounts()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(1);
        HtmlParseOptions parseOptions = new HtmlParseOptions();
        parseOptions.SetLimits(limits);
        HtmlParseResult parsed = Html.Parse(
            "<p a=1 a=2 a=3 a=4></p>", parseOptions,
            CancellationToken.None);
        HtmlEntityResult entities = HtmlEntities.DecodeText(
            "&bogus;&bogus;&bogus;", limits,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.GetRetainedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(parsed.GetDroppedDiagnosticCount(),
                Is.GreaterThan(0));
            Assert.That(parsed.GetDiagnosticCount(), Is.EqualTo(
                parsed.GetRetainedDiagnosticCount()
                + parsed.GetDroppedDiagnosticCount()));
            Assert.That(parsed.WereDiagnosticsTruncated(), Is.True);
            Assert.That(entities.GetRetainedDiagnosticCount(),
                Is.EqualTo(1));
            Assert.That(entities.GetDroppedDiagnosticCount(),
                Is.GreaterThan(0));
            Assert.That(entities.GetDiagnosticCount(), Is.EqualTo(
                entities.GetRetainedDiagnosticCount()
                + entities.GetDroppedDiagnosticCount()));
            Assert.That(entities.WereDiagnosticsTruncated(), Is.True);
        });
    }

    [Test]
    public void SelectorAndUrlFailuresExposePrimaryDiagnosticCodes()
    {
        HtmlSelectorCompileResult selector = HtmlSelectors.Compile("div[");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(8);
        HtmlUrlResult url = HtmlUrls.Normalize(
            "https://example.test/", limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(selector.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.SelectorInvalidSyntax));
            Assert.That(selector.IsPartial(), Is.False);
            Assert.That(selector.GetRetainedDiagnosticCount(),
                Is.EqualTo(selector.GetDiagnostics().Length));
            Assert.That(selector.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(url.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(url.IsPartial(), Is.False);
            Assert.That(url.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(url.WereDiagnosticsTruncated(), Is.False);
        });
    }

    [Test]
    public void SerializationLimitExceptionExposesStableCode()
    {
        HtmlNode paragraph = Html.Parse("<p>x</p>").QuerySelector("p");
        HtmlSerializerOptions options = HtmlSerializerOptions.CreateDefault();
        HtmlLimits limits = options.GetLimits();
        limits.SetMaximumOutputCharacters(3);
        options.SetLimits(limits);

        HtmlSerializationLimitException failure =
            Assert.Throws<HtmlSerializationLimitException>(() =>
                HtmlSerializer.Serialize(paragraph, options,
                    CancellationToken.None));

        Assert.That(failure.GetCode(), Is.EqualTo(
            HtmlDiagnosticCode.SerializationLimitExceeded));
    }

    [Test]
    public void SanitizerFailureIsFailClosedAndCoded()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(8);
        limits.SetMaximumDiagnostics(1);
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetLimits(limits);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(
            "<p a=1 a=2>long input</p>",
            HtmlSanitizerPolicy.CreateRichText().Compile(), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsPartial(), Is.False);
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.SanitizerLimitExceeded));
            Assert.That(result.GetReport().IsPartial(), Is.True);
            Assert.That(result.GetReport().GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.SanitizerLimitExceeded));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(
                result.GetRetainedDiagnosticCount()
                + result.GetDroppedDiagnosticCount()));
        });
    }

    [Test]
    public void ConversionResultsExposePartialStateAndLimitCodes()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(8);
        HtmlTextOptions textOptions = new HtmlTextOptions();
        textOptions.SetLimits(limits);
        HtmlMarkdownOptions markdownOptions =
            HtmlMarkdownOptions.CreateCommonMark();
        markdownOptions.SetLimits(limits);
        HtmlTextResult text = HtmlTextConverter.Convert(
            "<p>abcdefghijklmnop</p>", textOptions,
            CancellationToken.None);
        HtmlMarkdownResult markdown = HtmlMarkdownConverter.Convert(
            "<p>abcdefghijklmnop</p>", markdownOptions,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(text.IsPartial(), Is.True);
            Assert.That(text.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.TextConversionLimitExceeded));
            Assert.That(text.GetRetainedDiagnosticCount(), Is.EqualTo(
                text.GetDiagnostics().Length));
            Assert.That(markdown.IsPartial(), Is.True);
            Assert.That(markdown.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
            Assert.That(markdown.GetRetainedDiagnosticCount(), Is.EqualTo(
                markdown.GetDiagnostics().Length));
        });
    }

    [Test]
    public void ExtractionAndPipelineResultsExposeBoundedFailureState()
    {
        HtmlDocument document = Html.Parse("<p>short</p>");
        HtmlArticleResult article = HtmlArticleExtractor.Extract(document,
            new HtmlArticleOptions());
        HtmlMetadata metadata = new HtmlMetadata(
            Array.Empty<HtmlMetadataCandidate>(), 1, 1, 0, false,
            Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlProcessingOptions processingOptions =
            new HtmlProcessingOptions();
        HtmlLimits limits = HtmlLimits.CreateUntrusted();
        limits.SetMaximumInputCharacters(4);
        processingOptions.SetLimits(limits);
        HtmlProcessingResult processing = HtmlProcessor.Process(
            "<p>long input</p>", processingOptions,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(article.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ArticleExtractionFailed));
            Assert.That(article.GetRetainedDiagnosticCount(), Is.EqualTo(
                article.GetDiagnostics().Length));
            Assert.That(metadata.IsPartial(), Is.True);
            Assert.That(metadata.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(metadata.GetRetainedDiagnosticCount(), Is.Zero);
            Assert.That(processing.IsPartial(), Is.True);
            Assert.That(processing.GetFailureCode(), Is.Not.EqualTo(
                HtmlDiagnosticCode.None));
            Assert.That(processing.GetDiagnosticCount(), Is.EqualTo(
                processing.GetRetainedDiagnosticCount()
                + processing.GetDroppedDiagnosticCount()));
            Assert.That(processing.GetStageRetainedDiagnosticCount(
                HtmlProcessingStage.Parse), Is.EqualTo(
                processing.GetStageDiagnostics(
                    HtmlProcessingStage.Parse).Length));
        });
    }

    [Test]
    public void CompileResultReportsRetainedAndDroppedDiagnostics()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(1);
        options.SetLimits(limits);
        options.SetEnableTrustedRawHtml(true);
        options.Keep("div >> p");
        options.Remove("::before");

        HtmlMarkdownCompileResult result =
            options.CompileWithDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsPartial(), Is.False);
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownInvalidRule));
            Assert.That(result.GetRetainedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDroppedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        });
    }
}

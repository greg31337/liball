using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicResultCoverageCompletionTests
{
    [Test]
    public void RemainingPublicEntryPointsReturnConsistentResults()
    {
        HtmlDocument document = Html.Parse(
            "<main name=content data-kind=article>"
            + "<a href=next>link</a><img src=image.png></main>");
        HtmlNode root = document.GetRoot();
        HtmlNode main = document.QuerySelector("main");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlArticleOptions articleOptions = HtmlArticleOptions.CreateDefault();
        articleOptions.SetMinimumContentCharacters(1);
        IHtmlArticleExtractorApi articleApi = new HtmlArticleExtractorApi();
        bool readable = articleApi.IsProbablyReadable(document,
            articleOptions);
        HtmlProcessingOptions processing = new HtmlProcessingOptions();
        processing.SetExtractMetadata(false);
        processing.SetExtractArticle(false);
        processing.SetSanitize(false);
        processing.SetSerializeHtml(false);
        processing.SetConvertText(false);
        processing.SetConvertMarkdown(false);
        IHtmlProcessorApi processor = new HtmlProcessorApi();
        HtmlProcessingResult processedDocument = processor.ProcessDocument(
            document, processing, CancellationToken.None);
        HtmlNode fragment = Html.ParseFragment("<p>fragment</p>", "body")
            .GetFragment();
        HtmlProcessingResult processedFragment = processor.ProcessFragment(
            fragment, processing, CancellationToken.None);
        HtmlNode paragraph = Html.Parse("<p>node</p>").QuerySelector("p");
        HtmlProcessingResult processedNode = processor.ProcessNode(
            paragraph, processing, CancellationToken.None);
        HtmlSanitizerChange change = new HtmlSanitizerChange(
            HtmlSanitizerChangeKind.Removed,
            HtmlSanitizerChangeReason.AttributeNotAllowed, "onclick",
            "secret", HtmlSourceSpan.CreateUnknown());

        Assert.Multiple(() =>
        {
            Assert.That(readable, Is.False);
            Assert.That(document.FindByNameWithLimits("content", 10, 100),
                Has.Length.EqualTo(1));
            Assert.That(document.FindByAttributeWithLimits(
                "data-kind", 10, 100), Has.Length.EqualTo(1));
            Assert.That(document.FindByAttributeValueWithLimits(
                "data-kind", "article", 10, 100), Has.Length.EqualTo(1));
            Assert.That(document.FindFirstWithLimit(
                value => value.GetLocalName() == "main", 100),
                Is.SameAs(main));
            Assert.That(document.FindAllWithLimits(
                value => value.GetKind() == HtmlNodeKind.Element, 10, 100),
                Has.Length.EqualTo(6));
            Assert.That(document.FindMediaWithLimits(10, 100),
                Has.Length.EqualTo(1));
            Assert.That(root.FindLinksWithLimits(10, 100),
                Has.Length.EqualTo(1));
            Assert.That(root.FindMedia(), Has.Length.EqualTo(1));
            Assert.That(root.FindMediaWithLimits(10, 100),
                Has.Length.EqualTo(1));
            Assert.That(processedDocument.IsSuccess(), Is.True);
            Assert.That(processedDocument.GetDocument(),
                Is.SameAs(document));
            Assert.That(processedFragment.IsSuccess(), Is.True);
            Assert.That(processedFragment.GetParseResult().GetFragment()
                .GetTextContent(), Is.EqualTo("fragment"));
            Assert.That(processedNode.IsSuccess(), Is.True);
            Assert.That(processedNode.GetDocument().QuerySelector("p")
                .GetTextContent(), Is.EqualTo("node"));
            Assert.That(change.GetKind(),
                Is.EqualTo(HtmlSanitizerChangeKind.Removed));
            Assert.That(change.GetOriginalValue(), Is.EqualTo("[redacted]"));
            Assert.That(limits.GetMaximumVisits(), Is.GreaterThan(100));
        });
    }

    [Test]
    public void NamespaceBindingsAndIncrementalProgressExposeTheirValues()
    {
        HtmlSelectorNamespaceBinding binding =
            new HtmlSelectorNamespaceBinding("svg", "urn:svg");
        HtmlSelectorNamespaceBinding emptyNamespace =
            new HtmlSelectorNamespaceBinding("local", "");
        HtmlParser parser = new HtmlParser(HtmlParseOptions.CreateDefault());
        parser.Feed("<p>text</p>".AsSpan(), true);
        int totalProcessed = 0;
        HtmlParseProgress progress = parser.Read();
        while (progress.GetStatus() ==
            HtmlParseProgressStatus.TokenProcessed)
        {
            Assert.That(progress.GetProcessedTokenCount(),
                Is.GreaterThan(0));
            totalProcessed = totalProcessed
                + progress.GetProcessedTokenCount();
            progress = parser.Read();
        }

        Assert.Multiple(() =>
        {
            Assert.That(binding.Prefix, Is.EqualTo("svg"));
            Assert.That(binding.NamespaceUri, Is.EqualTo("urn:svg"));
            Assert.That(emptyNamespace.NamespaceUri, Is.Empty);
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSelectorNamespaceBinding(null, "urn:svg"));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSelectorNamespaceBinding("", "urn:svg"));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSelectorNamespaceBinding("svg", null));
            Assert.That(totalProcessed, Is.GreaterThan(0));
            Assert.That(progress.GetProcessedTokenCount(),
                Is.GreaterThanOrEqualTo(0));
            Assert.That(parser.Complete().IsSuccess(), Is.True);
        });
    }

    [Test]
    public void PublicResultsExposeCountsNullsAndDefensiveSnapshots()
    {
        IHtmlRefreshApi refreshApi = new HtmlRefreshApi();
        HtmlRefreshResult refresh = refreshApi.Parse("7; url=next.html");
        HtmlRefreshResult invalidRefresh = refreshApi.Parse("soon");
        IHtmlUrlsApi urlsApi = new HtmlUrlsApi();
        HtmlUrlResult absolute =
            urlsApi.Normalize("https://example.test/a/../b");
        HtmlUrlResult invalidUrl = urlsApi.Normalize("not-relative");
        HtmlDiagnostic originalUrlDiagnostic =
            invalidUrl.GetDiagnostics()[0];
        HtmlDiagnostic[] urlSnapshot = invalidUrl.GetDiagnostics();
        urlSnapshot[0] = null;

        IHtmlSelectorsApi selectorsApi = new HtmlSelectorsApi();
        HtmlSelectorCompileResult selector = selectorsApi.Compile("p");
        HtmlSelectorCompileResult invalidSelector =
            selectorsApi.Compile("div >> p");
        HtmlDiagnostic originalSelectorDiagnostic =
            invalidSelector.GetDiagnostics()[0];
        HtmlSelectorCompileError originalSelectorError =
            invalidSelector.GetErrors()[0];
        HtmlDiagnostic[] selectorDiagnostics =
            invalidSelector.GetDiagnostics();
        HtmlSelectorCompileError[] selectorErrors =
            invalidSelector.GetErrors();
        selectorDiagnostics[0] = null;
        selectorErrors[0] = null;

        HtmlProcessingOptions skippedOptions = new HtmlProcessingOptions();
        skippedOptions.SetExtractMetadata(false);
        skippedOptions.SetExtractArticle(false);
        skippedOptions.SetSanitize(false);
        skippedOptions.SetSerializeHtml(false);
        skippedOptions.SetConvertText(false);
        skippedOptions.SetConvertMarkdown(false);
        IHtmlProcessorApi processor = new HtmlProcessorApi();
        HtmlProcessingResult characters = processor.ProcessString(
            "<p>text</p>", skippedOptions, CancellationToken.None);
        HtmlProcessingOptions byteOptions = skippedOptions.Copy();
        HtmlParseOptions byteParseOptions = byteOptions.GetParseOptions();
        byteParseOptions.SetEncoding(Encoding.UTF8);
        byteOptions.SetParseOptions(byteParseOptions);
        HtmlProcessingResult bytes = processor.ProcessBytes(
            Encoding.UTF8.GetBytes("<p>text</p>"), byteOptions,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(refresh.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(refresh.GetRetainedDiagnosticCount(),
                Is.EqualTo(refresh.GetDiagnostics().Length));
            Assert.That(refresh.GetDiagnosticCount(), Is.EqualTo(
                refresh.GetRetainedDiagnosticCount()
                + refresh.GetDroppedDiagnosticCount()));
            Assert.That(refresh.WereDiagnosticsTruncated(), Is.False);
            Assert.That(invalidRefresh.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.MicrosyntaxInvalid));
            Assert.That(invalidRefresh.GetRetainedDiagnosticCount(),
                Is.EqualTo(1));
            Assert.That(invalidRefresh.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(invalidRefresh.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(invalidRefresh.WereDiagnosticsTruncated(), Is.False);
            Assert.That(absolute.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(absolute.WasCancelled(), Is.False);
            Assert.That(absolute.GetRetainedDiagnosticCount(), Is.Zero);
            Assert.That(absolute.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(absolute.WereDiagnosticsTruncated(), Is.False);
            Assert.That(invalidUrl.GetRetainedDiagnosticCount(),
                Is.EqualTo(invalidUrl.GetDiagnostics().Length));
            Assert.That(invalidUrl.GetDiagnosticCount(), Is.EqualTo(
                invalidUrl.GetRetainedDiagnosticCount()));
            Assert.That(invalidUrl.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(invalidUrl.WereDiagnosticsTruncated(), Is.False);
            Assert.That(invalidUrl.WasCancelled(), Is.False);
            Assert.That(invalidUrl.GetDiagnostics()[0],
                Is.SameAs(originalUrlDiagnostic));
            Assert.That(selector.GetDiagnosticCount(), Is.Zero);
            Assert.That(selector.WereDiagnosticsTruncated(), Is.False);
            Assert.That(selector.WasCancelled(), Is.False);
            Assert.That(invalidSelector.GetDiagnosticCount(),
                Is.EqualTo(invalidSelector.GetRetainedDiagnosticCount()));
            Assert.That(invalidSelector.GetDroppedDiagnosticCount(), Is.Zero);
            Assert.That(invalidSelector.WereDiagnosticsTruncated(), Is.False);
            Assert.That(invalidSelector.WasCancelled(), Is.False);
            Assert.That(invalidSelector.GetDiagnostics()[0],
                Is.SameAs(originalSelectorDiagnostic));
            Assert.That(invalidSelector.GetErrors()[0],
                Is.SameAs(originalSelectorError));
            Assert.That(characters.GetDetectedEncoding(), Is.Null);
            Assert.That(characters.GetEncodingDetection(), Is.Null);
            Assert.That(characters.GetMetadata(), Is.Null);
            Assert.That(characters.GetArticleResult(), Is.Null);
            Assert.That(characters.GetArticle(), Is.Null);
            Assert.That(characters.GetExtractedArticle(), Is.Null);
            Assert.That(characters.GetSanitizeResult(), Is.Null);
            Assert.That(characters.GetSanitizedNode(), Is.Null);
            Assert.That(characters.GetSanitizerReport(), Is.Null);
            Assert.That(characters.GetHtml(), Is.Null);
            Assert.That(characters.GetTextResult(), Is.Null);
            Assert.That(characters.GetText(), Is.Null);
            Assert.That(characters.GetMarkdownResult(), Is.Null);
            Assert.That(characters.GetMarkdown(), Is.Null);
            Assert.That(characters.WereDiagnosticsTruncated(), Is.False);
            Assert.That(bytes.GetDetectedEncoding(), Is.Not.Null);
            Assert.That(bytes.GetEncodingDetection(), Is.Not.Null);
        });
    }

    [Test]
    public void SerializationAndDecodingResultsEnforceTheirInvariants()
    {
        HtmlSerializationResult complete = new HtmlSerializationResult(
            HtmlSerializationStatus.Complete, true, "abc", 3);
        HtmlSerializationResult emptyLimit = new HtmlSerializationResult(
            HtmlSerializationStatus.LimitExceeded, false, null, 0);
        HtmlSerializationResult partialLimit = new HtmlSerializationResult(
            HtmlSerializationStatus.LimitExceeded, false, null, 2);

        Assert.Multiple(() =>
        {
            Assert.That(complete.IsComplete(), Is.True);
            Assert.That(complete.WasLimitExceeded(), Is.False);
            Assert.That(complete.IsPartial(), Is.False);
            Assert.That(complete.HasString(), Is.True);
            Assert.That(complete.GetString(), Is.EqualTo("abc"));
            Assert.That(complete.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(emptyLimit.IsPartial(), Is.False);
            Assert.That(emptyLimit.GetString(), Is.Null);
            Assert.That(partialLimit.IsPartial(), Is.True);
            Assert.That(partialLimit.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.SerializationLimitExceeded));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSerializationResult((HtmlSerializationStatus)(-1),
                    false, null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSerializationResult((HtmlSerializationStatus)99,
                    false, null, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSerializationResult(
                    HtmlSerializationStatus.Complete, true, null, 0));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSerializationResult(
                    HtmlSerializationStatus.Complete, false, "x", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSerializationResult(
                    HtmlSerializationStatus.Complete, false, null, -1));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSerializationResult(
                    HtmlSerializationStatus.Complete, true, "x", 2));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSerializationResult.CreateString(null, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlDecodingError(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlDecodingError(0, 0));
        });
    }

    [Test]
    public void MarkdownResultCoversEveryFailureAndCountState()
    {
        HtmlDiagnostic cancelledDiagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.OperationCancelled,
            HtmlDiagnosticSeverity.Error, "cancelled",
            HtmlSourceSpan.CreateUnknown());
        HtmlMarkdownResult success = new HtmlMarkdownResult(true, true,
            false, "ok", Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlMarkdownResult cancelled = new HtmlMarkdownResult(false, false,
            false, "", [cancelledDiagnostic], 1, 0);
        HtmlMarkdownResult truncated = new HtmlMarkdownResult(true, false,
            true, "cut", Array.Empty<HtmlDiagnostic>(), 1, 1);
        HtmlMarkdownResult failed = new HtmlMarkdownResult(false, false,
            false, "", Array.Empty<HtmlDiagnostic>(), 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(success.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(success.IsPartial(), Is.False);
            Assert.That(success.WereDiagnosticsTruncated(), Is.False);
            Assert.That(cancelled.WasCancelled(), Is.True);
            Assert.That(cancelled.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationCancelled));
            Assert.That(cancelled.IsPartial(), Is.False);
            Assert.That(truncated.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
            Assert.That(truncated.IsPartial(), Is.True);
            Assert.That(truncated.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(truncated.GetDroppedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(truncated.WereDiagnosticsTruncated(), Is.True);
            Assert.That(failed.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationFailed));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlMarkdownResult(false, false, false, null,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlMarkdownResult(false, false, false, "", null,
                    0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlMarkdownResult(false, false, false, "",
                    Array.Empty<HtmlDiagnostic>(), -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlMarkdownResult(false, false, false, "",
                    Array.Empty<HtmlDiagnostic>(), 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlMarkdownResult(false, false, false, "",
                    Array.Empty<HtmlDiagnostic>(), 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlMarkdownResult(false, false, false, "",
                    [cancelledDiagnostic], 2, 0));
            Assert.Throws<ArgumentException>(() =>
                new HtmlMarkdownResult(true, true, true, "",
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentException>(() =>
                new HtmlMarkdownResult(false, false, false, "", [null],
                    1, 0));
        });
    }

    [Test]
    public void SanitizerResultAndReportCoverFailureAndNullContracts()
    {
        HtmlSanitizerReport normal = new HtmlSanitizerReport(
            Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
            0, 0, false, false, false);
        HtmlSanitizerReport limited = new HtmlSanitizerReport(
            Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
            0, 0, false, true, false);
        HtmlSanitizerReport cancelled = new HtmlSanitizerReport(
            Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
            0, 0, false, false, true);
        HtmlDocument document = Html.Parse("<p>safe</p>");
        HtmlNode fragment = Html.ParseFragment("<p>safe</p>", "body")
            .GetFragment();
        HtmlNode subtree = document.QuerySelector("p");
        HtmlSanitizeResult documentResult = new HtmlSanitizeResult(true,
            false, document.GetRoot(), "<p>safe</p>", normal,
            Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult fragmentResult = new HtmlSanitizeResult(true,
            false, fragment, "<p>safe</p>", normal,
            Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult subtreeResult = new HtmlSanitizeResult(true,
            false, subtree, "<p>safe</p>", normal,
            Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult failed = new HtmlSanitizeResult(false, false,
            null, "", normal, Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult cancelledResult = new HtmlSanitizeResult(false,
            false, null, "", cancelled, Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult limitedResult = new HtmlSanitizeResult(false,
            false, null, "", limited, Array.Empty<HtmlDiagnostic>(), 0, 0);
        HtmlSanitizeResult dropped = new HtmlSanitizeResult(true, true,
            subtree, "safe", normal, Array.Empty<HtmlDiagnostic>(), 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(documentResult.GetDocument(), Is.SameAs(document));
            Assert.That(documentResult.GetFragment(), Is.Null);
            Assert.That(documentResult.GetSubtree(), Is.Null);
            Assert.That(fragmentResult.GetDocument(), Is.Null);
            Assert.That(fragmentResult.GetFragment(), Is.SameAs(fragment));
            Assert.That(fragmentResult.GetSubtree(), Is.Null);
            Assert.That(subtreeResult.GetDocument(), Is.Null);
            Assert.That(subtreeResult.GetFragment(), Is.Null);
            Assert.That(subtreeResult.GetSubtree(), Is.SameAs(subtree));
            Assert.That(failed.GetNode(), Is.Null);
            Assert.That(failed.GetDocument(), Is.Null);
            Assert.That(failed.GetFragment(), Is.Null);
            Assert.That(failed.GetSubtree(), Is.Null);
            Assert.That(failed.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationFailed));
            Assert.That(cancelledResult.WasCancelled(), Is.True);
            Assert.That(cancelledResult.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationCancelled));
            Assert.That(limitedResult.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.SanitizerLimitExceeded));
            Assert.That(dropped.IsPartial(), Is.True);
            Assert.That(dropped.WereDiagnosticsTruncated(), Is.True);
            Assert.That(normal.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
            Assert.That(normal.IsPartial(), Is.False);
            Assert.That(normal.GetReasonCount(
                HtmlSanitizerChangeReason.None), Is.Zero);
            Assert.That(normal.GetReasonCount(
                (HtmlSanitizerChangeReason)999999), Is.Zero);
            Assert.That(limited.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.SanitizerLimitExceeded));
            Assert.That(limited.IsPartial(), Is.True);
            Assert.That(cancelled.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.OperationCancelled));
            Assert.That(cancelled.IsPartial(), Is.True);
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizerReport(null, Array.Empty<long>(), 0, 0,
                    false, false, false));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizerReport(
                    Array.Empty<HtmlSanitizerChange>(), null, 0, 0,
                    false, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizerReport(
                    Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
                    -1, 0, false, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizerReport(
                    Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
                    0, -1, false, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizerReport(
                    Array.Empty<HtmlSanitizerChange>(), Array.Empty<long>(),
                    0, 1, false, false, false));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSanitizerReport([null], Array.Empty<long>(),
                    1, 0, false, false, false));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSanitizerReport(
                    Array.Empty<HtmlSanitizerChange>(), [-1],
                    0, 0, false, false, false));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizeResult(false, false, null, null, normal,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizeResult(false, false, null, "", null,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizeResult(false, false, null, "", normal,
                    null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizeResult(false, false, null, "", normal,
                    Array.Empty<HtmlDiagnostic>(), -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizeResult(false, false, null, "", normal,
                    Array.Empty<HtmlDiagnostic>(), 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlSanitizeResult(false, false, null, "", normal,
                    Array.Empty<HtmlDiagnostic>(), 0, 1));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSanitizeResult(true, false, null, "", normal,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSanitizeResult(false, false, subtree, "", normal,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentException>(() =>
                new HtmlSanitizeResult(false, false, null, "unsafe", normal,
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
        });
    }

    [Test]
    public void UrlResultRejectsInvalidDiagnosticCollections()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new HtmlUrlResult(
                "", "", false, false, false, false, null));
            Assert.Throws<ArgumentException>(() => new HtmlUrlResult(
                "", "", false, false, false, false, [null]));
        });
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Coordinates the configured HTML processing stages and combines their outputs and diagnostics.</summary>
internal static class HtmlProcessor
{
    /// <summary>Runs the configured parsing, extraction, sanitization, serialization, text, and Markdown stages.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The requested pipeline outputs, per-stage status, timing, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlProcessingResult Process(string html,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, false, true);
        return run.Process(html);
    }

    internal static HtmlProcessingResult ProcessCharacters(
        ReadOnlySpan<char> html, IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, false, true);
        return run.ProcessCharacters(html);
    }

    /// <summary>Runs the configured parsing, extraction, sanitization, serialization, text, and Markdown stages.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The requested pipeline outputs, per-stage status, timing, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlProcessingResult Process(byte[] html,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, true, true);
        return run.ProcessBytes(html);
    }

    internal static HtmlProcessingResult ProcessByteSpan(
        ReadOnlySpan<byte> html, IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, true, true);
        return run.ProcessBytes(html.ToArray());
    }

    internal static HtmlProcessingResult ProcessStream(Stream stream,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlCompiledProcessingOptions compiled = options.Compile();
        HtmlProcessingRun run = new HtmlProcessingRun(compiled,
            cancellationToken, true, true);
        return run.ProcessStream(stream,
            compiled.GetParseOptions().GetLeaveOpen());
    }

    internal static HtmlProcessingResult ProcessReader(TextReader reader,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, false, true);
        return run.ProcessReader(reader);
    }

    internal static HtmlProcessingResult ProcessDocument(
        HtmlDocument document, IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlProcessingRun run = new HtmlProcessingRun(options.Compile(),
            cancellationToken, false, false);
        return run.ProcessExisting(document.GetRoot());
    }

    internal static HtmlProcessingResult ProcessFragment(HtmlNode fragment,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        HtmlSanitizerApi.ValidateFragment(fragment);
        HtmlCompiledProcessingOptions compiled =
            ValidateNodeOptions(options);
        HtmlProcessingRun run = new HtmlProcessingRun(compiled,
            cancellationToken, false, false);
        return run.ProcessExisting(fragment);
    }

    internal static HtmlProcessingResult ProcessNode(HtmlNode node,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        HtmlSanitizerApi.ValidateSubtree(node);
        HtmlCompiledProcessingOptions compiled =
            ValidateNodeOptions(options);
        HtmlProcessingRun run = new HtmlProcessingRun(compiled,
            cancellationToken, false, false);
        return run.ProcessExisting(node);
    }

    internal static HtmlCompiledProcessingOptions ValidateNodeOptions(
        IHtmlProcessingConfiguration options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlCompiledProcessingOptions compiled = options.Compile();
        if (compiled.GetExtractMetadata() || compiled.GetExtractArticle())
        {
            throw new ArgumentException(
                "Fragment and subtree pipelines cannot run document metadata or article extraction stages.",
                "options");
        }

        return compiled;
    }
}

internal class HtmlPipelineDecodeResult
{
    internal HtmlDecodedInput decoded;
    internal HtmlEncodingDetectionResult detection;
    internal bool limitExceeded;

    internal HtmlPipelineDecodeResult(HtmlDecodedInput decodedValue,
        HtmlEncodingDetectionResult detectionValue, bool limitExceededValue)
    {
        this.decoded = decodedValue;
        this.detection = detectionValue;
        this.limitExceeded = limitExceededValue;
    }

    internal HtmlDecodedInput GetDecodedInput()
    {
        return this.decoded;
    }

    internal HtmlEncodingDetectionResult GetDetection()
    {
        return this.detection;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }
}

internal class HtmlProcessingRun
{
    internal const int StageCount = 8;
    internal const int DecoderBufferCharacters = 4096;

    internal HtmlCompiledProcessingOptions options;
    internal CancellationToken cancellationToken;
    internal HtmlBudget budget;
    internal HtmlProcessingStageStatus[] statuses;
    internal long[] elapsedTicks;
    internal HtmlParseResult? parseResult;
    internal HtmlMetadata? metadata;
    internal HtmlArticleResult? articleResult;
    internal HtmlArticle? article;
    internal HtmlSanitizeResult? sanitizeResult;
    internal string? html;
    internal HtmlTextResult? textResult;
    internal HtmlMarkdownResult? markdownResult;
    internal HtmlDiagnostic[][] stageDiagnostics;
    internal long[] stageDiagnosticCounts;
    internal long[] stageDroppedDiagnosticCounts;
    internal bool stopped;
    internal HtmlNode? suppliedNode;

    internal HtmlProcessingRun(HtmlCompiledProcessingOptions optionsValue,
        CancellationToken cancellationTokenValue, bool decodeRequested,
        bool parseRequested)
    {
        this.options = optionsValue;
        this.cancellationToken = cancellationTokenValue;
        this.budget = new HtmlBudget(optionsValue.GetLimits(),
            cancellationTokenValue);
        this.statuses = new HtmlProcessingStageStatus[StageCount];
        this.elapsedTicks = new long[StageCount];
        this.parseResult = null;
        this.metadata = null;
        this.articleResult = null;
        this.article = null;
        this.sanitizeResult = null;
        this.html = null;
        this.textResult = null;
        this.markdownResult = null;
        this.stageDiagnostics = new HtmlDiagnostic[StageCount][];
        this.stageDiagnosticCounts = new long[StageCount];
        this.stageDroppedDiagnosticCounts = new long[StageCount];
        this.stopped = false;
        this.suppliedNode = null;
        this.InitializeStageDiagnostics();
        this.InitializeStatuses(decodeRequested, parseRequested);
    }

    internal HtmlProcessingResult Process(string input)
    {
        return this.ProcessCharacters(input.AsSpan());
    }

    internal HtmlProcessingResult ProcessCharacters(ReadOnlySpan<char> input)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        long started = Stopwatch.GetTimestamp();
        HtmlParser parser = HtmlParser.Create(this.options.GetParseOptions(),
            this.cancellationToken, this.budget);
        parser.Feed(input, true);
        this.parseResult = parser.Complete();
        this.SetStageDiagnostics(HtmlProcessingStage.Parse,
            this.parseResult.GetDiagnostics(),
            this.parseResult.GetDiagnosticCount(),
            this.parseResult.GetDroppedDiagnosticCount());
        this.FinishParse(started);
        this.ProcessRemaining();
        return this.CreateResult();
    }

    internal HtmlProcessingResult ProcessBytes(byte[] input)
    {
        return this.ProcessStream(new MemoryStream(input, false), false);
    }

    internal HtmlProcessingResult ProcessStream(Stream input,
        bool leaveOpen)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlParseOptions parseOptions = this.options.GetParseOptions();
        HtmlDiagnosticCollector diagnostics =
            new HtmlDiagnosticCollector(this.budget);
        HtmlStreamDecoder decoder;
        long decodeStarted = Stopwatch.GetTimestamp();
        if (parseOptions.HasEncoding())
        {
            decoder = HtmlStreamDecoder.CreateWithEncoding(input,
                parseOptions.GetEncoding(), leaveOpen, this.budget,
                diagnostics);
        }
        else
        {
            decoder = HtmlStreamDecoder.Create(input, leaveOpen,
                DecoderBufferCharacters,
                parseOptions.GetTransportEncodingLabel(),
                "windows-1252", this.budget, diagnostics);
        }

        System.Threading.Tasks.Task<HtmlPipelineDecodeResult>? decodeTask =
            System.Threading.Tasks.Task.Run(() =>
                new HtmlPipelineDecodeResult(decoder.Load(),
                    decoder.GetDetection(), decoder.WasLimitExceeded()));
        while (!decodeTask.IsCompleted)
        {
            Thread.Yield();
        }

        decoder.Close();
        if (!decodeTask.IsCompletedSuccessfully)
        {
            AggregateException? aggregate = decodeTask.Exception;
            if (aggregate == null)
            {
                throw new InvalidOperationException(
                    "A failed decoding task must contain an exception.");
            }

            throw aggregate.GetBaseException();
        }

        HtmlPipelineDecodeResult decodeResult = decodeTask.Result;
        HtmlDecodedInput decoded = decodeResult.GetDecodedInput();
        HtmlEncodingDetectionResult detection = decodeResult.GetDetection();
        bool decodeLimitExceeded = decodeResult.WasLimitExceeded();

        HtmlProcessingStageStatus decodeStatus =
            HtmlProcessingStageStatus.Succeeded;
        if (!decoded.IsSupported())
        {
            decodeStatus = HtmlProcessingStageStatus.Failed;
        }
        else if (decodeLimitExceeded)
        {
            decodeStatus = HtmlProcessingStageStatus.Incomplete;
        }

        this.SetTiming(HtmlProcessingStage.Decode, decodeStatus,
            decodeStarted);
        this.SetStageDiagnostics(HtmlProcessingStage.Decode,
            diagnostics.GetDiagnostics(), diagnostics.GetTotalCount(),
            diagnostics.GetDroppedCount());

        long parseStarted = Stopwatch.GetTimestamp();
        HtmlParser parser = HtmlParser.Create(parseOptions,
            this.cancellationToken, this.budget, true);
        parser.AddInputDiagnostics(diagnostics.GetDiagnostics());
        if (diagnostics.WasTruncated())
        {
            parser.SetInputDiagnosticsTruncated(
                diagnostics.GetDroppedCount());
        }

        string label = detection.GetEncodingName();
        Encoding? encoding = Html.ResolveEncoding(label, parseOptions);
        parser.SetEncodingMetadata(encoding, label, detection);
        parser.FeedDecoded(decoded, true);
        if (decodeLimitExceeded)
        {
            parser.SetInputLimitExceeded();
        }

        if (!decoded.IsSupported())
        {
            parser.SetInputFailed();
        }

        this.parseResult = parser.Complete();
        this.SetStageDiagnostics(HtmlProcessingStage.Parse,
            this.parseResult.GetDiagnostics(),
            this.parseResult.GetDiagnosticCount(),
            this.parseResult.GetDroppedDiagnosticCount());
        this.FinishParse(parseStarted);
        this.ProcessRemaining();
        return this.CreateResult();
    }

    internal HtmlProcessingResult ProcessReader(TextReader reader)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        long started = Stopwatch.GetTimestamp();
        HtmlParseOptions parseOptions = this.options.GetParseOptions();
        HtmlParser parser = HtmlParser.Create(parseOptions,
            this.cancellationToken, this.budget);
        this.parseResult = Html.ParseReaderWithParser(reader, parseOptions,
            this.cancellationToken, parser);
        this.SetStageDiagnostics(HtmlProcessingStage.Parse,
            this.parseResult.GetDiagnostics(),
            this.parseResult.GetDiagnosticCount(),
            this.parseResult.GetDroppedDiagnosticCount());
        this.FinishParse(started);
        this.ProcessRemaining();
        return this.CreateResult();
    }

    internal HtmlProcessingResult ProcessExisting(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlDocument document = node.GetOwnerDocumentStorage();
        HtmlNode? fragment = node.GetKindStorage() == HtmlNodeKind.Fragment
            ? node : null;
        this.parseResult = new HtmlParseResult(true, true, false, false,
            document, fragment, new HtmlDiagnostic[0], 0, 0, null, "",
            null);
        this.suppliedNode = node.GetKindStorage() == HtmlNodeKind.Document
            ? CreateDocumentContent(document) : node;
        this.ProcessRemaining();
        return this.CreateResult();
    }

    internal void ProcessRemaining()
    {
        if (this.stopped)
        {
            return;
        }

        HtmlParseResult? parseResult = this.parseResult;
        if (parseResult == null)
        {
            throw new InvalidOperationException(
                "Remaining pipeline stages require a parse result.");
        }

        HtmlDocument document = parseResult.GetDocument();
        if (this.IsRequested(HtmlProcessingStage.Metadata))
        {
            long started = Stopwatch.GetTimestamp();
            this.metadata = HtmlMetadataExtractor.ExtractCore(document,
                this.options.GetMetadataOptions(), this.cancellationToken,
                this.budget);
            HtmlProcessingStageStatus status =
                this.metadata.IsPartial()
                    || this.budget.HasExceededLimit()
                ? HtmlProcessingStageStatus.Incomplete
                : HtmlProcessingStageStatus.Succeeded;
            this.SetTiming(HtmlProcessingStage.Metadata, status, started);
            this.SetStageDiagnostics(HtmlProcessingStage.Metadata,
                this.metadata.GetDiagnostics(),
                this.metadata.GetDiagnosticCount(),
                this.metadata.GetDroppedDiagnosticCount());
            this.StopIfIncomplete(status);
        }

        if (!this.stopped
            && this.IsRequested(HtmlProcessingStage.Article))
        {
            long started = Stopwatch.GetTimestamp();
            this.articleResult = HtmlArticleExtractor.ExtractParsed(
                document, this.options.GetArticleOptions(),
                this.cancellationToken, parseResult.IsSuccess(),
                parseResult.IsComplete(),
                parseResult.WasTruncated(), this.metadata,
                this.budget);
            HtmlProcessingStageStatus status =
                GetArticleStatus(this.articleResult);
            this.SetTiming(HtmlProcessingStage.Article, status, started);
            this.SetStageDiagnostics(HtmlProcessingStage.Article,
                this.articleResult.GetDiagnostics(),
                this.articleResult.GetDiagnosticCount(),
                this.articleResult.GetDroppedDiagnosticCount());
            this.StopIfIncomplete(status);
        }

        HtmlNode? workingNode = this.suppliedNode == null
            ? CreateDocumentContent(document) : this.suppliedNode;
        if (!this.stopped && this.articleResult != null
            && this.articleResult.IsSuccess())
        {
            HtmlArticle? extractedArticle =
                this.articleResult.GetArticle();
            if (extractedArticle == null)
            {
                throw new InvalidOperationException(
                    "Successful article extraction must produce an article.");
            }

            workingNode = extractedArticle.GetContent();
        }

        if (!this.stopped
            && this.IsRequested(HtmlProcessingStage.Sanitize))
        {
            long started = Stopwatch.GetTimestamp();
            this.sanitizeResult = HtmlSanitizer.SanitizeForPipeline(
                workingNode, this.options.GetSanitizerPolicy(),
                this.options.GetLimits(), this.cancellationToken,
                this.budget);
            HtmlProcessingStageStatus status =
                GetSanitizeStatus(this.sanitizeResult);
            this.SetTiming(HtmlProcessingStage.Sanitize, status, started);
            this.SetStageDiagnostics(HtmlProcessingStage.Sanitize,
                this.sanitizeResult.GetDiagnostics(),
                this.sanitizeResult.GetDiagnostics().Length, 0);
            this.StopIfIncomplete(status);
            if (!this.stopped)
            {
                workingNode = this.sanitizeResult.GetNode();
            }
        }

        if (!this.stopped
            && this.IsRequested(HtmlProcessingStage.Serialize))
        {
            long started = Stopwatch.GetTimestamp();
            if (workingNode == null)
            {
                throw new InvalidOperationException(
                    "A requested serialization stage requires a node.");
            }

            HtmlSerializerOptions serializer =
                this.options.GetSerializerOptions();
            this.html = HtmlSerializer.Serialize(workingNode,
                serializer, this.cancellationToken, this.budget);

            HtmlProcessingStageStatus status =
                this.budget.HasExceededLimit()
                ? HtmlProcessingStageStatus.Incomplete
                : HtmlProcessingStageStatus.Succeeded;
            if (status != HtmlProcessingStageStatus.Succeeded)
            {
                this.html = null;
                HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
                    HtmlDiagnosticCode.SerializationLimitExceeded,
                    HtmlDiagnosticSeverity.Warning,
                    "HTML serialization exceeded the shared output limit.",
                    HtmlSourceSpan.CreateUnknown());
                this.SetStageDiagnostics(HtmlProcessingStage.Serialize,
                    new HtmlDiagnostic[] { diagnostic }, 1, 0);
            }

            this.SetTiming(HtmlProcessingStage.Serialize, status, started);
            this.StopIfIncomplete(status);
        }

        if (!this.stopped
            && this.IsRequested(HtmlProcessingStage.Text))
        {
            long started = Stopwatch.GetTimestamp();
            this.textResult = HtmlTextConverter.Convert(workingNode,
                this.options.GetTextOptions(), this.cancellationToken,
                this.budget);
            HtmlProcessingStageStatus status = GetTextStatus(
                this.textResult);
            this.SetTiming(HtmlProcessingStage.Text, status, started);
            this.SetStageDiagnostics(HtmlProcessingStage.Text,
                this.textResult.GetDiagnostics(),
                this.textResult.GetDiagnosticCount(),
                this.textResult.GetDroppedDiagnosticCount());
            this.StopIfIncomplete(status);
        }

        if (!this.stopped
            && this.IsRequested(HtmlProcessingStage.Markdown))
        {
            long started = Stopwatch.GetTimestamp();
            HtmlCompiledMarkdownOptions? markdownOptions =
                this.options.GetMarkdownOptions();
            if (markdownOptions == null)
            {
                throw new InvalidOperationException(
                    "A requested Markdown stage requires compiled options.");
            }

            this.markdownResult = HtmlMarkdownConverter.Convert(workingNode,
                markdownOptions, this.cancellationToken, this.budget);
            HtmlProcessingStageStatus status = GetMarkdownStatus(
                this.markdownResult);
            this.SetTiming(HtmlProcessingStage.Markdown, status, started);
            this.SetStageDiagnostics(HtmlProcessingStage.Markdown,
                this.markdownResult.GetDiagnostics(),
                this.markdownResult.GetDiagnosticCount(),
                this.markdownResult.GetDroppedDiagnosticCount());
            this.StopIfIncomplete(status);
        }

        if (this.sanitizeResult != null && this.html != null)
        {
            this.sanitizeResult = new HtmlSanitizeResult(true,
                this.sanitizeResult.WasTruncated(),
                this.sanitizeResult.GetNode(), this.html,
                this.sanitizeResult.GetReport(),
                this.sanitizeResult.GetDiagnostics(),
                this.sanitizeResult.GetDiagnosticCount(),
                this.sanitizeResult.GetDroppedDiagnosticCount());
        }

        this.CreateProcessedArticle();
    }

    internal void FinishParse(long started)
    {
        HtmlParseResult? parseResult = this.parseResult;
        if (parseResult == null)
        {
            throw new InvalidOperationException(
                "Finishing parsing requires a parse result.");
        }

        HtmlProcessingStageStatus status = GetParseStatus(parseResult);
        this.SetTiming(HtmlProcessingStage.Parse, status, started);
        this.StopIfIncomplete(status);
    }

    internal void StopIfIncomplete(HtmlProcessingStageStatus status)
    {
        if (status != HtmlProcessingStageStatus.Succeeded)
        {
            this.stopped = true;
        }
    }

    internal HtmlProcessingResult CreateResult()
    {
        HtmlProcessingStageTiming[] timings =
            new HtmlProcessingStageTiming[StageCount];
        bool success = true;
        HtmlParseResult? parseResult = this.parseResult;
        if (parseResult == null)
        {
            throw new InvalidOperationException(
                "Creating a pipeline result requires a parse result.");
        }

        bool truncated = parseResult.WasTruncated();
        int index = 0;
        while (index < StageCount)
        {
            HtmlProcessingStage stage =
                (HtmlProcessingStage)(index + 1);
            HtmlProcessingStageStatus status = this.statuses[index];
            timings[index] = new HtmlProcessingStageTiming(stage, status,
                this.elapsedTicks[index]);
            if (status == HtmlProcessingStageStatus.Failed
                || status == HtmlProcessingStageStatus.Incomplete
                || status == HtmlProcessingStageStatus.Skipped)
            {
                success = false;
            }

            if (status == HtmlProcessingStageStatus.Incomplete)
            {
                truncated = true;
            }

            index = index + 1;
        }

        if (this.articleResult != null && this.articleResult.WasTruncated()
            || this.sanitizeResult != null
                && this.sanitizeResult.WasTruncated()
            || this.textResult != null && this.textResult.WasTruncated()
            || this.markdownResult != null
                && this.markdownResult.WasTruncated()
            || this.budget.HasExceededLimit())
        {
            truncated = true;
        }

        HtmlDiagnostic[] diagnostics = this.CreateDiagnostics();
        return new HtmlProcessingResult(success, success && !truncated,
            truncated, parseResult, this.metadata,
            this.articleResult, this.article, this.sanitizeResult,
            this.html, this.textResult, this.markdownResult, timings,
            this.stageDiagnostics, this.stageDiagnosticCounts,
            this.stageDroppedDiagnosticCounts, diagnostics,
            this.options.GetLimits().GetMaximumDiagnostics());
    }

    internal HtmlDiagnostic[] CreateDiagnostics()
    {
        if (!this.budget.HasExceededLimit())
        {
            return new HtmlDiagnostic[0];
        }

        return new HtmlDiagnostic[]
        {
            HtmlDiagnostic.Create(HtmlDiagnosticCode.PipelineLimitExceeded,
                HtmlDiagnosticSeverity.Warning,
                "The shared processing resource budget was exceeded.",
                HtmlSourceSpan.CreateUnknown())
        };
    }

    internal void InitializeStatuses(bool decodeRequested,
        bool parseRequested)
    {
        int index = 0;
        while (index < StageCount)
        {
            this.statuses[index] = HtmlProcessingStageStatus.NotRequested;
            index = index + 1;
        }

        if (decodeRequested)
        {
            this.SetInitialRequested(HtmlProcessingStage.Decode);
        }

        if (parseRequested)
        {
            this.SetInitialRequested(HtmlProcessingStage.Parse);
        }
        if (this.options.GetExtractMetadata()
            || this.options.GetExtractArticle())
        {
            this.SetInitialRequested(HtmlProcessingStage.Metadata);
        }

        if (this.options.GetExtractArticle())
        {
            this.SetInitialRequested(HtmlProcessingStage.Article);
        }

        if (this.options.GetSanitize())
        {
            this.SetInitialRequested(HtmlProcessingStage.Sanitize);
        }

        if (this.options.GetSerializeHtml())
        {
            this.SetInitialRequested(HtmlProcessingStage.Serialize);
        }

        if (this.options.GetConvertText())
        {
            this.SetInitialRequested(HtmlProcessingStage.Text);
        }

        if (this.options.GetConvertMarkdown())
        {
            this.SetInitialRequested(HtmlProcessingStage.Markdown);
        }
    }

    internal void InitializeStageDiagnostics()
    {
        int index = 0;
        while (index < this.stageDiagnostics.Length)
        {
            this.stageDiagnostics[index] = new HtmlDiagnostic[0];
            index = index + 1;
        }
    }

    internal void SetStageDiagnostics(HtmlProcessingStage stage,
        HtmlDiagnostic[] values, long totalCount, long droppedCount)
    {
        int index = (int)stage - 1;
        this.stageDiagnostics[index] = values;
        this.stageDiagnosticCounts[index] = totalCount;
        this.stageDroppedDiagnosticCounts[index] = droppedCount;
    }

    internal void CreateProcessedArticle()
    {
        if (this.articleResult == null || !this.articleResult.IsSuccess()
            || this.sanitizeResult == null
            || !this.sanitizeResult.IsSuccess())
        {
            this.article = null;
            return;
        }

        HtmlArticle? extracted =
            this.articleResult.GetUnsanitizedArticle();
        HtmlNode? sanitizedNode = this.sanitizeResult.GetNode();
        if (extracted == null || sanitizedNode == null)
        {
            throw new InvalidOperationException(
                "Successful article and sanitizer stages must contain output.");
        }

        string? text = this.textResult == null
            ? null : this.textResult.GetText();
        string? markdown = this.markdownResult == null
            ? null : this.markdownResult.GetMarkdown();
        this.article = extracted.CreateProcessed(
            sanitizedNode, this.html, text, markdown);
    }

    internal void SetInitialRequested(HtmlProcessingStage stage)
    {
        this.statuses[(int)stage - 1] =
            HtmlProcessingStageStatus.Skipped;
    }

    internal bool IsRequested(HtmlProcessingStage stage)
    {
        return this.statuses[(int)stage - 1]
            != HtmlProcessingStageStatus.NotRequested;
    }

    internal void SetTiming(HtmlProcessingStage stage,
        HtmlProcessingStageStatus status, long started)
    {
        int index = (int)stage - 1;
        this.statuses[index] = status;
        this.elapsedTicks[index] =
            Stopwatch.GetElapsedTime(started).Ticks;
    }

    internal static HtmlProcessingStageStatus GetParseStatus(HtmlParseResult result)
    {
        if (result.IsSuccess() && result.IsComplete()
            && !result.WasTruncated())
        {
            return HtmlProcessingStageStatus.Succeeded;
        }

        return result.IsComplete()
            ? HtmlProcessingStageStatus.Failed
            : HtmlProcessingStageStatus.Incomplete;
    }

    internal static HtmlProcessingStageStatus GetArticleStatus(
        HtmlArticleResult result)
    {
        if (result.IsSuccess() && result.IsComplete()
            && !result.WasTruncated())
        {
            return HtmlProcessingStageStatus.Succeeded;
        }

        return result.IsComplete()
            ? HtmlProcessingStageStatus.Failed
            : HtmlProcessingStageStatus.Incomplete;
    }

    internal static HtmlProcessingStageStatus GetSanitizeStatus(
        HtmlSanitizeResult result)
    {
        if (result.IsSuccess() && !result.WasTruncated())
        {
            return HtmlProcessingStageStatus.Succeeded;
        }

        return result.WasTruncated()
            ? HtmlProcessingStageStatus.Incomplete
            : HtmlProcessingStageStatus.Failed;
    }

    internal static HtmlProcessingStageStatus GetTextStatus(HtmlTextResult result)
    {
        if (result.IsSuccess() && result.IsComplete()
            && !result.WasTruncated())
        {
            return HtmlProcessingStageStatus.Succeeded;
        }

        return result.IsComplete()
            ? HtmlProcessingStageStatus.Failed
            : HtmlProcessingStageStatus.Incomplete;
    }

    internal static HtmlProcessingStageStatus GetMarkdownStatus(
        HtmlMarkdownResult result)
    {
        if (result.IsSuccess() && result.IsComplete()
            && !result.WasTruncated())
        {
            return HtmlProcessingStageStatus.Succeeded;
        }

        return result.IsComplete()
            ? HtmlProcessingStageStatus.Failed
            : HtmlProcessingStageStatus.Incomplete;
    }

    internal static HtmlNode CreateDocumentContent(HtmlDocument document)
    {
        HtmlNode? body = document.GetBody();
        if (body == null)
        {
            return document.GetRoot();
        }

        HtmlNode fragment = document.CreateFragment();
        HtmlNode[] children = body.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            fragment.Append(children[index].Clone(true));
            index = index + 1;
        }

        return fragment;
    }
}

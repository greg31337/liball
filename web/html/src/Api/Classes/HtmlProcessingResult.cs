using System;
using System.Text;

namespace html;
/// <summary>Combines requested pipeline outputs, per-stage status, timing, and diagnostics.</summary>
public class HtmlProcessingResult : IHtmlProcessingResult
{
    internal bool success;
    internal bool complete;
    internal bool truncated;
    internal HtmlParseResult parseResult;
    internal HtmlMetadata? metadata;
    internal HtmlArticleResult? articleResult;
    internal HtmlArticle? article;
    internal HtmlSanitizeResult? sanitizeResult;
    internal string? html;
    internal HtmlTextResult? textResult;
    internal HtmlMarkdownResult? markdownResult;
    internal HtmlProcessingStageTiming[] timings;
    internal HtmlDiagnostic[][] stageDiagnostics;
    internal long[] stageDiagnosticCounts;
    internal long[] stageDroppedDiagnosticCounts;
    internal HtmlDiagnostic[] diagnostics = Array.Empty<HtmlDiagnostic>();
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlProcessingResult(bool successValue, bool completeValue, bool truncatedValue, HtmlParseResult parseResultValue, HtmlMetadata? metadataValue, HtmlArticleResult? articleResultValue, HtmlArticle? articleValue, HtmlSanitizeResult? sanitizeResultValue, string? htmlValue, HtmlTextResult? textResultValue, HtmlMarkdownResult? markdownResultValue, HtmlProcessingStageTiming[] timingsValue, HtmlDiagnostic[][] stageDiagnosticsValue, long[] stageDiagnosticCountsValue, long[] stageDroppedDiagnosticCountsValue, HtmlDiagnostic[] diagnosticsValue, int maximumDiagnostics)
    {
        if (parseResultValue == null)
        {
            throw new ArgumentNullException("parseResultValue");
        }

        if (completeValue && truncatedValue)
        {
            throw new ArgumentException("A complete processing result cannot be truncated.", "completeValue");
        }

        if (articleValue != null && !articleValue.IsContentSanitized())
        {
            throw new ArgumentException("A processed article must contain sanitized content.", "articleValue");
        }

        if (htmlValue != null && (sanitizeResultValue == null || !sanitizeResultValue.IsSuccess()))
        {
            throw new ArgumentException("Renderable processing HTML requires successful sanitization.", "htmlValue");
        }

        if (maximumDiagnostics < 1)
        {
            throw new ArgumentOutOfRangeException("maximumDiagnostics");
        }

        this.success = successValue;
        this.complete = completeValue;
        this.truncated = truncatedValue;
        this.parseResult = parseResultValue;
        this.metadata = metadataValue;
        this.articleResult = articleResultValue;
        this.article = articleValue;
        this.sanitizeResult = sanitizeResultValue;
        this.html = htmlValue;
        this.textResult = textResultValue;
        this.markdownResult = markdownResultValue;
        this.timings = CopyTimings(timingsValue);
        this.stageDiagnostics = CopyStageDiagnostics(stageDiagnosticsValue);
        this.stageDiagnosticCounts = CopyCounts(stageDiagnosticCountsValue, "stageDiagnosticCountsValue");
        this.stageDroppedDiagnosticCounts = CopyCounts(stageDroppedDiagnosticCountsValue, "stageDroppedDiagnosticCountsValue");
        this.ValidateStageDiagnosticCounts();
        this.BuildDiagnostics(diagnosticsValue, maximumDiagnostics);
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.complete;
    }

    /// <summary>Reports whether the result was truncated by a configured limit.</summary>
    /// <returns><c>true</c> when a configured limit truncated the result; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return this.truncated;
    }

    /// <summary>Reports whether incomplete processing retained usable stage output.</summary>
    /// <returns><c>true</c> when processing is incomplete but retains its parse-stage tree; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.complete;
    }

    /// <summary>Reports whether cancellation stopped processing.</summary>
    /// <returns><c>true</c> when the pipeline or parse result records cancellation; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return this.parseResult.WasCancelled()
            || HtmlResultDiagnostics.ContainsCode(this.diagnostics,
                HtmlDiagnosticCode.OperationCancelled);
    }

    /// <summary>Gets the stable code explaining unsuccessful processing.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when processing succeeded completely.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success && this.complete)
        {
            return HtmlDiagnosticCode.None;
        }

        if (this.WasCancelled())
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.truncated)
        {
            return HtmlDiagnosticCode.PipelineLimitExceeded;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            HtmlDiagnosticCode.OperationFailed);
    }

    /// <summary>Gets the mandatory parse-stage result that supplies the pipeline document.</summary>
    /// <returns>The parse result, including its diagnostics and decoding metadata.</returns>
    public HtmlParseResult GetParseResult()
    {
        return this.parseResult;
    }

    /// <summary>Gets the document produced by the mandatory parse stage.</summary>
    /// <returns>The parsed document.</returns>
    public HtmlDocument GetDocument()
    {
        return this.parseResult.GetDocument();
    }

    /// <summary>Gets the .NET encoding instance used by the pipeline's byte-input parse stage.</summary>
    /// <returns>The detected encoding. Returns null when processing character input that did not require byte decoding.</returns>
    public Encoding? GetDetectedEncoding()
    {
        return this.parseResult.GetDetectedEncoding();
    }

    /// <summary>Gets the canonical encoding label recorded by byte decoding.</summary>
    /// <returns>The label, or an empty string for character input.</returns>
    public string GetDetectedEncodingLabel()
    {
        return this.parseResult.GetDetectedEncodingLabel();
    }

    /// <summary>Gets how byte input selected its decoder.</summary>
    /// <returns>The encoding-detection result. Returns null when processing character input.</returns>
    public HtmlEncodingDetectionResult? GetEncodingDetection()
    {
        return this.parseResult.GetEncodingDetection();
    }

    /// <summary>Gets precedence-resolved document metadata from the extraction stage.</summary>
    /// <returns>The metadata. Returns null when metadata extraction was not requested.</returns>
    public HtmlMetadata? GetMetadata()
    {
        return this.metadata;
    }

    /// <summary>Gets the unsanitized article-extraction stage report.</summary>
    /// <remarks>Use <see cref = "GetArticle"/> for the sanitized article
    /// produced by the processing pipeline.</remarks>
    /// <returns>The extraction-stage result. Returns null when article extraction was not requested.</returns>
    public HtmlArticleResult? GetArticleResult()
    {
        return this.articleResult;
    }

    /// <summary>Gets the processed article with sanitized content.</summary>
    /// <returns>The sanitized article. Returns null when article extraction
    /// or sanitization was not requested or did not succeed.</returns>
    public HtmlArticle? GetArticle()
    {
        return this.article;
    }

    /// <summary>Gets the unsanitized extraction-stage article.</summary>
    /// <remarks>This value is retained for extraction diagnostics and must
    /// not be treated as renderable content.</remarks>
    /// <returns>The extracted article. Returns null when extraction was not requested or did not produce an article.</returns>
    public HtmlArticle? GetExtractedArticle()
    {
        return this.articleResult == null ? null : this.articleResult.GetUnsanitizedArticle();
    }

    /// <summary>Gets the complete sanitizer-stage status, report, diagnostics, and output.</summary>
    /// <returns>The sanitize result. Returns null when sanitization was not requested.</returns>
    public HtmlSanitizeResult? GetSanitizeResult()
    {
        return this.sanitizeResult;
    }

    /// <summary>Gets the DOM output produced by successful sanitization.</summary>
    /// <returns>The sanitized node. Returns null when sanitization was skipped or failed.</returns>
    public HtmlNode? GetSanitizedNode()
    {
        return this.sanitizeResult == null ? null : this.sanitizeResult.GetNode();
    }

    /// <summary>Gets the bounded audit report produced by the sanitizer stage.</summary>
    /// <returns>The sanitizer report. Returns null when sanitization was not requested.</returns>
    public HtmlSanitizerReport? GetSanitizerReport()
    {
        return this.sanitizeResult == null ? null : this.sanitizeResult.GetReport();
    }

    /// <summary>Gets renderable HTML produced only after sanitization.</summary>
    /// <returns>Sanitized HTML. Returns null when serialization was not
    /// requested or did not complete safely.</returns>
    public string? GetHtml()
    {
        return this.html;
    }

    /// <summary>Gets the plain-text conversion status, output, and diagnostics.</summary>
    /// <returns>The text result. Returns null when text conversion was not requested.</returns>
    public HtmlTextResult? GetTextResult()
    {
        return this.textResult;
    }

    /// <summary>Gets the plain-text output from the conversion stage.</summary>
    /// <returns>The text. Returns null when text conversion was not requested.</returns>
    public string? GetText()
    {
        return this.textResult == null ? null : this.textResult.GetText();
    }

    /// <summary>Gets the Markdown conversion status, output, and diagnostics.</summary>
    /// <returns>The Markdown result. Returns null when Markdown conversion was not requested.</returns>
    public HtmlMarkdownResult? GetMarkdownResult()
    {
        return this.markdownResult;
    }

    /// <summary>Gets the Markdown output from the conversion stage.</summary>
    /// <returns>The Markdown. Returns null when Markdown conversion was not requested.</returns>
    public string? GetMarkdown()
    {
        return this.markdownResult == null ? null : this.markdownResult.GetMarkdown();
    }

    /// <summary>Gets timing records for the pipeline stages that actually ran.</summary>
    /// <returns>A snapshot of the stage timings; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlProcessingStageTiming[] GetStageTimings()
    {
        return CopyTimings(this.timings);
    }

    /// <summary>Gets the timing record for one requested pipeline stage.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>The recorded timing. Returns null when the requested stage did not run.</returns>
    public HtmlProcessingStageTiming? GetStageTiming(HtmlProcessingStage stage)
    {
        HtmlProcessingValidation.ValidateStage(stage, "stage");
        int index = 0;
        while (index < this.timings.Length)
        {
            if (this.timings[index].GetStage() == stage)
            {
                return this.timings[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Gets whether one pipeline stage was requested and how it ended.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>The stage status.</returns>
    public HtmlProcessingStageStatus GetStageStatus(HtmlProcessingStage stage)
    {
        HtmlProcessingStageTiming? timing = this.GetStageTiming(stage);
        return timing == null ? HtmlProcessingStageStatus.NotRequested : timing.GetStatus();
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many pipeline diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many diagnostics all requested pipeline stages produced before retention limits were applied.</summary>
    /// <returns>The total diagnostic count, including retained and dropped entries.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many pipeline diagnostics could not be retained.</summary>
    /// <returns>The number omitted from <see cref = "GetDiagnostics"/> because the shared diagnostic limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether the diagnostic retention limit discarded entries.</summary>
    /// <returns><c>true</c> when one or more diagnostics were not retained; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    /// <summary>Gets the retained diagnostics attributed to one pipeline stage.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>A snapshot of the stage diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetStageDiagnostics(HtmlProcessingStage stage)
    {
        HtmlProcessingValidation.ValidateStage(stage, "stage");
        return CopyDiagnostics(this.stageDiagnostics[(int)stage - 1]);
    }

    /// <summary>Gets how many diagnostics were retained for one stage.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>The length of the stage diagnostics snapshot.</returns>
    public int GetStageRetainedDiagnosticCount(HtmlProcessingStage stage)
    {
        HtmlProcessingValidation.ValidateStage(stage, "stage");
        return this.stageDiagnostics[(int)stage - 1].Length;
    }

    /// <summary>Gets how many diagnostics one stage produced before retention limits were applied.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>The stage's total diagnostic count, including retained and dropped entries.</returns>
    public long GetStageDiagnosticCount(HtmlProcessingStage stage)
    {
        HtmlProcessingValidation.ValidateStage(stage, "stage");
        return this.stageDiagnosticCounts[(int)stage - 1];
    }

    /// <summary>Gets how many diagnostics from one stage could not be retained.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns>The number omitted from <see cref = "GetStageDiagnostics"/> because the shared diagnostic limit was exhausted.</returns>
    public long GetStageDroppedDiagnosticCount(HtmlProcessingStage stage)
    {
        HtmlProcessingValidation.ValidateStage(stage, "stage");
        return this.stageDroppedDiagnosticCounts[(int)stage - 1];
    }

    /// <summary>Reports whether the stage diagnostic retention limit discarded entries.</summary>
    /// <param name = "stage">The pipeline stage to inspect.</param>
    /// <returns><c>true</c> when one or more stage diagnostics were not retained; otherwise, <c>false</c>.</returns>
    public bool WereStageDiagnosticsTruncated(HtmlProcessingStage stage)
    {
        return this.GetStageDroppedDiagnosticCount(stage) > 0;
    }

    internal void ValidateStageDiagnosticCounts()
    {
        if (this.stageDiagnostics.Length != this.timings.Length || this.stageDiagnosticCounts.Length != this.timings.Length || this.stageDroppedDiagnosticCounts.Length != this.timings.Length)
        {
            throw new ArgumentException("Processing stage report arrays must have equal lengths.", "stageDiagnosticsValue");
        }

        int index = 0;
        while (index < this.stageDiagnostics.Length)
        {
            long total = this.stageDiagnosticCounts[index];
            long dropped = this.stageDroppedDiagnosticCounts[index];
            if (dropped > total || this.stageDiagnostics[index].Length != total - dropped)
            {
                throw new ArgumentOutOfRangeException("stageDiagnosticCountsValue");
            }

            index = index + 1;
        }
    }

    internal void BuildDiagnostics(HtmlDiagnostic[] pipelineDiagnostics, int maximumDiagnostics)
    {
        if (pipelineDiagnostics == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        long total = pipelineDiagnostics.Length;
        int index = 0;
        while (index < this.stageDiagnosticCounts.Length)
        {
            total = AddSaturated(total, this.stageDiagnosticCounts[index]);
            index = index + 1;
        }

        int retainedCount = 0;
        index = 0;
        while (index < this.stageDiagnostics.Length)
        {
            retainedCount = AddRetained(retainedCount, this.stageDiagnostics[index].Length, maximumDiagnostics);
            index = index + 1;
        }

        retainedCount = AddRetained(retainedCount, pipelineDiagnostics.Length, maximumDiagnostics);
        this.diagnostics = new HtmlDiagnostic[retainedCount];
        int outputIndex = 0;
        index = 0;
        while (index < this.stageDiagnostics.Length)
        {
            outputIndex = CopyDiagnostics(this.stageDiagnostics[index], this.diagnostics, outputIndex);
            index = index + 1;
        }

        CopyDiagnostics(pipelineDiagnostics, this.diagnostics, outputIndex);
        this.diagnosticCount = total;
        this.droppedDiagnosticCount = total - retainedCount;
    }

    internal static HtmlProcessingStageTiming[] CopyTimings(HtmlProcessingStageTiming[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("timingsValue");
        }

        HtmlProcessingStageTiming[] result = new HtmlProcessingStageTiming[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Processing timings cannot contain null.", "timingsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Processing diagnostics cannot contain null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static HtmlDiagnostic[][] CopyStageDiagnostics(HtmlDiagnostic[][] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("stageDiagnosticsValue");
        }

        HtmlDiagnostic[][] result = new HtmlDiagnostic[values.Length][];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = CopyDiagnostics(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal static long[] CopyCounts(long[] values, string parameterName)
    {
        if (values == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        long[] result = new long[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static int AddRetained(int current, int additional, int maximum)
    {
        int remaining = maximum - current;
        return additional > remaining ? maximum : current + additional;
    }

    internal static int CopyDiagnostics(HtmlDiagnostic[] source, HtmlDiagnostic[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length && destinationIndex < destination.Length)
        {
            destination[destinationIndex] = source[index];
            destinationIndex = destinationIndex + 1;
            index = index + 1;
        }

        return destinationIndex;
    }

    internal static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first ? long.MaxValue : first + second;
    }
}

using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlProcessingResult"/>.</summary>
public interface IHtmlProcessingResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Reports whether incomplete processing retained usable stage output.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped processing.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining unsuccessful processing.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetParseResult operation.</summary>
    public HtmlParseResult GetParseResult();
    /// <summary>Executes the GetDocument operation.</summary>
    public HtmlDocument GetDocument();
    /// <summary>Executes the GetDetectedEncoding operation.</summary>
    public Encoding? GetDetectedEncoding();
    /// <summary>Executes the GetDetectedEncodingLabel operation.</summary>
    public string GetDetectedEncodingLabel();
    /// <summary>Executes the GetEncodingDetection operation.</summary>
    public HtmlEncodingDetectionResult? GetEncodingDetection();
    /// <summary>Executes the GetMetadata operation.</summary>
    public HtmlMetadata? GetMetadata();
    /// <summary>Executes the GetArticleResult operation.</summary>
    public HtmlArticleResult? GetArticleResult();
    /// <summary>Executes the GetArticle operation.</summary>
    public HtmlArticle? GetArticle();
    /// <summary>Executes the GetExtractedArticle operation.</summary>
    public HtmlArticle? GetExtractedArticle();
    /// <summary>Executes the GetSanitizeResult operation.</summary>
    public HtmlSanitizeResult? GetSanitizeResult();
    /// <summary>Executes the GetSanitizedNode operation.</summary>
    public HtmlNode? GetSanitizedNode();
    /// <summary>Executes the GetSanitizerReport operation.</summary>
    public HtmlSanitizerReport? GetSanitizerReport();
    /// <summary>Executes the GetHtml operation.</summary>
    public string? GetHtml();
    /// <summary>Executes the GetTextResult operation.</summary>
    public HtmlTextResult? GetTextResult();
    /// <summary>Executes the GetText operation.</summary>
    public string? GetText();
    /// <summary>Executes the GetMarkdownResult operation.</summary>
    public HtmlMarkdownResult? GetMarkdownResult();
    /// <summary>Executes the GetMarkdown operation.</summary>
    public string? GetMarkdown();
    /// <summary>Executes the GetStageTimings operation.</summary>
    public HtmlProcessingStageTiming[] GetStageTimings();
    /// <summary>Executes the GetStageTiming operation.</summary>
    public HtmlProcessingStageTiming? GetStageTiming(HtmlProcessingStage stage);
    /// <summary>Executes the GetStageStatus operation.</summary>
    public HtmlProcessingStageStatus GetStageStatus(HtmlProcessingStage stage);
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many pipeline diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Executes the GetDiagnosticCount operation.</summary>
    public long GetDiagnosticCount();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
    /// <summary>Executes the GetStageDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetStageDiagnostics(HtmlProcessingStage stage);
    /// <summary>Gets how many diagnostics were retained for one stage.</summary>
    public int GetStageRetainedDiagnosticCount(HtmlProcessingStage stage);
    /// <summary>Executes the GetStageDiagnosticCount operation.</summary>
    public long GetStageDiagnosticCount(HtmlProcessingStage stage);
    /// <summary>Executes the GetStageDroppedDiagnosticCount operation.</summary>
    public long GetStageDroppedDiagnosticCount(HtmlProcessingStage stage);
    /// <summary>Executes the WereStageDiagnosticsTruncated operation.</summary>
    public bool WereStageDiagnosticsTruncated(HtmlProcessingStage stage);
}

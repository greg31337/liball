using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizeResult"/>.</summary>
public interface IHtmlSanitizeResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Reports whether a successful sanitizer result is partial.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped sanitization.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining sanitizer failure.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetNode operation.</summary>
    public HtmlNode? GetNode();
    /// <summary>Gets the sanitized document produced by a document workflow.</summary>
    public HtmlDocument? GetDocument();
    /// <summary>Gets the sanitized fragment produced by a fragment workflow.</summary>
    public HtmlNode? GetFragment();
    /// <summary>Gets the sanitized subtree produced by a subtree workflow.</summary>
    public HtmlNode? GetSubtree();
    /// <summary>Executes the GetHtml operation.</summary>
    public string GetHtml();
    /// <summary>Executes the GetReport operation.</summary>
    public HtmlSanitizerReport GetReport();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many sanitizer diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total sanitizer diagnostic count before retention limits were applied.</summary>
    public long GetDiagnosticCount();
    /// <summary>Gets how many sanitizer diagnostics were dropped.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether sanitizer diagnostic retention was truncated.</summary>
    public bool WereDiagnosticsTruncated();
}

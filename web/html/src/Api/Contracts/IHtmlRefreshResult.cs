using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlRefreshResult"/>.</summary>
public interface IHtmlRefreshResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the HasUrl operation.</summary>
    public bool HasUrl();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
    /// <summary>Reports whether parsing completed without a resource limit.</summary>
    public bool IsComplete();
    /// <summary>Executes the GetDelaySeconds operation.</summary>
    public long GetDelaySeconds();
    /// <summary>Executes the GetUrl operation.</summary>
    public string GetUrl();
    /// <summary>Gets parsing diagnostics.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the primary failure code.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Gets the retained diagnostic count.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total diagnostic count.</summary>
    public long GetDiagnosticCount();
    /// <summary>Gets the dropped diagnostic count.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether diagnostics were truncated.</summary>
    public bool WereDiagnosticsTruncated();
}

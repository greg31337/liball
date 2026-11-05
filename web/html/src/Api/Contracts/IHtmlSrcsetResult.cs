using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSrcsetResult"/>.</summary>
public interface IHtmlSrcsetResult
{
    /// <summary>Executes the IsValid operation.</summary>
    public bool IsValid();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
    /// <summary>Executes the GetErrorCount operation.</summary>
    public int GetErrorCount();
    /// <summary>Executes the GetCandidates operation.</summary>
    public HtmlSrcsetCandidate[] GetCandidates();
    /// <summary>Reports whether usable candidates accompany errors or truncation.</summary>
    public bool IsPartial();
    /// <summary>Gets detailed parse diagnostics.</summary>
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

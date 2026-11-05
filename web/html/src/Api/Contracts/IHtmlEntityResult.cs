using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlEntityResult"/>.</summary>
public interface IHtmlEntityResult
{
    /// <summary>Executes the GetString operation.</summary>
    public string GetString();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the stable code explaining an incomplete entity operation.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Gets how many diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total diagnostic count before retention limits were applied.</summary>
    public long GetDiagnosticCount();
    /// <summary>Gets how many diagnostics were dropped.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether diagnostic retention was truncated.</summary>
    public bool WereDiagnosticsTruncated();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
    /// <summary>Reports whether the retained string is a partial result.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped the operation.</summary>
    public bool WasCancelled();
}

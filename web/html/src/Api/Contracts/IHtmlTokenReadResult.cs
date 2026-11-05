using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTokenReadResult"/>.</summary>
public interface IHtmlTokenReadResult
{
    /// <summary>Executes the GetStatus operation.</summary>
    public HtmlTokenReadStatus GetStatus();
    /// <summary>Executes the HasToken operation.</summary>
    public bool HasToken();
    /// <summary>Executes the GetToken operation.</summary>
    public HtmlToken? GetToken();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the stable code that explains a terminal limit, cancellation, or failure status.</summary>
    /// <returns>The primary diagnostic code, or <see cref = "HtmlDiagnosticCode.None"/> when the read did not fail.</returns>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Gets how many diagnostics are retained by this result.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount();
    /// <summary>Executes the IsPartial operation.</summary>
    public bool IsPartial();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
}

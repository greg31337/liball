using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTextResult"/>.</summary>
public interface IHtmlTextResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Reports whether incomplete conversion retained safe text.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped conversion.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining unsuccessful conversion.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetText operation.</summary>
    public string GetText();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many conversion diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Executes the GetDiagnosticCount operation.</summary>
    public long GetDiagnosticCount();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
}

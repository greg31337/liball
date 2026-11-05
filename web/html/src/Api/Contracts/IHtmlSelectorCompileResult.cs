using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSelectorCompileResult"/>.</summary>
public interface IHtmlSelectorCompileResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the GetSelector operation.</summary>
    public HtmlSelector? GetSelector();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the stable code explaining failed selector compilation.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Gets how many selector diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total selector diagnostic count.</summary>
    public long GetDiagnosticCount();
    /// <summary>Gets how many selector diagnostics were dropped.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether selector diagnostic retention was truncated.</summary>
    public bool WereDiagnosticsTruncated();
    /// <summary>Reports whether compilation returned a partial selector.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped compilation.</summary>
    public bool WasCancelled();
    /// <summary>Executes the GetErrors operation.</summary>
    public HtmlSelectorCompileError[] GetErrors();
}

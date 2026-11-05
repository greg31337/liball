using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlUrlResult"/>.</summary>
public interface IHtmlUrlResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the GetUrl operation.</summary>
    public string GetUrl();
    /// <summary>Executes the GetScheme operation.</summary>
    public string GetScheme();
    /// <summary>Executes the WasRelative operation.</summary>
    public bool WasRelative();
    /// <summary>Executes the WasDeobfuscated operation.</summary>
    public bool WasDeobfuscated();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
    /// <summary>Reports whether a failed URL operation retained partial output.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped URL processing.</summary>
    public bool WasCancelled();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the stable code explaining URL failure.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Gets how many URL diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Gets the total URL diagnostic count.</summary>
    public long GetDiagnosticCount();
    /// <summary>Gets how many URL diagnostics were dropped.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Reports whether URL diagnostic retention was truncated.</summary>
    public bool WereDiagnosticsTruncated();
}

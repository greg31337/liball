using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlMarkdownCompileResult"/>.</summary>
public interface IHtmlMarkdownCompileResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Reports whether compilation returned partial options.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped compilation.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining compilation failure.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetOptions operation.</summary>
    public HtmlCompiledMarkdownOptions? GetOptions();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many compile diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Executes the GetDiagnosticCount operation.</summary>
    public long GetDiagnosticCount();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
}

using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlArticleResult"/>.</summary>
public interface IHtmlArticleResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasTruncated operation.</summary>
    public bool WasTruncated();
    /// <summary>Reports whether incomplete extraction retained an article.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped extraction.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining unsuccessful extraction.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetArticle operation.</summary>
    public HtmlArticle? GetArticle();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many extraction diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Executes the GetDiagnosticCount operation.</summary>
    public long GetDiagnosticCount();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
}

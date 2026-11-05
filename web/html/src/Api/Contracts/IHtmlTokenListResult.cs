using System;

namespace html;
/// <summary>Defines bounded token-list result inspection.</summary>
public interface IHtmlTokenListResult
{
    /// <summary>Reports whether parsing completed.</summary>
    public bool IsComplete();
    /// <summary>Reports whether a limit was exceeded.</summary>
    public bool WasLimitExceeded();
    /// <summary>Gets retained tokens.</summary>
    public string[] GetTokens();
    /// <summary>Gets parsing diagnostics.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the stable failure code.</summary>
    public HtmlDiagnosticCode GetFailureCode();
}

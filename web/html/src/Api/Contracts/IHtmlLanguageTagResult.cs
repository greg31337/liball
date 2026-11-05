using System;

namespace html;
/// <summary>Defines language-tag result inspection operations.</summary>
public interface IHtmlLanguageTagResult
{
    /// <summary>Reports whether parsing succeeded.</summary>
    public bool IsSuccess();
    /// <summary>Gets the normalized tag.</summary>
    public string GetTag();
    /// <summary>Gets the primary-language subtag.</summary>
    public string GetPrimaryLanguage();
    /// <summary>Reports whether a limit was exceeded.</summary>
    public bool WasLimitExceeded();
    /// <summary>Gets parsing diagnostics.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets the primary failure code.</summary>
    public HtmlDiagnosticCode GetFailureCode();
}

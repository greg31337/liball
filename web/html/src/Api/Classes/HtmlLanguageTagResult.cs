using System;

namespace html;
/// <summary>Reports normalized language-tag parsing output and diagnostics.</summary>
public class HtmlLanguageTagResult : IHtmlLanguageTagResult
{
    internal bool success;
    internal bool limitExceeded;
    internal string tag;
    internal string primaryLanguage;
    internal HtmlDiagnostic[] diagnostics;

    internal HtmlLanguageTagResult(bool successValue,
        bool limitExceededValue, string tagValue,
        string primaryLanguageValue, HtmlDiagnostic[] diagnosticsValue)
    {
        this.success = successValue;
        this.limitExceeded = limitExceededValue;
        this.tag = tagValue;
        this.primaryLanguage = primaryLanguageValue;
        this.diagnostics = HtmlUrlResult.CopyDiagnostics(diagnosticsValue);
    }

    /// <summary>Reports whether the input was a well-formed language tag.</summary>
    /// <returns><c>true</c> for a valid tag; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Gets the normalized language tag.</summary>
    /// <returns>The normalized tag when parsing succeeded; otherwise, an empty string.</returns>
    public string GetTag()
    {
        return this.tag;
    }

    /// <summary>Gets the normalized primary-language subtag.</summary>
    /// <returns>The primary language when parsing succeeded; otherwise, an empty string.</returns>
    public string GetPrimaryLanguage()
    {
        return this.primaryLanguage;
    }

    /// <summary>Reports whether parsing exceeded a configured limit.</summary>
    /// <returns><c>true</c> when a limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Gets language-tag diagnostics.</summary>
    /// <returns>A caller-owned snapshot; the array is empty for valid input.</returns>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return HtmlUrlResult.CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable failure code.</summary>
    /// <returns>The primary diagnostic code, or <see cref="HtmlDiagnosticCode.None"/> on success.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success)
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            this.limitExceeded
                ? HtmlDiagnosticCode.MicrosyntaxLimitExceeded
                : HtmlDiagnosticCode.MicrosyntaxInvalid);
    }
}

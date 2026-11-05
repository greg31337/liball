using System;
using System.Globalization;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlDiagnostic"/>.</summary>
public interface IHtmlDiagnostic
{
    /// <summary>Executes the GetCode operation.</summary>
    public HtmlDiagnosticCode GetCode();
    /// <summary>Executes the GetIdentifier operation.</summary>
    public string GetIdentifier();
    /// <summary>Executes the GetSeverity operation.</summary>
    public HtmlDiagnosticSeverity GetSeverity();
    /// <summary>Executes the GetMessage operation.</summary>
    public string GetMessage();
    /// <summary>Executes the GetSpan operation.</summary>
    public HtmlSourceSpan GetSpan();
}

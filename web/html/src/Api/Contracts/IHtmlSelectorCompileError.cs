using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSelectorCompileError"/>.</summary>
public interface IHtmlSelectorCompileError
{
    /// <summary>Executes the GetCode operation.</summary>
    public HtmlDiagnosticCode GetCode();
    /// <summary>Executes the GetIdentifier operation.</summary>
    public string GetIdentifier();
    /// <summary>Executes the GetMessage operation.</summary>
    public string GetMessage();
    /// <summary>Executes the GetPosition operation.</summary>
    public HtmlSourcePosition GetPosition();
    /// <summary>Executes the GetKind operation.</summary>
    public HtmlSelectorCompileErrorKind GetKind();
    /// <summary>Executes the IsUnsupportedFeature operation.</summary>
    public bool IsUnsupportedFeature();
}

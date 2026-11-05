using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlAttribute"/>.</summary>
public interface IHtmlAttribute
{
    /// <summary>Executes the GetName operation.</summary>
    public string GetName();
    /// <summary>Executes the GetLocalName operation.</summary>
    public string GetLocalName();
    /// <summary>Executes the GetPrefix operation.</summary>
    public string GetPrefix();
    /// <summary>Executes the GetNamespaceUri operation.</summary>
    public string GetNamespaceUri();
    /// <summary>Executes the GetValue operation.</summary>
    public string GetValue();
    /// <summary>Executes the GetSourceSpan operation.</summary>
    public HtmlSourceSpan GetSourceSpan();
}

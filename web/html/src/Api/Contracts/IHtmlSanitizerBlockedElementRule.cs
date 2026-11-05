using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerBlockedElementRule"/>.</summary>
public interface IHtmlSanitizerBlockedElementRule
{
    /// <summary>Executes the GetNamespaceUri operation.</summary>
    public string GetNamespaceUri();
    /// <summary>Executes the GetLocalName operation.</summary>
    public string GetLocalName();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlBlockedElementAction GetAction();
}

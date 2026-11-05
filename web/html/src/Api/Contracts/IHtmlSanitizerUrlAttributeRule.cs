using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerUrlAttributeRule"/>.</summary>
public interface IHtmlSanitizerUrlAttributeRule
{
    /// <summary>Executes the GetElementNamespaceUri operation.</summary>
    public string GetElementNamespaceUri();
    /// <summary>Executes the GetElementLocalName operation.</summary>
    public string GetElementLocalName();
    /// <summary>Executes the GetAttributeNamespaceUri operation.</summary>
    public string GetAttributeNamespaceUri();
    /// <summary>Executes the GetAttributeLocalName operation.</summary>
    public string GetAttributeLocalName();
    /// <summary>Executes the GetPolicy operation.</summary>
    public HtmlSanitizerUrlPolicy GetPolicy();
}

using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerAttributeTokenRule"/>.</summary>
public interface IHtmlSanitizerAttributeTokenRule
{
    /// <summary>Executes the GetElementNamespaceUri operation.</summary>
    public string GetElementNamespaceUri();
    /// <summary>Executes the GetElementLocalName operation.</summary>
    public string GetElementLocalName();
    /// <summary>Executes the GetAttributeNamespaceUri operation.</summary>
    public string GetAttributeNamespaceUri();
    /// <summary>Executes the GetAttributeLocalName operation.</summary>
    public string GetAttributeLocalName();
    /// <summary>Executes the GetAttributePrefix operation.</summary>
    public string GetAttributePrefix();
    /// <summary>Executes the GetTokens operation.</summary>
    public string[] GetTokens();
}

using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerAttributeValueRule"/>.</summary>
public interface IHtmlSanitizerAttributeValueRule
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
    /// <summary>Executes the GetValue operation.</summary>
    public string GetValue();
    /// <summary>Executes the IsForced operation.</summary>
    public bool IsForced();
}

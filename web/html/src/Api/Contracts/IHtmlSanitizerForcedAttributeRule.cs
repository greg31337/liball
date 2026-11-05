using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerForcedAttributeRule"/>.</summary>
public interface IHtmlSanitizerForcedAttributeRule
{
    /// <summary>Executes the GetElementName operation.</summary>
    public string GetElementName();
    /// <summary>Executes the GetAttributeName operation.</summary>
    public string GetAttributeName();
    /// <summary>Executes the GetValue operation.</summary>
    public string GetValue();
}

using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerElementAttributeRule"/>.</summary>
public interface IHtmlSanitizerElementAttributeRule
{
    /// <summary>Executes the GetElementName operation.</summary>
    public string GetElementName();
    /// <summary>Executes the GetAttributeName operation.</summary>
    public string GetAttributeName();
}

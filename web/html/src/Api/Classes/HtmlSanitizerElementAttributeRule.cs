using System;

namespace html;
/// <summary>Allows one unnamespaced attribute on one HTML element.</summary>
public class HtmlSanitizerElementAttributeRule : IHtmlSanitizerElementAttributeRule
{
    internal string elementName;
    internal string attributeName;
    internal HtmlSanitizerElementAttributeRule(string elementNameValue, string attributeNameValue)
    {
        this.elementName = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementNameValue, "elementNameValue");
        this.attributeName = HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeNameValue, "attributeNameValue");
    }

    /// <summary>Gets the normalized HTML element name matched by this allow rule.</summary>
    /// <returns>The ASCII-lowercase element name.</returns>
    public string GetElementName()
    {
        return this.elementName;
    }

    /// <summary>Gets the normalized unnamespaced attribute name allowed on the matched element.</summary>
    /// <returns>The ASCII-lowercase attribute name.</returns>
    public string GetAttributeName()
    {
        return this.attributeName;
    }

    internal HtmlSanitizerElementAttributeRule Copy()
    {
        return new HtmlSanitizerElementAttributeRule(this.elementName, this.attributeName);
    }
}

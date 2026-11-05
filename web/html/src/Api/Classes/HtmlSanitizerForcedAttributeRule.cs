using System;

namespace html;
/// <summary>Defines an unnamespaced attribute value that is added to a matching HTML element.</summary>
public class HtmlSanitizerForcedAttributeRule : IHtmlSanitizerForcedAttributeRule
{
    internal string elementName;
    internal string attributeName;
    internal string value;
    internal HtmlSanitizerForcedAttributeRule(string elementNameValue, string attributeNameValue, string valueValue)
    {
        this.elementName = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementNameValue, "elementNameValue");
        this.attributeName = HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeNameValue, "attributeNameValue");
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        this.value = valueValue;
    }

    /// <summary>Gets the normalized HTML element name that receives the forced attribute.</summary>
    /// <returns>The ASCII-lowercase element name.</returns>
    public string GetElementName()
    {
        return this.elementName;
    }

    /// <summary>Gets the normalized unnamespaced attribute name added or overwritten by this rule.</summary>
    /// <returns>The ASCII-lowercase attribute name.</returns>
    public string GetAttributeName()
    {
        return this.attributeName;
    }

    /// <summary>Gets the trusted value forced onto the matched element.</summary>
    /// <returns>The exact configured attribute value.</returns>
    public string GetValue()
    {
        return this.value;
    }

    internal HtmlSanitizerForcedAttributeRule Copy()
    {
        return new HtmlSanitizerForcedAttributeRule(this.elementName, this.attributeName, this.value);
    }
}

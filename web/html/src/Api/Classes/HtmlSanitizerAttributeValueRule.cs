using System;

namespace html;
/// <summary>Requires or forces one namespace-aware attribute value on a matching element.</summary>
public class HtmlSanitizerAttributeValueRule : IHtmlSanitizerAttributeValueRule
{
    internal string elementNamespaceUri;
    internal string elementLocalName;
    internal string attributeNamespaceUri;
    internal string attributeLocalName;
    internal string attributePrefix;
    internal string value;
    internal bool forced;
    internal HtmlSanitizerAttributeValueRule(string elementNamespaceUriValue, string elementLocalNameValue, string attributeNamespaceUriValue, string attributeLocalNameValue, string attributePrefixValue, string valueValue, bool forcedValue)
    {
        this.elementNamespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUriValue, "elementNamespaceUriValue");
        this.elementLocalName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(this.elementNamespaceUri, elementLocalNameValue, "elementLocalNameValue");
        this.attributeNamespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(attributeNamespaceUriValue, "attributeNamespaceUriValue");
        this.attributeLocalName = HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(this.attributeNamespaceUri, attributeLocalNameValue, "attributeLocalNameValue");
        if (attributePrefixValue == null)
        {
            throw new ArgumentNullException("attributePrefixValue");
        }

        this.attributePrefix = HtmlSanitizerPolicyNames.NormalizePrefix(attributePrefixValue, "attributePrefixValue");
        if (this.attributePrefix.Length > 0 && this.attributeNamespaceUri.Length == 0)
        {
            throw new ArgumentException("A prefixed attribute must have a namespace URI.", "attributePrefixValue");
        }

        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        this.value = valueValue;
        this.forced = forcedValue;
    }

    /// <summary>Gets the namespace URI of elements matched by this fixed-value rule.</summary>
    /// <returns>The normalized element namespace URI.</returns>
    public string GetElementNamespaceUri()
    {
        return this.elementNamespaceUri;
    }

    /// <summary>Gets the local name of elements matched by this fixed-value rule.</summary>
    /// <returns>The normalized element local name.</returns>
    public string GetElementLocalName()
    {
        return this.elementLocalName;
    }

    /// <summary>Gets the namespace URI of attributes matched by this fixed-value rule.</summary>
    /// <returns>The normalized attribute namespace URI, or an empty string for an unnamespaced attribute.</returns>
    public string GetAttributeNamespaceUri()
    {
        return this.attributeNamespaceUri;
    }

    /// <summary>Gets the local name of attributes matched by this fixed-value rule.</summary>
    /// <returns>The normalized attribute local name.</returns>
    public string GetAttributeLocalName()
    {
        return this.attributeLocalName;
    }

    /// <summary>Gets the namespace prefix used when this rule creates or overwrites the attribute.</summary>
    /// <returns>The configured prefix, or an empty string for no prefix.</returns>
    public string GetAttributePrefix()
    {
        return this.attributePrefix;
    }

    /// <summary>Gets the value used by this attribute-value rule.</summary>
    /// <returns>The configured comparison or replacement value.</returns>
    public string GetValue()
    {
        return this.value;
    }

    /// <summary>Reports whether the rule requires the attribute to be present.</summary>
    /// <returns><c>true</c> when the attribute is required; otherwise, <c>false</c>.</returns>
    public bool IsForced()
    {
        return this.forced;
    }

    internal HtmlSanitizerAttributeValueRule Copy()
    {
        return new HtmlSanitizerAttributeValueRule(this.elementNamespaceUri, this.elementLocalName, this.attributeNamespaceUri, this.attributeLocalName, this.attributePrefix, this.value, this.forced);
    }
}

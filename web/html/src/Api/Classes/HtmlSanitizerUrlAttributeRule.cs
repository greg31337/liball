using System;

namespace html;
/// <summary>Assigns a URL policy to one namespace-aware attribute on a matching element.</summary>
public class HtmlSanitizerUrlAttributeRule : IHtmlSanitizerUrlAttributeRule
{
    internal string elementNamespaceUri;
    internal string elementLocalName;
    internal string attributeNamespaceUri;
    internal string attributeLocalName;
    internal HtmlSanitizerUrlPolicy policy;
    internal HtmlSanitizerUrlAttributeRule(string elementNamespaceUriValue, string elementLocalNameValue, string attributeNamespaceUriValue, string attributeLocalNameValue, HtmlSanitizerUrlPolicy policyValue)
    {
        this.elementNamespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUriValue, "elementNamespaceUriValue");
        this.elementLocalName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(this.elementNamespaceUri, elementLocalNameValue, "elementLocalNameValue");
        this.attributeNamespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(attributeNamespaceUriValue, "attributeNamespaceUriValue");
        this.attributeLocalName = HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(this.attributeNamespaceUri, attributeLocalNameValue, "attributeLocalNameValue");
        if (policyValue == null)
        {
            throw new ArgumentNullException("policyValue");
        }

        this.policy = policyValue.Copy();
    }

    /// <summary>Gets the namespace URI of elements matched by this URL-attribute rule.</summary>
    /// <returns>The normalized element namespace URI.</returns>
    public string GetElementNamespaceUri()
    {
        return this.elementNamespaceUri;
    }

    /// <summary>Gets the local name of elements matched by this URL-attribute rule.</summary>
    /// <returns>The normalized element local name.</returns>
    public string GetElementLocalName()
    {
        return this.elementLocalName;
    }

    /// <summary>Gets the namespace URI of attributes matched by this URL-attribute rule.</summary>
    /// <returns>The normalized attribute namespace URI, or an empty string for an unnamespaced attribute.</returns>
    public string GetAttributeNamespaceUri()
    {
        return this.attributeNamespaceUri;
    }

    /// <summary>Gets the local name of attributes matched by this URL-attribute rule.</summary>
    /// <returns>The normalized attribute local name.</returns>
    public string GetAttributeLocalName()
    {
        return this.attributeLocalName;
    }

    /// <summary>Gets an immutable snapshot of the URL policy assigned to this element and attribute pair.</summary>
    /// <returns>A policy value whose allowlist arrays are independent snapshots.</returns>
    public HtmlSanitizerUrlPolicy GetPolicy()
    {
        return this.policy.Copy();
    }

    internal HtmlSanitizerUrlAttributeRule Copy()
    {
        return new HtmlSanitizerUrlAttributeRule(this.elementNamespaceUri, this.elementLocalName, this.attributeNamespaceUri, this.attributeLocalName, this.policy);
    }
}

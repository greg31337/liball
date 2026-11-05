using System;

namespace html;
/// <summary>Stores a namespace URI and local name used to identify an allowed element.</summary>
public class HtmlSanitizerElementName : IHtmlSanitizerElementName
{
    internal string namespaceUri = "";
    internal string localName = "";
    internal HtmlSanitizerElementName(string namespaceUriValue, string localNameValue)
    {
        this.namespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(namespaceUriValue, "namespaceUriValue");
        this.localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(this.namespaceUri, localNameValue, "localNameValue");
    }

    /// <summary>Gets the namespace URI used to match the element.</summary>
    /// <returns>The normalized namespace URI, or an empty string for no namespace.</returns>
    public string GetNamespaceUri()
    {
        return this.namespaceUri;
    }

    /// <summary>Gets the namespace-local element name used by the rule.</summary>
    /// <returns>The normalized local name.</returns>
    public string GetLocalName()
    {
        return this.localName;
    }

    internal HtmlSanitizerElementName Copy()
    {
        return new HtmlSanitizerElementName(this.namespaceUri, this.localName);
    }
}

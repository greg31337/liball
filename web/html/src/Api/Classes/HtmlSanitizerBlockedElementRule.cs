using System;

namespace html;
/// <summary>Associates a namespace-aware element name with its blocked-element action.</summary>
public class HtmlSanitizerBlockedElementRule : IHtmlSanitizerBlockedElementRule
{
    internal string namespaceUri = "";
    internal string localName = "";
    internal HtmlBlockedElementAction action;
    internal HtmlSanitizerBlockedElementRule(string namespaceUriValue, string localNameValue, HtmlBlockedElementAction actionValue)
    {
        this.Initialize(namespaceUriValue, localNameValue, actionValue);
    }

    internal void Initialize(string namespaceUriValue, string localNameValue, HtmlBlockedElementAction actionValue)
    {
        this.namespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(namespaceUriValue, "namespaceUriValue");
        this.localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(this.namespaceUri, localNameValue, "localNameValue");
        HtmlSanitizerPolicyNames.ValidateAction(actionValue, "actionValue");
        this.action = actionValue;
    }

    internal string GetElementName()
    {
        return this.localName;
    }

    /// <summary>Gets the namespace URI used to match blocked elements.</summary>
    /// <returns>The normalized namespace URI.</returns>
    public string GetNamespaceUri()
    {
        return this.namespaceUri;
    }

    /// <summary>Gets the namespace-local name used to match blocked elements.</summary>
    /// <returns>The normalized local name.</returns>
    public string GetLocalName()
    {
        return this.localName;
    }

    /// <summary>Gets the behavior applied when this rule matches.</summary>
    /// <returns>The configured rule action.</returns>
    public HtmlBlockedElementAction GetAction()
    {
        return this.action;
    }

    internal HtmlSanitizerBlockedElementRule Copy()
    {
        return new HtmlSanitizerBlockedElementRule(this.namespaceUri, this.localName, this.action);
    }
}

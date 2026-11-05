using System;

namespace html;
/// <summary>Defines how a matching attribute is kept, dropped, renamed, or normalized.</summary>
public class HtmlSanitizerAttributeRule : IHtmlSanitizerAttributeRule
{
    internal bool elementSpecific;
    internal string elementNamespaceUri;
    internal string elementLocalName;
    internal string attributeNamespaceUri;
    internal string attributeLocalName;
    internal HtmlSanitizerAttributeAction action;
    internal string targetNamespaceUri;
    internal string targetLocalName;
    internal string targetPrefix;
    internal HtmlSanitizerAttributeNormalization normalization;
    internal HtmlSanitizerAttributeValidation validation;
    internal string valuePrefix;
    internal HtmlSanitizerAttributeRule(bool elementSpecificValue, string elementNamespaceUriValue, string elementLocalNameValue, string attributeNamespaceUriValue, string attributeLocalNameValue, HtmlSanitizerAttributeAction actionValue, string targetNamespaceUriValue, string targetLocalNameValue, string targetPrefixValue, HtmlSanitizerAttributeNormalization normalizationValue, HtmlSanitizerAttributeValidation validationValue, string valuePrefixValue)
    {
        this.elementSpecific = elementSpecificValue;
        this.elementNamespaceUri = elementSpecificValue ? HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUriValue, "elementNamespaceUriValue") : "";
        this.elementLocalName = elementSpecificValue ? HtmlSanitizerPolicyNames.NormalizeElementLocalName(this.elementNamespaceUri, elementLocalNameValue, "elementLocalNameValue") : "";
        this.attributeNamespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(attributeNamespaceUriValue, "attributeNamespaceUriValue");
        this.attributeLocalName = HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(this.attributeNamespaceUri, attributeLocalNameValue, "attributeLocalNameValue");
        HtmlSanitizerPolicyNames.ValidateAttributeAction(actionValue, "actionValue");
        this.action = actionValue;
        this.targetNamespaceUri = actionValue == HtmlSanitizerAttributeAction.Rename ? HtmlSanitizerPolicyNames.ValidateNamespaceUri(targetNamespaceUriValue, "targetNamespaceUriValue") : "";
        this.targetLocalName = actionValue == HtmlSanitizerAttributeAction.Rename ? HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(this.targetNamespaceUri, targetLocalNameValue, "targetLocalNameValue") : "";
        if (targetPrefixValue == null)
        {
            throw new ArgumentNullException("targetPrefixValue");
        }

        this.targetPrefix = actionValue == HtmlSanitizerAttributeAction.Rename ? HtmlSanitizerPolicyNames.NormalizePrefix(targetPrefixValue, "targetPrefixValue") : "";
        if (this.targetPrefix.Length > 0 && this.targetNamespaceUri.Length == 0)
        {
            throw new ArgumentException("A prefixed attribute must have a namespace URI.", "targetPrefixValue");
        }

        HtmlSanitizerPolicyNames.ValidateAttributeNormalization(normalizationValue, "normalizationValue");
        this.normalization = normalizationValue;
        HtmlSanitizerPolicyNames.ValidateAttributeValidation(validationValue, "validationValue");
        this.validation = validationValue;
        if (valuePrefixValue == null)
        {
            throw new ArgumentNullException("valuePrefixValue");
        }

        if (actionValue == HtmlSanitizerAttributeAction.Prefix
            && valuePrefixValue.Length == 0)
        {
            throw new ArgumentException("An attribute value prefix cannot be empty.", "valuePrefixValue");
        }

        this.valuePrefix = actionValue == HtmlSanitizerAttributeAction.Prefix
            ? valuePrefixValue : "";
    }

    /// <summary>Reports whether the rule applies only to a named element.</summary>
    /// <returns><c>true</c> when the rule is restricted to one element name; otherwise, <c>false</c>.</returns>
    public bool IsElementSpecific()
    {
        return this.elementSpecific;
    }

    /// <summary>Gets the namespace URI used to restrict this attribute rule to an element.</summary>
    /// <returns>The normalized namespace URI, or an empty string when <see cref = "IsElementSpecific"/> is <c>false</c>.</returns>
    public string GetElementNamespaceUri()
    {
        return this.elementNamespaceUri;
    }

    /// <summary>Gets the local name used to restrict this attribute rule to an element.</summary>
    /// <returns>The normalized local name, or an empty string when <see cref = "IsElementSpecific"/> is <c>false</c>.</returns>
    public string GetElementLocalName()
    {
        return this.elementLocalName;
    }

    /// <summary>Gets the namespace URI of attributes matched by this rule.</summary>
    /// <returns>The normalized namespace URI, or an empty string for unnamespaced attributes.</returns>
    public string GetAttributeNamespaceUri()
    {
        return this.attributeNamespaceUri;
    }

    /// <summary>Gets the local name of attributes matched by this rule.</summary>
    /// <returns>The normalized attribute local name.</returns>
    public string GetAttributeLocalName()
    {
        return this.attributeLocalName;
    }

    /// <summary>Gets the behavior applied when this rule matches.</summary>
    /// <returns>The configured rule action.</returns>
    public HtmlSanitizerAttributeAction GetAction()
    {
        return this.action;
    }

    /// <summary>Gets the namespace URI assigned when this rule renames an attribute.</summary>
    /// <returns>The normalized target namespace URI, or an empty string when no namespace is assigned.</returns>
    public string GetTargetNamespaceUri()
    {
        return this.targetNamespaceUri;
    }

    /// <summary>Gets the local name assigned when this rule renames an attribute.</summary>
    /// <returns>The normalized target local name, or an empty string when the action does not rename.</returns>
    public string GetTargetLocalName()
    {
        return this.targetLocalName;
    }

    /// <summary>Gets the namespace prefix assigned when this rule renames an attribute.</summary>
    /// <returns>The target prefix, or an empty string when no prefix is assigned.</returns>
    public string GetTargetPrefix()
    {
        return this.targetPrefix;
    }

    /// <summary>Gets the value-normalization steps applied after this rule accepts an attribute.</summary>
    /// <returns>The configured normalization flags.</returns>
    public HtmlSanitizerAttributeNormalization GetNormalization()
    {
        return this.normalization;
    }

    /// <summary>Gets the built-in validation applied to matching values.</summary>
    /// <returns>The configured validation kind.</returns>
    public HtmlSanitizerAttributeValidation GetValidation()
    {
        return this.validation;
    }

    /// <summary>Gets the prefix ensured on matching values.</summary>
    /// <returns>The configured prefix, or an empty string for other actions.</returns>
    public string GetValuePrefix()
    {
        return this.valuePrefix;
    }

    internal HtmlSanitizerAttributeRule Copy()
    {
        return new HtmlSanitizerAttributeRule(this.elementSpecific, this.elementNamespaceUri, this.elementLocalName, this.attributeNamespaceUri, this.attributeLocalName, this.action, this.targetNamespaceUri, this.targetLocalName, this.targetPrefix, this.normalization, this.validation, this.valuePrefix);
    }
}

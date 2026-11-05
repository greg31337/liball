using System;

namespace html;
/// <summary>Defines the allowed tokens for one namespace-aware attribute on a matching element.</summary>
public class HtmlSanitizerAttributeTokenRule : IHtmlSanitizerAttributeTokenRule
{
    internal string elementNamespaceUri;
    internal string elementLocalName;
    internal string attributeNamespaceUri;
    internal string attributeLocalName;
    internal string attributePrefix;
    internal string[] tokens;
    internal HtmlSanitizerAttributeTokenRule(string elementNamespaceUriValue, string elementLocalNameValue, string attributeNamespaceUriValue, string attributeLocalNameValue, string attributePrefixValue, string[] tokensValue)
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

        if (tokensValue == null)
        {
            throw new ArgumentNullException("tokensValue");
        }

        if (tokensValue.Length == 0)
        {
            throw new ArgumentException("At least one forced attribute token is required.", "tokensValue");
        }

        this.tokens = new string[tokensValue.Length];
        int index = 0;
        while (index < tokensValue.Length)
        {
            string token = tokensValue[index];
            if (token == null)
            {
                throw new ArgumentException("A forced attribute token cannot be null.", "tokensValue");
            }

            HtmlTokenLists.SerializeSpaceSeparated(new string[] { token });
            if (HtmlTokenLists.ContainsAsciiInsensitive(this.tokens, index, token))
            {
                throw new ArgumentException("Forced attribute tokens must be unique ignoring ASCII case.", "tokensValue");
            }

            this.tokens[index] = token;
            index = index + 1;
        }
    }

    /// <summary>Gets the namespace URI of elements matched by this required-token rule.</summary>
    /// <returns>The normalized element namespace URI.</returns>
    public string GetElementNamespaceUri()
    {
        return this.elementNamespaceUri;
    }

    /// <summary>Gets the local name of elements matched by this required-token rule.</summary>
    /// <returns>The normalized element local name.</returns>
    public string GetElementLocalName()
    {
        return this.elementLocalName;
    }

    /// <summary>Gets the namespace URI of attributes matched by this required-token rule.</summary>
    /// <returns>The normalized attribute namespace URI, or an empty string for an unnamespaced attribute.</returns>
    public string GetAttributeNamespaceUri()
    {
        return this.attributeNamespaceUri;
    }

    /// <summary>Gets the local name of attributes matched by this required-token rule.</summary>
    /// <returns>The normalized attribute local name.</returns>
    public string GetAttributeLocalName()
    {
        return this.attributeLocalName;
    }

    /// <summary>Gets the namespace prefix used when this rule creates the attribute.</summary>
    /// <returns>The configured prefix, or an empty string for no prefix.</returns>
    public string GetAttributePrefix()
    {
        return this.attributePrefix;
    }

    /// <summary>Gets the required attribute tokens in their normalized comparison form.</summary>
    /// <returns>A snapshot of the tokens; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetTokens()
    {
        string[] result = new string[this.tokens.Length];
        Array.Copy(this.tokens, result, this.tokens.Length);
        return result;
    }

    internal HtmlSanitizerAttributeTokenRule Copy()
    {
        return new HtmlSanitizerAttributeTokenRule(this.elementNamespaceUri, this.elementLocalName, this.attributeNamespaceUri, this.attributeLocalName, this.attributePrefix, this.tokens);
    }
}

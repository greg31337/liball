using System;

namespace html;
/// <summary>Associates a CSS selector namespace prefix with a namespace URI.</summary>
public class HtmlSelectorNamespaceBinding
{
    /// <summary>The non-empty prefix used in selector qualified names.</summary>
    public string Prefix;
    /// <summary>The namespace URI matched by the prefix.</summary>
    public string NamespaceUri;

    /// <summary>Creates a selector namespace binding.</summary>
    /// <param name="prefix">The non-empty selector prefix.</param>
    /// <param name="namespaceUri">The namespace URI associated with the prefix.</param>
    public HtmlSelectorNamespaceBinding(string prefix, string namespaceUri)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (prefix.Length == 0)
        {
            throw new ArgumentException(
                "A selector namespace prefix cannot be empty.", "prefix");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        this.Prefix = prefix;
        this.NamespaceUri = namespaceUri;
    }
}

using System;

namespace html;
/// <summary>Stores an HTML attribute name, namespace, value, and optional source location.</summary>
public class HtmlAttribute : IHtmlAttribute
{
    internal string name;
    internal string localName;
    internal string prefix;
    internal string namespaceUri;
    internal string value;
    internal HtmlSourceSpan sourceSpan;
    internal HtmlAttribute(string name, string localName, string prefix, string namespaceUri, string value, HtmlSourceSpan sourceSpan)
    {
        this.name = name;
        this.localName = localName;
        this.prefix = prefix;
        this.namespaceUri = namespaceUri;
        this.value = value;
        this.sourceSpan = sourceSpan;
    }

    /// <summary>Creates an attribute without an XML namespace or prefix.</summary>
    /// <param name = "name">The attribute name.</param>
    /// <param name = "value">The attribute value.</param>
    /// <param name = "sourceSpan">The source location, or an unknown span when unavailable.</param>
    /// <returns>A newly allocated attribute containing the supplied name, value, and source location.</returns>
    internal static HtmlAttribute Create(string name, string value, HtmlSourceSpan sourceSpan)
    {
        return CreateNamespaced(name, name, "", "", value, sourceSpan);
    }

    /// <summary>Creates an attribute with explicit qualified-name and namespace components.</summary>
    /// <param name = "name">The qualified attribute name to store.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <param name = "prefix">The namespace prefix, or an empty string for no prefix.</param>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "value">The attribute value.</param>
    /// <param name = "sourceSpan">The source location, or an unknown span when unavailable.</param>
    /// <returns>A newly allocated attribute containing the supplied namespace data, value, and source location.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static HtmlAttribute CreateNamespaced(string name, string localName, string prefix, string namespaceUri, string value, HtmlSourceSpan sourceSpan)
    {
        ValidateName(name, "name");
        ValidateName(localName, "localName");
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        return new HtmlAttribute(name, localName, prefix, namespaceUri, value, sourceSpan);
    }

    /// <summary>Gets the qualified attribute name, including its prefix when present.</summary>
    /// <returns>The stored qualified name.</returns>
    public string GetName()
    {
        return this.name;
    }

    /// <summary>Gets the namespace-local part of the attribute name.</summary>
    /// <returns>The local name without a namespace prefix.</returns>
    public string GetLocalName()
    {
        return this.localName;
    }

    /// <summary>Gets the namespace prefix stored on the attribute.</summary>
    /// <returns>The prefix, or an empty string when the attribute has none.</returns>
    public string GetPrefix()
    {
        return this.prefix;
    }

    /// <summary>Gets the namespace URI associated with the attribute.</summary>
    /// <returns>The namespace URI, or an empty string for an unnamespaced attribute.</returns>
    public string GetNamespaceUri()
    {
        return this.namespaceUri;
    }

    /// <summary>Gets the decoded attribute value stored in the DOM.</summary>
    /// <returns>The attribute value after HTML character-reference decoding.</returns>
    public string GetValue()
    {
        return this.value;
    }

    /// <summary>Gets where this attribute appeared in the parsed input.</summary>
    /// <returns>The half-open source span, or an explicitly unknown span for a programmatically created or untracked attribute.</returns>
    public HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static void ValidateName(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("An attribute name cannot be empty.", parameterName);
        }
    }
}

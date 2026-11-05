namespace svg;

/// <summary>Represents one ordered namespace-aware XML attribute.</summary>
public class SvgAttribute
{
    /// <summary>The recognized namespace-aware attribute category.</summary>
    public SvgAttributeKind AttributeKind;
    /// <summary>The exact qualified name.</summary>
    public string QualifiedName;
    /// <summary>The namespace prefix, or an empty string.</summary>
    public string Prefix;
    /// <summary>The local name.</summary>
    public string LocalName;
    /// <summary>The resolved namespace URI, or an empty string.</summary>
    public string NamespaceUri;
    /// <summary>The decoded XML-normalized value.</summary>
    public string Value;
    /// <summary>The owning element, or <c>null</c> while unattached.</summary>
    public SvgNode? OwnerElement;
    /// <summary>The owning document, or <c>null</c> while unattached.</summary>
    public SvgDocument? OwnerDocument;
    /// <summary>The retained lexical attribute token, or <c>null</c> for generated attributes.</summary>
    public SvgXmlAttributeToken? RawToken;
    /// <summary>The original source span, or <c>null</c> for generated attributes.</summary>
    public SvgSourceSpan? SourceSpan;
    /// <summary>How the current attribute region relates to authored source.</summary>
    public SvgSourceMapKind SourceMapKind;
    /// <summary>Whether the attribute has changed after source construction.</summary>
    public bool IsDirty;
    /// <summary>The document or detached-attribute version at the most recent change.</summary>
    public long Version;

    /// <summary>Creates an unattached clean XML attribute.</summary>
    /// <param name="qualifiedName">The qualified name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="value">The decoded XML-normalized value.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgAttribute(string qualifiedName, string prefix, string localName, string namespaceUri, string value)
    {
        if (qualifiedName == null) throw new System.ArgumentNullException("qualifiedName");
        if (prefix == null) throw new System.ArgumentNullException("prefix");
        if (localName == null) throw new System.ArgumentNullException("localName");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (value == null) throw new System.ArgumentNullException("value");
        AttributeKind = new SvgNodeNames().GetAttributeKind(namespaceUri, localName);
        QualifiedName = qualifiedName;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Value = value;
        OwnerElement = null;
        OwnerDocument = null;
        RawToken = null;
        SourceSpan = null;
        SourceMapKind = SvgSourceMapKind.Generated;
        IsDirty = false;
        Version = 0;
    }

    /// <summary>Marks this attribute and its owner changed and returns the assigned version.</summary>
    /// <returns>The version assigned to this change.</returns>
    /// <exception cref="System.OverflowException">Thrown when a detached version cannot be incremented.</exception>
    internal long MarkDirty()
    {
        long nextVersion;
        if (OwnerDocument != null) nextVersion = OwnerDocument.MarkDirty();
        else
        {
            Version = Version + 1;
            nextVersion = Version;
        }
        IsDirty = true;
        Version = nextVersion;
        MarkSourceRewritten();
        if (OwnerElement != null)
        {
            OwnerElement.IsDirty = true;
            OwnerElement.Version = nextVersion;
            OwnerElement.MarkSourceRewritten();
        }
        return nextVersion;
    }

    internal void MarkSourceRewritten()
    {
        if (SourceMapKind == SvgSourceMapKind.Original) SourceMapKind = SvgSourceMapKind.Rewritten;
    }
}

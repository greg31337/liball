namespace svg;

/// <summary>Represents one tagged, namespace-aware XML node without an inheritance hierarchy.</summary>
public class SvgNode
{
    /// <summary>The tagged node kind.</summary>
    public SvgNodeKind Kind;
    /// <summary>The recognized SVG element kind, or <c>Unknown</c> for other node and element names.</summary>
    public SvgElementKind ElementKind;
    /// <summary>The exact qualified name, or an empty string.</summary>
    public string QualifiedName;
    /// <summary>The namespace prefix, or an empty string.</summary>
    public string Prefix;
    /// <summary>The local name, or an empty string.</summary>
    public string LocalName;
    /// <summary>The resolved namespace URI, or an empty string.</summary>
    public string NamespaceUri;
    /// <summary>The normalized character or instruction value, or an empty string.</summary>
    public string Value;
    /// <summary>The ordered child nodes.</summary>
    public System.Collections.Generic.List<SvgNode> Children;
    /// <summary>The ordered element attributes.</summary>
    public System.Collections.Generic.List<SvgAttribute> Attributes;
    /// <summary>The parent node, or <c>null</c> for the document node or an unattached node.</summary>
    public SvgNode? Parent;
    /// <summary>The owning document, or <c>null</c> for an unattached node.</summary>
    public SvgDocument? OwnerDocument;
    /// <summary>The retained opening or content lexical token, or <c>null</c> for generated nodes.</summary>
    public SvgXmlToken? RawToken;
    /// <summary>The retained end-element lexical token, or <c>null</c> when it does not apply.</summary>
    public SvgXmlToken? EndToken;
    /// <summary>The original source span, or <c>null</c> for generated nodes.</summary>
    public SvgSourceSpan? SourceSpan;
    /// <summary>How the current node region relates to authored source.</summary>
    public SvgSourceMapKind SourceMapKind;
    /// <summary>Whether the node has changed after source construction.</summary>
    public bool IsDirty;
    /// <summary>The document or detached-node version at the most recent change.</summary>
    public long Version;

    /// <summary>Creates an unattached clean tagged node.</summary>
    /// <param name="kind">The tagged node kind.</param>
    /// <param name="qualifiedName">The qualified name, or an empty string.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local name, or an empty string.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="value">The normalized value, or an empty string.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>kind</c> is invalid.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when a string argument is <c>null</c>.</exception>
    public SvgNode(SvgNodeKind kind, string qualifiedName, string prefix, string localName, string namespaceUri, string value)
    {
        if (kind < SvgNodeKind.Document || kind > SvgNodeKind.Doctype) throw new System.ArgumentOutOfRangeException("kind");
        if (qualifiedName == null) throw new System.ArgumentNullException("qualifiedName");
        if (prefix == null) throw new System.ArgumentNullException("prefix");
        if (localName == null) throw new System.ArgumentNullException("localName");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (value == null) throw new System.ArgumentNullException("value");
        Kind = kind;
        ElementKind = kind == SvgNodeKind.Element ? new SvgNodeNames().GetElementKind(namespaceUri, localName) : SvgElementKind.Unknown;
        QualifiedName = qualifiedName;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Value = value;
        Children = new System.Collections.Generic.List<SvgNode>();
        Attributes = new System.Collections.Generic.List<SvgAttribute>();
        Parent = null;
        OwnerDocument = null;
        RawToken = null;
        EndToken = null;
        SourceSpan = null;
        SourceMapKind = SvgSourceMapKind.Generated;
        IsDirty = false;
        Version = 0;
    }

    /// <summary>Marks this node changed and returns its new local or document version.</summary>
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
        return nextVersion;
    }

    internal void MarkSourceRewritten()
    {
        if (SourceMapKind == SvgSourceMapKind.Original) SourceMapKind = SvgSourceMapKind.Rewritten;
    }
}

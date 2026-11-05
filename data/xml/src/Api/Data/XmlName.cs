namespace xml;

/// <summary>
/// Carries the lexical and expanded parts of an XML name.
/// </summary>
public class XmlName
{
    /// <summary>
    /// The lexical prefix, or null when no prefix is present.
    /// </summary>
    public string? Prefix;
    /// <summary>
    /// The local name.
    /// </summary>
    public string? LocalName;
    /// <summary>
    /// The namespace URI, or null when the name has no namespace.
    /// </summary>
    public string? NamespaceUri;
    /// <summary>
    /// The lexical qualified name.
    /// </summary>
    public string? QualifiedName;
}


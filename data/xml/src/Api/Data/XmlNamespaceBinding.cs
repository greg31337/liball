namespace xml;

/// <summary>
/// Carries one namespace prefix binding.
/// </summary>
public class XmlNamespaceBinding
{
    /// <summary>
    /// The bound prefix, with an empty string representing the default namespace.
    /// </summary>
    public string? Prefix;
    /// <summary>
    /// The namespace URI, with an empty string representing undeclaration.
    /// </summary>
    public string? NamespaceUri;
    /// <summary>
    /// Whether the binding undeclares a namespace.
    /// </summary>
    public bool IsUndeclaration;
    /// <summary>
    /// The source range of the declaration.
    /// </summary>
    public SourceRange? Range;
}


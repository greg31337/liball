namespace xml;

/// <summary>
/// Carries one DTD element declaration.
/// </summary>
public class XmlElementDeclaration
{
    /// <summary>
    /// The declared element name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The normalized lexical content model.
    /// </summary>
    public string? ContentModel;
    /// <summary>
    /// The element names referenced by the content model.
    /// </summary>
    public string[]? ReferencedNames;
    /// <summary>
    /// Whether the declaration uses EMPTY.
    /// </summary>
    public bool IsEmpty;
    /// <summary>
    /// Whether the declaration uses ANY.
    /// </summary>
    public bool IsAny;
    /// <summary>
    /// Whether the declaration permits mixed content.
    /// </summary>
    public bool IsMixed;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}


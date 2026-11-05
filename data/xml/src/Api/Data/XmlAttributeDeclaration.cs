namespace xml;

/// <summary>
/// Carries one DTD attribute declaration.
/// </summary>
public class XmlAttributeDeclaration
{
    /// <summary>
    /// The element name to which the attribute belongs.
    /// </summary>
    public XmlName? ElementName;
    /// <summary>
    /// The declared attribute name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The declared attribute type.
    /// </summary>
    public string? TypeName;
    /// <summary>
    /// The enumeration or NOTATION values.
    /// </summary>
    public string[]? AllowedValues;
    /// <summary>
    /// The default mode such as required, implied, or fixed.
    /// </summary>
    public string? DefaultKind;
    /// <summary>
    /// The normalized default value.
    /// </summary>
    public string? DefaultValue;
    /// <summary>
    /// Whether the attribute has type ID.
    /// </summary>
    public bool IsId;
    /// <summary>
    /// Whether the declaration names a namespace declaration.
    /// </summary>
    public bool IsNamespaceDeclaration;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}


namespace xml;

/// <summary>
/// Carries one ordered XML attribute.
/// </summary>
public class XmlAttributeData
{
    /// <summary>
    /// The attribute name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The normalized attribute value.
    /// </summary>
    public string? Value;
    /// <summary>
    /// The lexical attribute value before normalization.
    /// </summary>
    public string? RawValue;
    /// <summary>
    /// The lexical quote character.
    /// </summary>
    public char Quote;
    /// <summary>
    /// Whether the attribute was present in source markup.
    /// </summary>
    public bool IsSpecified;
    /// <summary>
    /// Whether the attribute came from a DTD or schema default.
    /// </summary>
    public bool IsDefaulted;
    /// <summary>
    /// Whether the declaration fixes the attribute value.
    /// </summary>
    public bool IsFixed;
    /// <summary>
    /// Whether the attribute is a namespace declaration.
    /// </summary>
    public bool IsNamespaceDeclaration;
    /// <summary>
    /// The source range of the attribute.
    /// </summary>
    public SourceRange? Range;
}


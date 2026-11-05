namespace xml;

/// <summary>
/// Carries a retained XML document type definition.
/// </summary>
public class XmlDocumentType
{
    /// <summary>
    /// The declared document element name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The external subset public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The external subset system identifier.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The lexical internal subset.
    /// </summary>
    public string? InternalSubset;
    /// <summary>
    /// The declaration base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The retained element declarations.
    /// </summary>
    public XmlElementDeclaration[]? ElementDeclarations;
    /// <summary>
    /// The retained attribute declarations.
    /// </summary>
    public XmlAttributeDeclaration[]? AttributeDeclarations;
    /// <summary>
    /// The retained general and parameter entity declarations.
    /// </summary>
    public XmlEntityDeclaration[]? EntityDeclarations;
    /// <summary>
    /// The retained notation declarations.
    /// </summary>
    public XmlNotationDeclaration[]? NotationDeclarations;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}


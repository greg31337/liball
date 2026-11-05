namespace xml;

/// <summary>
/// Carries one retained DTD notation declaration.
/// </summary>
public class XmlNotationDeclaration
{
    /// <summary>
    /// The notation name.
    /// </summary>
    public string? Name;
    /// <summary>
    /// The public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The system identifier.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The declaration base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}


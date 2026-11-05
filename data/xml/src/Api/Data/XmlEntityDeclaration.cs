namespace xml;

/// <summary>
/// Carries one retained DTD entity declaration.
/// </summary>
public class XmlEntityDeclaration
{
    /// <summary>
    /// The entity name.
    /// </summary>
    public string? Name;
    /// <summary>
    /// The internal replacement text.
    /// </summary>
    public string? ReplacementText;
    /// <summary>
    /// The external public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The external system identifier.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The notation name for an unparsed entity.
    /// </summary>
    public string? NotationName;
    /// <summary>
    /// The declaration base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// Whether this is a parameter entity.
    /// </summary>
    public bool IsParameter;
    /// <summary>
    /// Whether this is an external entity.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// Whether this is a parsed entity.
    /// </summary>
    public bool IsParsed;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}


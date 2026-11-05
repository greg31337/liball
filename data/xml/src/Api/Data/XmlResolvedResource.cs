namespace xml;

/// <summary>
/// Carries a resource returned by an XML resource resolver.
/// </summary>
public class XmlResolvedResource
{
    /// <summary>
    /// The resolved resource kind.
    /// </summary>
    public XmlResourceKind Kind;
    /// <summary>
    /// The access decision applied to the resource.
    /// </summary>
    public XmlResourceAccess Access;
    /// <summary>
    /// The resolved input.
    /// </summary>
    public XmlInput? Input;
    /// <summary>
    /// The URI supplied to the resolver.
    /// </summary>
    public string? RequestedUri;
    /// <summary>
    /// The absolute URI of the resolved resource.
    /// </summary>
    public string? AbsoluteUri;
    /// <summary>
    /// The normalized public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The system identifier.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The resource media type.
    /// </summary>
    public string? MediaType;
    /// <summary>
    /// The resource encoding name.
    /// </summary>
    public string? EncodingName;
    /// <summary>
    /// Whether the consumer owns and closes the resolved input.
    /// </summary>
    public bool OwnsResource;
}

